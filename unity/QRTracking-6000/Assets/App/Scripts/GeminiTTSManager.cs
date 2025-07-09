using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace QuestCameraTools.Gemini
{
    /// <summary>
    /// Service manager for Google Cloud Text-to-Speech API integration.
    /// Handles TTS requests, audio clip conversion, and voice configuration.
    /// Supports both async/await and coroutine patterns for Unity compatibility.
    /// </summary>
    public class GeminiTTSManager : MonoBehaviour
    {
        [Header("TTS Configuration")]
        [SerializeField] private string ttsEndpoint = "https://texttospeech.googleapis.com/v1/text:synthesize";
        [SerializeField] private string defaultVoiceName = "en-US-Neural2-J";
        [SerializeField] private string defaultLanguageCode = "en-US";
        [SerializeField] private float defaultSpeakingRate = 1.0f;
        [SerializeField] private float defaultPitch = 0.0f;
        [SerializeField] private float defaultVolumeGain = 0.0f;
        
        [Header("Request Settings")]
        [SerializeField] private int maxRetries = 3;
        [SerializeField] private float requestTimeout = 15f;
        [SerializeField] private float retryDelay = 1f;
        
        [Header("Audio Settings")]
        [SerializeField] private AudioEncoding audioEncoding = AudioEncoding.LINEAR16;
        [SerializeField] private int sampleRateHertz = 22050;
        [SerializeField] private int audioChannels = 1;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool logRequestDetails = false;
        
        private string apiKey;
        private bool isInitialized = false;
        
        /// <summary>
        /// Audio encoding formats supported by Google Cloud TTS
        /// </summary>
        public enum AudioEncoding
        {
            LINEAR16,
            MP3,
            OGG_OPUS,
            MULAW,
            ALAW
        }
        
        /// <summary>
        /// Structure for TTS request parameters
        /// </summary>
        [Serializable]
        public struct TTSRequest
        {
            public string text;
            public string languageCode;
            public string voiceName;
            public float speakingRate;
            public float pitch;
            public float volumeGain;
            public AudioEncoding audioEncoding;
            public int sampleRateHertz;
            
            /// <summary>
            /// Create a default TTS request with the provided text
            /// </summary>
            /// <param name="text">Text to convert to speech</param>
            /// <returns>Default TTS request configuration</returns>
            public static TTSRequest CreateDefault(string text)
            {
                return new TTSRequest
                {
                    text = text,
                    languageCode = "en-US",
                    voiceName = "en-US-Neural2-J",
                    speakingRate = 1.0f,
                    pitch = 0.0f,
                    volumeGain = 0.0f,
                    audioEncoding = AudioEncoding.LINEAR16,
                    sampleRateHertz = 22050
                };
            }
        }
        
        /// <summary>
        /// Response structure for TTS API results
        /// </summary>
        [Serializable]
        public struct TTSResponse
        {
            public bool success;
            public AudioClip audioClip;
            public string error;
            public int statusCode;
            public float responseTime;
            public int audioLength; // in samples
        }
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeTTS();
        }
        
        private void Start()
        {
            if (!isInitialized)
            {
                InitializeTTS();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeTTS()
        {
            if (isInitialized) return;
            
            // Initialize API key from the same sources as GeminiAPIManager
            InitializeAuthentication();
            
            if (string.IsNullOrEmpty(apiKey))
            {
                LogError("TTS initialization failed - no API key available");
                return;
            }
            
            isInitialized = true;
            
            if (enableDebugLogs)
                Debug.Log("[GeminiTTSManager] Initialized successfully");
        }
        
        private void InitializeAuthentication()
        {
            // Priority order for API key acquisition (same as GeminiAPIManager)
            
            // 1. Environment variable (recommended for production)
            apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                LogDebug("Using environment variable API key");
                return;
            }
            
            // 2. PlayerPrefs (for development persistence)
            apiKey = PlayerPrefs.GetString("GeminiAPIKey", "");
            if (!string.IsNullOrEmpty(apiKey))
            {
                LogDebug("Using PlayerPrefs API key");
                return;
            }
            
            LogError("No API key found. Please set GEMINI_API_KEY environment variable or configure in PlayerPrefs");
        }
        
        #endregion
        
        #region Public API Methods
        
        /// <summary>
        /// Convert text to speech using async/await pattern
        /// </summary>
        /// <param name="text">Text to convert to speech</param>
        /// <returns>TTS response with audio clip or error information</returns>
        public async Task<TTSResponse> ConvertTextToSpeechAsync(string text)
        {
            var request = TTSRequest.CreateDefault(text);
            return await ConvertTextToSpeechAsync(request);
        }
        
        /// <summary>
        /// Convert text to speech with custom configuration using async/await pattern
        /// </summary>
        /// <param name="request">TTS request configuration</param>
        /// <returns>TTS response with audio clip or error information</returns>
        public async Task<TTSResponse> ConvertTextToSpeechAsync(TTSRequest request)
        {
            if (!isInitialized)
            {
                return CreateErrorResponse("TTS not initialized", 0);
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                return CreateErrorResponse("No API key available", 401);
            }
            
            if (string.IsNullOrEmpty(request.text))
            {
                return CreateErrorResponse("No text provided", 400);
            }
            
            return await SendTTSRequestWithRetry(request);
        }
        
        /// <summary>
        /// Convert text to speech using coroutines (for non-async contexts)
        /// </summary>
        /// <param name="text">Text to convert to speech</param>
        /// <param name="callback">Callback to handle the response</param>
        public void ConvertTextToSpeech(string text, Action<TTSResponse> callback)
        {
            var request = TTSRequest.CreateDefault(text);
            ConvertTextToSpeech(request, callback);
        }
        
        /// <summary>
        /// Convert text to speech with custom configuration using coroutines
        /// </summary>
        /// <param name="request">TTS request configuration</param>
        /// <param name="callback">Callback to handle the response</param>
        public void ConvertTextToSpeech(TTSRequest request, Action<TTSResponse> callback)
        {
            if (!isInitialized)
            {
                callback?.Invoke(CreateErrorResponse("TTS not initialized", 0));
                return;
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                callback?.Invoke(CreateErrorResponse("No API key available", 401));
                return;
            }
            
            if (string.IsNullOrEmpty(request.text))
            {
                callback?.Invoke(CreateErrorResponse("No text provided", 400));
                return;
            }
            
            StartCoroutine(SendTTSRequestCoroutine(request, callback));
        }
        
        /// <summary>
        /// Set API key at runtime (useful for dynamic configuration)
        /// </summary>
        /// <param name="key">The API key to set</param>
        /// <param name="persistToPlayerPrefs">Whether to save to PlayerPrefs</param>
        public void SetAPIKey(string key, bool persistToPlayerPrefs = false)
        {
            apiKey = key;
            
            if (persistToPlayerPrefs)
            {
                PlayerPrefs.SetString("GeminiAPIKey", key);
                PlayerPrefs.Save();
            }
            
            LogDebug("API key updated");
        }
        
        /// <summary>
        /// Check if the TTS manager is properly initialized and ready
        /// </summary>
        /// <returns>True if ready to process requests</returns>
        public bool IsReady()
        {
            return isInitialized && !string.IsNullOrEmpty(apiKey);
        }
        
        #endregion
        
        #region Request Processing
        
        private async Task<TTSResponse> SendTTSRequestWithRetry(TTSRequest request)
        {
            TTSResponse lastResponse = CreateErrorResponse("Unknown error", 0);
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    LogDebug($"Sending TTS request attempt {attempt + 1}/{maxRetries}");
                    
                    var response = await SendSingleTTSRequest(request);
                    
                    if (response.success)
                    {
                        LogDebug($"TTS request successful on attempt {attempt + 1}");
                        return response;
                    }
                    
                    lastResponse = response;
                    
                    // Check if we should retry based on status code
                    if (!ShouldRetry(response.statusCode))
                    {
                        LogDebug($"Not retrying TTS due to status code: {response.statusCode}");
                        break;
                    }
                    
                    // Wait before retry (exponential backoff)
                    if (attempt < maxRetries - 1)
                    {
                        float delay = retryDelay * Mathf.Pow(2, attempt);
                        LogDebug($"Waiting {delay}s before TTS retry...");
                        await Task.Delay(Mathf.RoundToInt(delay * 1000));
                    }
                }
                catch (Exception e)
                {
                    LogError($"TTS request attempt {attempt + 1} failed: {e.Message}");
                    lastResponse = CreateErrorResponse($"Exception: {e.Message}", 0);
                }
            }
            
            LogError($"All {maxRetries} TTS attempts failed. Last error: {lastResponse.error}");
            return lastResponse;
        }
        
        private async Task<TTSResponse> SendSingleTTSRequest(TTSRequest request)
        {
            float startTime = Time.time;
            
            try
            {
                string requestJson = BuildTTSRequestJson(request);
                
                if (logRequestDetails)
                {
                    LogDebug($"TTS Request JSON: {requestJson}");
                }
                
                using (UnityWebRequest webRequest = new UnityWebRequest(ttsEndpoint, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    
                    // Set headers
                    webRequest.SetRequestHeader("Content-Type", "application/json");
                    webRequest.SetRequestHeader("X-Goog-Api-Key", apiKey);
                    webRequest.timeout = Mathf.RoundToInt(requestTimeout);
                    
                    // Send request
                    var operation = webRequest.SendWebRequest();
                    
                    // Wait for completion
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    float responseTime = Time.time - startTime;
                    
                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        string responseText = webRequest.downloadHandler.text;
                        
                        if (logRequestDetails)
                        {
                            LogDebug($"TTS Response: {responseText}");
                        }
                        
                        AudioClip audioClip = await ConvertResponseToAudioClip(responseText, request);
                        
                        if (audioClip != null)
                        {
                            return new TTSResponse
                            {
                                success = true,
                                audioClip = audioClip,
                                error = null,
                                statusCode = (int)webRequest.responseCode,
                                responseTime = responseTime,
                                audioLength = audioClip.samples
                            };
                        }
                        else
                        {
                            return CreateErrorResponse("Failed to convert response to audio clip", (int)webRequest.responseCode, responseTime);
                        }
                    }
                    else
                    {
                        return CreateErrorResponse(
                            $"TTS request failed: {webRequest.error}", 
                            (int)webRequest.responseCode, 
                            responseTime
                        );
                    }
                }
            }
            catch (Exception e)
            {
                float responseTime = Time.time - startTime;
                return CreateErrorResponse($"Exception during TTS request: {e.Message}", 0, responseTime);
            }
        }
        
        private IEnumerator SendTTSRequestCoroutine(TTSRequest request, Action<TTSResponse> callback)
        {
            bool completed = false;
            TTSResponse result = default;
            
            // Start async operation
            _ = Task.Run(async () =>
            {
                try
                {
                    result = await SendTTSRequestWithRetry(request);
                }
                catch (Exception e)
                {
                    result = CreateErrorResponse($"Async TTS operation failed: {e.Message}", 0);
                }
                finally
                {
                    completed = true;
                }
            });
            
            // Wait for completion
            while (!completed)
            {
                yield return null;
            }
            
            callback?.Invoke(result);
        }
        
        #endregion
        
        #region JSON Processing
        
        private string BuildTTSRequestJson(TTSRequest request)
        {
            // Apply defaults for missing values
            var voiceName = !string.IsNullOrEmpty(request.voiceName) ? request.voiceName : defaultVoiceName;
            var languageCode = !string.IsNullOrEmpty(request.languageCode) ? request.languageCode : defaultLanguageCode;
            var speakingRate = request.speakingRate > 0 ? request.speakingRate : defaultSpeakingRate;
            var pitch = request.pitch != 0 ? request.pitch : defaultPitch;
            var volumeGain = request.volumeGain != 0 ? request.volumeGain : defaultVolumeGain;
            var encoding = request.audioEncoding != 0 ? request.audioEncoding : audioEncoding;
            var sampleRate = request.sampleRateHertz > 0 ? request.sampleRateHertz : sampleRateHertz;
            
            var requestObject = new
            {
                input = new
                {
                    text = request.text
                },
                voice = new
                {
                    languageCode = languageCode,
                    name = voiceName
                },
                audioConfig = new
                {
                    audioEncoding = encoding.ToString(),
                    sampleRateHertz = sampleRate,
                    speakingRate = speakingRate,
                    pitch = pitch,
                    volumeGainDb = volumeGain
                }
            };
            
            return JsonUtility.ToJson(requestObject);
        }
        
        private async Task<AudioClip> ConvertResponseToAudioClip(string responseJson, TTSRequest request)
        {
            try
            {
                var response = JsonUtility.FromJson<TTSAPIResponse>(responseJson);
                
                if (response?.audioContent != null)
                {
                    return await ConvertBase64ToAudioClip(response.audioContent, request);
                }
                
                LogError("TTS response parsing failed: No audio content found");
                return null;
            }
            catch (Exception e)
            {
                LogError($"TTS response parsing failed: {e.Message}");
                return null;
            }
        }
        
        private async Task<AudioClip> ConvertBase64ToAudioClip(string base64Audio, TTSRequest request)
        {
            try
            {
                // Decode base64 to byte array
                byte[] audioBytes = Convert.FromBase64String(base64Audio);
                
                // Process based on audio encoding
                switch (request.audioEncoding)
                {
                    case AudioEncoding.LINEAR16:
                        return await ConvertLinear16ToAudioClip(audioBytes, request);
                    case AudioEncoding.MP3:
                        return await ConvertMP3ToAudioClip(audioBytes, request);
                    case AudioEncoding.OGG_OPUS:
                        return await ConvertOggOpusToAudioClip(audioBytes, request);
                    default:
                        LogError($"Unsupported audio encoding: {request.audioEncoding}");
                        return null;
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to convert base64 to audio clip: {e.Message}");
                return null;
            }
        }
        
        private async Task<AudioClip> ConvertLinear16ToAudioClip(byte[] audioBytes, TTSRequest request)
        {
            try
            {
                // Convert bytes to float array for Unity AudioClip
                float[] floatArray = new float[audioBytes.Length / 2];
                
                for (int i = 0; i < floatArray.Length; i++)
                {
                    // Convert 16-bit signed integer to float (-1.0 to 1.0)
                    short sample = BitConverter.ToInt16(audioBytes, i * 2);
                    floatArray[i] = sample / 32768.0f;
                }
                
                // Create AudioClip
                int sampleRate = request.sampleRateHertz > 0 ? request.sampleRateHertz : sampleRateHertz;
                int channels = audioChannels;
                
                AudioClip clip = AudioClip.Create("TTSAudio", floatArray.Length / channels, channels, sampleRate, false);
                clip.SetData(floatArray, 0);
                
                await Task.Yield(); // Allow Unity to process the clip creation
                
                LogDebug($"Successfully created AudioClip: {clip.length}s, {clip.frequency}Hz, {clip.channels} channels");
                return clip;
            }
            catch (Exception e)
            {
                LogError($"Failed to convert LINEAR16 to AudioClip: {e.Message}");
                return null;
            }
        }
        
        private async Task<AudioClip> ConvertMP3ToAudioClip(byte[] audioBytes, TTSRequest request)
        {
            // Note: Unity doesn't natively support MP3 decoding at runtime
            // This would require a third-party library like NAudio or similar
            LogError("MP3 decoding is not supported in this implementation. Use LINEAR16 encoding instead.");
            await Task.Yield();
            return null;
        }
        
        private async Task<AudioClip> ConvertOggOpusToAudioClip(byte[] audioBytes, TTSRequest request)
        {
            // Note: Unity doesn't natively support OGG Opus decoding at runtime
            // This would require a third-party library
            LogError("OGG Opus decoding is not supported in this implementation. Use LINEAR16 encoding instead.");
            await Task.Yield();
            return null;
        }
        
        #endregion
        
        #region Error Handling
        
        private TTSResponse CreateErrorResponse(string error, int statusCode, float responseTime = 0f)
        {
            return new TTSResponse
            {
                success = false,
                audioClip = null,
                error = error,
                statusCode = statusCode,
                responseTime = responseTime,
                audioLength = 0
            };
        }
        
        private bool ShouldRetry(int statusCode)
        {
            // Retry on server errors and rate limits
            return statusCode >= 500 || statusCode == 429 || statusCode == 0;
        }
        
        #endregion
        
        #region Logging
        
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[GeminiTTSManager] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[GeminiTTSManager] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[GeminiTTSManager] {message}");
        }
        
        #endregion
        
        #region Data Structures
        
        [Serializable]
        private class TTSAPIResponse
        {
            public string audioContent;
        }
        
        #endregion
    }
}