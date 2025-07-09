using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace QuestCameraTools.App
{
    /// <summary>
    /// GeminiVoiceToggle component for voice-activated AI interactions with visual context.
    /// Handles press-to-talk functionality, integrates with Gemini API, and provides audio feedback.
    /// </summary>
    public class GeminiVoiceToggle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Visual States")]
        [SerializeField] private Material idleMaterial;
        [SerializeField] private Material recordingMaterial;
        [SerializeField] private Material processingMaterial;
        [SerializeField] private Material playingMaterial;
        
        [Header("Audio Settings")]
        [SerializeField] private int recordingFrequency = 16000; // Optimized for voice
        [SerializeField] private float maxRecordingTime = 30f;
        [SerializeField] private float minRecordingTime = 0.5f;
        
        [Header("API Configuration")]
        [SerializeField] private string geminiApiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro-vision:generateContent";
        [SerializeField] private string ttsApiEndpoint = "https://texttospeech.googleapis.com/v1/text:synthesize";
        [SerializeField] private bool useEnvironmentKey = true;
        [SerializeField] private string apiKeyOverride; // For testing only
        
        [Header("References")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Image targetImage;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Camera passthroughCamera;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        /// <summary>
        /// Current state of the voice toggle component
        /// </summary>
        private enum State
        {
            Idle,           // Ready for interaction
            Recording,      // Actively recording audio
            Processing,     // Sending request to API
            Playing         // Playing back response
        }
        
        private State currentState = State.Idle;
        private AudioClip recordedClip;
        private Coroutine recordingCoroutine;
        private Coroutine processingCoroutine;
        private string apiKey;
        private float recordingStartTime;
        private byte[] lastScreenshotData;
        
        // Audio processing
        private readonly List<float> audioSamples = new List<float>();
        private const int SAMPLE_RATE = 16000;
        private const float SILENCE_THRESHOLD = 0.01f;
        
        // API constants
        private const int MAX_RETRIES = 3;
        private const float REQUEST_TIMEOUT = 30f;
        private const int MAX_IMAGE_SIZE = 1024;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeComponents();
            InitializeAPI();
            UpdateVisualState();
            
            if (enableDebugLogs)
                Debug.Log($"[GeminiVoiceToggle] Initialized on {gameObject.name}");
        }
        
        private void OnDestroy()
        {
            // Cleanup resources
            if (recordingCoroutine != null)
            {
                StopCoroutine(recordingCoroutine);
                Microphone.End(null);
            }
            
            if (processingCoroutine != null)
            {
                StopCoroutine(processingCoroutine);
            }
            
            if (recordedClip != null)
            {
                Destroy(recordedClip);
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            // Auto-assign components if not set
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();
            
            if (targetImage == null)
                targetImage = GetComponent<Image>();
            
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            
            if (passthroughCamera == null)
                passthroughCamera = Camera.main;
            
            // Validate required components
            if (idleMaterial == null || recordingMaterial == null || 
                processingMaterial == null || playingMaterial == null)
            {
                Debug.LogError("[GeminiVoiceToggle] Please assign all state materials!");
                enabled = false;
                return;
            }
            
            if (audioSource == null)
            {
                Debug.LogError("[GeminiVoiceToggle] AudioSource component required!");
                enabled = false;
                return;
            }
        }
        
        private void InitializeAPI()
        {
            // Priority order for API key acquisition
            if (useEnvironmentKey)
            {
                apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = PlayerPrefs.GetString("GeminiAPIKey", "");
            }
            
            if (string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiKeyOverride))
            {
                apiKey = apiKeyOverride;
                Debug.LogWarning("[GeminiVoiceToggle] Using override API key - not recommended for production");
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[GeminiVoiceToggle] No API key found. Please set GEMINI_API_KEY environment variable");
                enabled = false;
            }
        }
        
        #endregion
        
        #region Input Handlers
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (currentState == State.Idle)
            {
                StartRecording();
            }
            else if (currentState == State.Playing)
            {
                StopPlayback();
            }
            
            if (enableDebugLogs)
                Debug.Log($"[GeminiVoiceToggle] Pointer down - State: {currentState}");
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            if (currentState == State.Recording)
            {
                StopRecording();
            }
            
            if (enableDebugLogs)
                Debug.Log($"[GeminiVoiceToggle] Pointer up - State: {currentState}");
        }
        
        #endregion
        
        #region Audio Recording
        
        private void StartRecording()
        {
            if (Microphone.devices.Length == 0)
            {
                HandleError("No microphone found", "recording");
                return;
            }
            
            string micDevice = Microphone.devices[0];
            currentState = State.Recording;
            recordingStartTime = Time.time;
            UpdateVisualState();
            
            // Capture screenshot for context
            CaptureScreenshot();
            
            // Start recording
            recordingCoroutine = StartCoroutine(RecordAudio(micDevice));
            
            if (enableDebugLogs)
                Debug.Log("[GeminiVoiceToggle] Started recording");
        }
        
        private void StopRecording()
        {
            if (currentState != State.Recording)
                return;
            
            float recordingDuration = Time.time - recordingStartTime;
            
            if (recordingCoroutine != null)
            {
                StopCoroutine(recordingCoroutine);
                recordingCoroutine = null;
            }
            
            Microphone.End(null);
            
            if (recordingDuration < minRecordingTime)
            {
                HandleError("Recording too short", "recording");
                return;
            }
            
            if (recordedClip != null && recordedClip.samples > 0)
            {
                ProcessVoiceQuery();
            }
            else
            {
                HandleError("No audio recorded", "recording");
            }
            
            if (enableDebugLogs)
                Debug.Log($"[GeminiVoiceToggle] Stopped recording - Duration: {recordingDuration:F2}s");
        }
        
        private IEnumerator RecordAudio(string micDevice)
        {
            recordedClip = Microphone.Start(micDevice, false, (int)maxRecordingTime, recordingFrequency);
            
            while (currentState == State.Recording && 
                   Time.time - recordingStartTime < maxRecordingTime)
            {
                yield return null;
            }
            
            if (currentState == State.Recording)
            {
                // Max recording time reached
                StopRecording();
            }
        }
        
        #endregion
        
        #region Screenshot Capture
        
        private void CaptureScreenshot()
        {
            if (passthroughCamera == null)
            {
                Debug.LogWarning("[GeminiVoiceToggle] No camera found for screenshot");
                return;
            }
            
            StartCoroutine(CaptureScreenshotCoroutine());
        }
        
        private IEnumerator CaptureScreenshotCoroutine()
        {
            yield return new WaitForEndOfFrame();
            
            try
            {
                // Create render texture
                RenderTexture renderTexture = new RenderTexture(MAX_IMAGE_SIZE, MAX_IMAGE_SIZE, 24);
                passthroughCamera.targetTexture = renderTexture;
                passthroughCamera.Render();
                
                // Read pixels
                RenderTexture.active = renderTexture;
                Texture2D texture = new Texture2D(MAX_IMAGE_SIZE, MAX_IMAGE_SIZE, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, MAX_IMAGE_SIZE, MAX_IMAGE_SIZE), 0, 0);
                texture.Apply();
                
                // Encode to PNG
                lastScreenshotData = texture.EncodeToPNG();
                
                // Cleanup
                passthroughCamera.targetTexture = null;
                RenderTexture.active = null;
                Destroy(renderTexture);
                Destroy(texture);
                
                if (enableDebugLogs)
                    Debug.Log($"[GeminiVoiceToggle] Screenshot captured: {lastScreenshotData.Length} bytes");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GeminiVoiceToggle] Screenshot capture failed: {e.Message}");
                lastScreenshotData = null;
            }
        }
        
        #endregion
        
        #region Voice Query Processing
        
        private void ProcessVoiceQuery()
        {
            if (processingCoroutine != null)
            {
                StopCoroutine(processingCoroutine);
            }
            
            processingCoroutine = StartCoroutine(ProcessVoiceQueryCoroutine());
        }
        
        private IEnumerator ProcessVoiceQueryCoroutine()
        {
            currentState = State.Processing;
            UpdateVisualState();
            
            // Convert audio to bytes
            byte[] audioBytes = ConvertAudioClipToWav(recordedClip);
            if (audioBytes == null)
            {
                HandleError("Failed to process audio", "processing");
                yield break;
            }
            
            // Send to Gemini API
            yield return StartCoroutine(SendGeminiRequest(audioBytes, lastScreenshotData));
        }
        
        private IEnumerator SendGeminiRequest(byte[] audioData, byte[] imageData)
        {
            string requestJson = BuildGeminiRequest(audioData, imageData);
            
            using (UnityWebRequest request = new UnityWebRequest(geminiApiEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(requestJson));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", apiKey);
                request.timeout = (int)REQUEST_TIMEOUT;
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    ProcessGeminiResponse(request.downloadHandler.text);
                }
                else
                {
                    HandleNetworkError(request.result, request.error);
                }
            }
        }
        
        private string BuildGeminiRequest(byte[] audioData, byte[] imageData)
        {
            var parts = new List<object>();
            
            parts.Add(new { text = "Analyze what you see in this image and respond to my voice query. Be conversational and helpful." });
            
            if (imageData != null)
            {
                parts.Add(new {
                    inline_data = new {
                        mime_type = "image/png",
                        data = Convert.ToBase64String(imageData)
                    }
                });
            }
            
            if (audioData != null)
            {
                parts.Add(new {
                    inline_data = new {
                        mime_type = "audio/wav",
                        data = Convert.ToBase64String(audioData)
                    }
                });
            }
            
            var request = new {
                contents = new[] {
                    new {
                        role = "user",
                        parts = parts.ToArray()
                    }
                },
                generationConfig = new {
                    temperature = 0.7f,
                    topK = 40,
                    topP = 0.95f,
                    maxOutputTokens = 1024
                }
            };
            
            return JsonUtility.ToJson(request);
        }
        
        private void ProcessGeminiResponse(string responseJson)
        {
            try
            {
                // Parse response (simplified - in production, use proper JSON parsing)
                var response = JsonUtility.FromJson<GeminiResponse>(responseJson);
                
                if (response?.candidates != null && response.candidates.Length > 0)
                {
                    string responseText = response.candidates[0].content.parts[0].text;
                    
                    if (enableDebugLogs)
                        Debug.Log($"[GeminiVoiceToggle] Gemini response: {responseText}");
                    
                    // Convert to speech and play
                    StartCoroutine(ConvertToSpeechAndPlay(responseText));
                }
                else
                {
                    HandleError("Empty response from Gemini", "processing");
                }
            }
            catch (Exception e)
            {
                HandleError($"Failed to parse response: {e.Message}", "processing");
            }
        }
        
        #endregion
        
        #region Text-to-Speech
        
        private IEnumerator ConvertToSpeechAndPlay(string text)
        {
            var ttsRequest = new {
                input = new { text = text },
                voice = new {
                    languageCode = "en-US",
                    name = "en-US-Neural2-J"
                },
                audioConfig = new {
                    audioEncoding = "MP3",
                    speakingRate = 1.0f
                }
            };
            
            string requestJson = JsonUtility.ToJson(ttsRequest);
            
            using (UnityWebRequest request = new UnityWebRequest(ttsApiEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(requestJson));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", apiKey);
                request.timeout = (int)REQUEST_TIMEOUT;
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var ttsResponse = JsonUtility.FromJson<TTSResponse>(request.downloadHandler.text);
                        byte[] audioData = Convert.FromBase64String(ttsResponse.audioContent);
                        
                        // Create audio clip from MP3 data (simplified - may need proper MP3 decoder)
                        AudioClip responseClip = CreateAudioClipFromMP3(audioData);
                        
                        if (responseClip != null)
                        {
                            PlayResponse(responseClip);
                        }
                        else
                        {
                            HandleError("Failed to create audio clip", "playback");
                        }
                    }
                    catch (Exception e)
                    {
                        HandleError($"Failed to process TTS response: {e.Message}", "playback");
                    }
                }
                else
                {
                    HandleError($"TTS request failed: {request.error}", "playback");
                }
            }
        }
        
        private AudioClip CreateAudioClipFromMP3(byte[] mp3Data)
        {
            // Note: This is a simplified implementation
            // In production, you'd need a proper MP3 decoder or use WAV format
            // For now, we'll return null and handle gracefully
            Debug.LogWarning("[GeminiVoiceToggle] MP3 decoding not implemented - consider using WAV format");
            return null;
        }
        
        #endregion
        
        #region Audio Playback
        
        private void PlayResponse(AudioClip responseClip)
        {
            if (audioSource == null || responseClip == null)
            {
                HandleError("Cannot play audio response", "playback");
                return;
            }
            
            currentState = State.Playing;
            UpdateVisualState();
            
            audioSource.clip = responseClip;
            audioSource.Play();
            
            StartCoroutine(WaitForPlaybackComplete());
            
            if (enableDebugLogs)
                Debug.Log($"[GeminiVoiceToggle] Playing response - Duration: {responseClip.length:F2}s");
        }
        
        private IEnumerator WaitForPlaybackComplete()
        {
            while (audioSource.isPlaying)
            {
                yield return null;
            }
            
            currentState = State.Idle;
            UpdateVisualState();
            
            if (enableDebugLogs)
                Debug.Log("[GeminiVoiceToggle] Playback complete");
        }
        
        private void StopPlayback()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                currentState = State.Idle;
                UpdateVisualState();
                
                if (enableDebugLogs)
                    Debug.Log("[GeminiVoiceToggle] Playback stopped");
            }
        }
        
        #endregion
        
        #region Visual State Management
        
        private void UpdateVisualState()
        {
            Material newMaterial = currentState switch
            {
                State.Idle => idleMaterial,
                State.Recording => recordingMaterial,
                State.Processing => processingMaterial,
                State.Playing => playingMaterial,
                _ => idleMaterial
            };
            
            if (targetRenderer != null)
            {
                targetRenderer.material = newMaterial;
            }
            
            if (targetImage != null)
            {
                targetImage.material = newMaterial;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[GeminiVoiceToggle] Updated visual state to {currentState}");
        }
        
        #endregion
        
        #region Error Handling
        
        private void HandleError(string message, string context)
        {
            Debug.LogError($"[GeminiVoiceToggle] {context}: {message}");
            
            // Reset to idle state
            currentState = State.Idle;
            UpdateVisualState();
            
            // Stop any ongoing operations
            if (recordingCoroutine != null)
            {
                StopCoroutine(recordingCoroutine);
                recordingCoroutine = null;
                Microphone.End(null);
            }
            
            if (processingCoroutine != null)
            {
                StopCoroutine(processingCoroutine);
                processingCoroutine = null;
            }
            
            // Cleanup audio clip
            if (recordedClip != null)
            {
                Destroy(recordedClip);
                recordedClip = null;
            }
            
            // TODO: Add user feedback UI for errors
        }
        
        private void HandleNetworkError(UnityWebRequest.Result result, string error)
        {
            string message = result switch
            {
                UnityWebRequest.Result.ConnectionError => "No internet connection",
                UnityWebRequest.Result.ProtocolError => $"API error: {error}",
                UnityWebRequest.Result.DataProcessingError => "Failed to process response",
                _ => $"Unknown error: {error}"
            };
            
            HandleError(message, "network");
        }
        
        #endregion
        
        #region Audio Utilities
        
        private byte[] ConvertAudioClipToWav(AudioClip clip)
        {
            if (clip == null)
                return null;
            
            try
            {
                float[] samples = new float[clip.samples];
                clip.GetData(samples, 0);
                
                // Convert to 16-bit WAV format
                byte[] wav = new byte[44 + samples.Length * 2];
                int offset = 0;
                
                // WAV header
                Buffer.BlockCopy(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, wav, offset, 4);
                offset += 4;
                Buffer.BlockCopy(BitConverter.GetBytes(36 + samples.Length * 2), 0, wav, offset, 4);
                offset += 4;
                Buffer.BlockCopy(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, wav, offset, 4);
                offset += 4;
                
                // Format chunk
                Buffer.BlockCopy(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, wav, offset, 4);
                offset += 4;
                Buffer.BlockCopy(BitConverter.GetBytes(16), 0, wav, offset, 4);
                offset += 4;
                Buffer.BlockCopy(BitConverter.GetBytes((short)1), 0, wav, offset, 2);
                offset += 2;
                Buffer.BlockCopy(BitConverter.GetBytes((short)1), 0, wav, offset, 2);
                offset += 2;
                Buffer.BlockCopy(BitConverter.GetBytes(clip.frequency), 0, wav, offset, 4);
                offset += 4;
                Buffer.BlockCopy(BitConverter.GetBytes(clip.frequency * 2), 0, wav, offset, 4);
                offset += 4;
                Buffer.BlockCopy(BitConverter.GetBytes((short)2), 0, wav, offset, 2);
                offset += 2;
                Buffer.BlockCopy(BitConverter.GetBytes((short)16), 0, wav, offset, 2);
                offset += 2;
                
                // Data chunk
                Buffer.BlockCopy(System.Text.Encoding.UTF8.GetBytes("data"), 0, wav, offset, 4);
                offset += 4;
                Buffer.BlockCopy(BitConverter.GetBytes(samples.Length * 2), 0, wav, offset, 4);
                offset += 4;
                
                // Audio data
                for (int i = 0; i < samples.Length; i++)
                {
                    short sample = (short)(samples[i] * 32767f);
                    Buffer.BlockCopy(BitConverter.GetBytes(sample), 0, wav, offset, 2);
                    offset += 2;
                }
                
                return wav;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GeminiVoiceToggle] Audio conversion failed: {e.Message}");
                return null;
            }
        }
        
        #endregion
        
        #region Data Structures
        
        [Serializable]
        private class GeminiResponse
        {
            public Candidate[] candidates;
        }
        
        [Serializable]
        private class Candidate
        {
            public Content content;
        }
        
        [Serializable]
        private class Content
        {
            public Part[] parts;
        }
        
        [Serializable]
        private class Part
        {
            public string text;
        }
        
        [Serializable]
        private class TTSResponse
        {
            public string audioContent;
        }
        
        #endregion
    }
}