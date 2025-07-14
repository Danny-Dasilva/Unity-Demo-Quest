using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace QuestCameraTools.App
{
    /// <summary>
    /// Service manager for Google Cloud Text-to-Speech API integration.
    /// Handles TTS requests, audio clip conversion, and voice configuration.
    /// Supports both async/await and coroutine patterns for Unity compatibility.
    /// </summary>
    public class GeminiTTSManager : MonoBehaviour
    {
        [Header("TTS Configuration")]
        [SerializeField] private string ttsModel = "gemini-2.5-flash-preview-tts";
        [SerializeField] private string ttsEndpoint = "streamGenerateContent";
        [SerializeField] private string defaultVoiceName = "Zephyr";
        [SerializeField] private string defaultLanguageCode = "en-US";
        [SerializeField] private float defaultSpeakingRate = 1.0f;
        [SerializeField] private float defaultPitch = 0.0f;
        [SerializeField] private float defaultVolumeGain = 0.8f;
        
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
                    voiceName = "Zephyr",
                    speakingRate = 1.0f,
                    pitch = 0.0f,
                    volumeGain = 0.8f,
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
            Debug.Log("[GeminiVoiceToggle] InitializeTTS called");
            
            if (isInitialized) {
                Debug.Log("[GeminiVoiceToggle] TTS already initialized, skipping");
                return;
            }
            
            // Initialize API key from the same sources as GeminiAPIManager
            Debug.Log("[GeminiVoiceToggle] Starting authentication initialization");
            InitializeAuthentication();
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[GeminiVoiceToggle] TTS initialization failed - no API key available");
                LogError("TTS initialization failed - no API key available");
                return;
            }
            
            isInitialized = true;
            Debug.Log("[GeminiVoiceToggle] TTS initialization completed successfully");
            
            if (enableDebugLogs)
                Debug.Log("[GeminiTTSManager] Initialized successfully");
        }
        
        private void InitializeAuthentication()
        {
            Debug.Log("[GeminiVoiceToggle] Starting API key acquisition");
            
            // Priority order for API key acquisition (same as GeminiAPIManager)
            
            // 1. Environment variable (recommended for production)
            Debug.Log("[GeminiVoiceToggle] Checking environment variable GEMINI_API_KEY");
            apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                Debug.Log("[GeminiVoiceToggle] Found API key in environment variable");
                LogDebug("Using environment variable API key");
                return;
            }
            
            // 2. PlayerPrefs (for development persistence)
            Debug.Log("[GeminiVoiceToggle] Checking PlayerPrefs for GeminiAPIKey");
            apiKey = PlayerPrefs.GetString("GeminiAPIKey", "");
            if (!string.IsNullOrEmpty(apiKey))
            {
                Debug.Log("[GeminiVoiceToggle] Found API key in PlayerPrefs");
                LogDebug("Using PlayerPrefs API key");
                return;
            }
            
            Debug.LogError("[GeminiVoiceToggle] No API key found in any location");
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
            Debug.Log($"[GeminiVoiceToggle] ConvertTextToSpeechAsync called with text: '{text}'");
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
            Debug.Log($"[GeminiVoiceToggle] ConvertTextToSpeechAsync called with TTSRequest - text: '{request.text}', voice: '{request.voiceName}'");
            
            if (!isInitialized)
            {
                Debug.LogError("[GeminiVoiceToggle] TTS not initialized when trying to convert text");
                return CreateErrorResponse("TTS not initialized", 0);
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[GeminiVoiceToggle] No API key available when trying to convert text");
                return CreateErrorResponse("No API key available", 401);
            }
            
            if (string.IsNullOrEmpty(request.text))
            {
                Debug.LogError("[GeminiVoiceToggle] No text provided for TTS conversion");
                return CreateErrorResponse("No text provided", 400);
            }
            
            Debug.Log("[GeminiVoiceToggle] All validation checks passed, sending TTS request");
            return await SendTTSRequestWithRetry(request);
        }
        
        /// <summary>
        /// Convert text to speech using coroutines (for non-async contexts)
        /// </summary>
        /// <param name="text">Text to convert to speech</param>
        /// <param name="callback">Callback to handle the response</param>
        public void ConvertTextToSpeech(string text, Action<TTSResponse> callback)
        {
            Debug.Log($"[GeminiVoiceToggle] ConvertTextToSpeech (coroutine) called with text: '{text}'");
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
            Debug.Log($"[GeminiVoiceToggle] ConvertTextToSpeech (coroutine) called with TTSRequest - text: '{request.text}', voice: '{request.voiceName}'");
            
            if (!isInitialized)
            {
                Debug.LogError("[GeminiVoiceToggle] TTS not initialized when trying to convert text (coroutine)");
                callback?.Invoke(CreateErrorResponse("TTS not initialized", 0));
                return;
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[GeminiVoiceToggle] No API key available when trying to convert text (coroutine)");
                callback?.Invoke(CreateErrorResponse("No API key available", 401));
                return;
            }
            
            if (string.IsNullOrEmpty(request.text))
            {
                Debug.LogError("[GeminiVoiceToggle] No text provided for TTS conversion (coroutine)");
                callback?.Invoke(CreateErrorResponse("No text provided", 400));
                return;
            }
            
            Debug.Log("[GeminiVoiceToggle] All validation checks passed, starting TTS coroutine");
            StartCoroutine(SendTTSRequestCoroutine(request, callback));
        }
        
        /// <summary>
        /// Set API key at runtime (useful for dynamic configuration)
        /// </summary>
        /// <param name="key">The API key to set</param>
        /// <param name="persistToPlayerPrefs">Whether to save to PlayerPrefs</param>
        public void SetAPIKey(string key, bool persistToPlayerPrefs = false)
        {
            Debug.Log($"[GeminiVoiceToggle] SetAPIKey called, persistToPlayerPrefs: {persistToPlayerPrefs}");
            apiKey = key;
            
            if (persistToPlayerPrefs)
            {
                Debug.Log("[GeminiVoiceToggle] Saving API key to PlayerPrefs");
                PlayerPrefs.SetString("GeminiAPIKey", key);
                PlayerPrefs.Save();
            }
            
            // Re-initialize if we weren't initialized before and now have an API key
            if (!isInitialized && !string.IsNullOrEmpty(apiKey))
            {
                isInitialized = true;
                Debug.Log("[GeminiVoiceToggle] TTS initialized after API key was set");
            }
            
            Debug.Log("[GeminiVoiceToggle] API key updated successfully");
            LogDebug("API key updated");
        }
        
        /// <summary>
        /// Check if the TTS manager is properly initialized and ready
        /// </summary>
        /// <returns>True if ready to process requests</returns>
        public bool IsReady()
        {
            bool ready = isInitialized && !string.IsNullOrEmpty(apiKey);
            Debug.Log($"[GeminiVoiceToggle] IsReady check: initialized={isInitialized}, hasApiKey={!string.IsNullOrEmpty(apiKey)}, ready={ready}");
            return ready;
        }
        
        #endregion
        
        #region Request Processing
        
        private async Task<TTSResponse> SendTTSRequestWithRetry(TTSRequest request)
        {
            Debug.Log($"[GeminiVoiceToggle] SendTTSRequestWithRetry started for text: '{request.text}'");
            TTSResponse lastResponse = CreateErrorResponse("Unknown error", 0);
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    Debug.Log($"[GeminiVoiceToggle] Sending TTS request attempt {attempt + 1}/{maxRetries}");
                    LogDebug($"Sending TTS request attempt {attempt + 1}/{maxRetries}");
                    
                    var response = await SendSingleTTSRequest(request);
                    
                    if (response.success)
                    {
                        Debug.Log($"[GeminiVoiceToggle] TTS request successful on attempt {attempt + 1}");
                        LogDebug($"TTS request successful on attempt {attempt + 1}");
                        return response;
                    }
                    
                    Debug.LogWarning($"[GeminiVoiceToggle] TTS request attempt {attempt + 1} failed: {response.error}");
                    lastResponse = response;
                    
                    // Check if we should retry based on status code
                    if (!ShouldRetry(response.statusCode))
                    {
                        Debug.LogWarning($"[GeminiVoiceToggle] Not retrying TTS due to status code: {response.statusCode}");
                        LogDebug($"Not retrying TTS due to status code: {response.statusCode}");
                        break;
                    }
                    
                    // Wait before retry (exponential backoff)
                    if (attempt < maxRetries - 1)
                    {
                        float delay = retryDelay * Mathf.Pow(2, attempt);
                        Debug.Log($"[GeminiVoiceToggle] Waiting {delay}s before TTS retry...");
                        LogDebug($"Waiting {delay}s before TTS retry...");
                        await Task.Delay(Mathf.RoundToInt(delay * 1000));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GeminiVoiceToggle] TTS request attempt {attempt + 1} failed with exception: {e.Message}");
                    LogError($"TTS request attempt {attempt + 1} failed: {e.Message}");
                    lastResponse = CreateErrorResponse($"Exception: {e.Message}", 0);
                }
            }
            
            Debug.LogError($"[GeminiVoiceToggle] All {maxRetries} TTS attempts failed. Last error: {lastResponse.error}");
            LogError($"All {maxRetries} TTS attempts failed. Last error: {lastResponse.error}");
            return lastResponse;
        }
        
        private async Task<TTSResponse> SendSingleTTSRequest(TTSRequest request)
        {
            Debug.Log($"[GeminiVoiceToggle] SendSingleTTSRequest started for text: '{request.text}'");
            float startTime = Time.time;
            
            try
            {
                Debug.Log($"[GeminiVoiceToggle] Building TTS request JSON");
                string requestJson = BuildTTSRequestJson(request);
                
                if (logRequestDetails)
                {
                    Debug.Log($"[GeminiVoiceToggle] TTS Request JSON: {requestJson}");
                    LogDebug($"TTS Request JSON: {requestJson}");
                }
                
                string fullUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{ttsModel}:{ttsEndpoint}?key={apiKey}";
                Debug.Log($"[GeminiVoiceToggle] Creating UnityWebRequest to endpoint: {fullUrl}");
                using (UnityWebRequest webRequest = new UnityWebRequest(fullUrl, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    
                    // Set headers
                    webRequest.SetRequestHeader("Content-Type", "application/json");
                    webRequest.timeout = Mathf.RoundToInt(requestTimeout);
                    
                    Debug.Log($"[GeminiVoiceToggle] Sending web request with timeout: {requestTimeout}s");
                    
                    // Send request
                    var operation = webRequest.SendWebRequest();
                    
                    // Wait for completion
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    float responseTime = Time.time - startTime;
                    Debug.Log($"[GeminiVoiceToggle] Request completed in {responseTime}s with result: {webRequest.result}");
                    
                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        string responseText = webRequest.downloadHandler.text;
                        Debug.Log($"[GeminiVoiceToggle] Received successful response, length: {responseText.Length} characters");
                        
                        if (logRequestDetails)
                        {
                            Debug.Log($"[GeminiVoiceToggle] TTS Response: {responseText}");
                            LogDebug($"TTS Response: {responseText}");
                        }
                        
                        Debug.Log($"[GeminiVoiceToggle] Converting response to audio clip");
                        AudioClip audioClip = await ConvertResponseToAudioClip(responseText, request);
                        
                        if (audioClip != null)
                        {
                            Debug.Log($"[GeminiVoiceToggle] Successfully created audio clip: {audioClip.length}s, {audioClip.samples} samples");
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
                            Debug.LogError($"[GeminiVoiceToggle] Failed to convert response to audio clip");
                            return CreateErrorResponse("Failed to convert response to audio clip", (int)webRequest.responseCode, responseTime);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[GeminiVoiceToggle] Web request failed: {webRequest.error}, Response code: {webRequest.responseCode}");
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
                Debug.LogError($"[GeminiVoiceToggle] Exception during TTS request: {e.Message}\nStack trace: {e.StackTrace}");
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
            Debug.Log($"[GeminiVoiceToggle] Building TTS request JSON for text: '{request.text}'");
            
            // Apply defaults for missing values
            var voiceName = !string.IsNullOrEmpty(request.voiceName) ? request.voiceName : defaultVoiceName;
            var languageCode = !string.IsNullOrEmpty(request.languageCode) ? request.languageCode : defaultLanguageCode;
            var speakingRate = request.speakingRate > 0 ? request.speakingRate : defaultSpeakingRate;
            var pitch = request.pitch != 0 ? request.pitch : defaultPitch;
            var volumeGain = request.volumeGain != 0 ? request.volumeGain : defaultVolumeGain;
            var encoding = request.audioEncoding != 0 ? request.audioEncoding : audioEncoding;
            var sampleRate = request.sampleRateHertz > 0 ? request.sampleRateHertz : sampleRateHertz;
            
            Debug.Log($"[GeminiVoiceToggle] Request parameters: voice='{voiceName}', lang='{languageCode}', rate={speakingRate}, pitch={pitch}, encoding={encoding}, sampleRate={sampleRate}");
            
            // Build JSON manually to match Gemini TTS API format (corrected structure)
            var jsonBuilder = new System.Text.StringBuilder();
            jsonBuilder.Append("{\"contents\":[{\"role\":\"user\",\"parts\":[{\"text\":\"");
            jsonBuilder.Append(request.text.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r"));
            jsonBuilder.Append("\"}]}],\"generationConfig\":{");
            jsonBuilder.Append("\"responseModalities\":[\"audio\"],");
            jsonBuilder.Append("\"temperature\":1,");
            jsonBuilder.Append("\"speech_config\":{");
            jsonBuilder.Append("\"voice_config\":{");
            jsonBuilder.Append("\"prebuilt_voice_config\":{");
            jsonBuilder.Append("\"voice_name\":\"");
            jsonBuilder.Append(voiceName);
            jsonBuilder.Append("\"}}}}}");
            
            string json = jsonBuilder.ToString();
            Debug.Log($"[GeminiVoiceToggle] Built JSON request, length: {json.Length} characters");
            return json;
        }
        
        private async Task<AudioClip> ConvertResponseToAudioClip(string responseJson, TTSRequest request)
        {
            Debug.Log($"[GeminiVoiceToggle] ConvertResponseToAudioClip starting");
            try
            {
                Debug.Log($"[GeminiVoiceToggle] Parsing JSON response, length: {responseJson.Length} characters");
                Debug.Log($"[GeminiVoiceToggle] Response preview: {responseJson.Substring(0, Mathf.Min(200, responseJson.Length))}...");
                
                // The response structure is: candidates[0].content.parts[0].inlineData.data
                // First find the inlineData block
                int inlineDataStart = responseJson.IndexOf("\"inlineData\":");
                if (inlineDataStart == -1)
                {
                    Debug.LogError($"[GeminiVoiceToggle] No inlineData found in response");
                    LogError("TTS response parsing failed: No inlineData found");
                    return null;
                }
                
                Debug.Log($"[GeminiVoiceToggle] Found inlineData at position {inlineDataStart}");
                
                // Find the opening brace after inlineData
                int braceStart = responseJson.IndexOf("{", inlineDataStart);
                if (braceStart == -1)
                {
                    Debug.LogError($"[GeminiVoiceToggle] No opening brace after inlineData");
                    LogError("TTS response parsing failed: Malformed inlineData");
                    return null;
                }
                
                // Find the "data" field within the inlineData object
                int dataFieldStart = responseJson.IndexOf("\"data\":", braceStart);
                if (dataFieldStart == -1)
                {
                    Debug.LogError($"[GeminiVoiceToggle] No data field in inlineData");
                    LogError("TTS response parsing failed: No data field in inlineData");
                    return null;
                }
                
                // Find the opening quote for the data value
                int dataValueStart = responseJson.IndexOf("\"", dataFieldStart + 7); // 7 = length of "data":
                if (dataValueStart == -1)
                {
                    Debug.LogError($"[GeminiVoiceToggle] No opening quote for data value");
                    LogError("TTS response parsing failed: Malformed data field");
                    return null;
                }
                dataValueStart += 1; // Skip the opening quote
                
                // Find the closing quote for the data value
                int dataValueEnd = dataValueStart;
                while (dataValueEnd < responseJson.Length)
                {
                    int nextQuote = responseJson.IndexOf("\"", dataValueEnd);
                    if (nextQuote == -1)
                    {
                        Debug.LogError($"[GeminiVoiceToggle] No closing quote for data value");
                        LogError("TTS response parsing failed: Unterminated data field");
                        return null;
                    }
                    
                    // Check if this quote is escaped
                    if (nextQuote > 0 && responseJson[nextQuote - 1] == '\\')
                    {
                        dataValueEnd = nextQuote + 1;
                        continue;
                    }
                    
                    dataValueEnd = nextQuote;
                    break;
                }
                
                string audioData = responseJson.Substring(dataValueStart, dataValueEnd - dataValueStart);
                Debug.Log($"[GeminiVoiceToggle] Found audio data in response, length: {audioData.Length} characters");
                Debug.Log($"[GeminiVoiceToggle] Audio data preview: {audioData.Substring(0, Mathf.Min(50, audioData.Length))}...");
                
                return await ConvertBase64ToAudioClip(audioData, request);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GeminiVoiceToggle] TTS response parsing failed: {e.Message}\nStack trace: {e.StackTrace}");
                LogError($"TTS response parsing failed: {e.Message}");
                return null;
            }
        }
        
        private async Task<AudioClip> ConvertBase64ToAudioClip(string base64Audio, TTSRequest request)
        {
            Debug.Log($"[GeminiVoiceToggle] ConvertBase64ToAudioClip starting with encoding: {request.audioEncoding}");
            try
            {
                // Decode base64 to byte array
                Debug.Log($"[GeminiVoiceToggle] Decoding base64 audio data, length: {base64Audio.Length} characters");
                byte[] audioBytes = Convert.FromBase64String(base64Audio);
                Debug.Log($"[GeminiVoiceToggle] Successfully decoded to {audioBytes.Length} bytes");
                
                // Gemini TTS API returns audio/L16 format which is LINEAR16 PCM at 24kHz
                // Override the request settings to match the actual response format
                var geminiRequest = new TTSRequest
                {
                    text = request.text,
                    audioEncoding = AudioEncoding.LINEAR16,
                    sampleRateHertz = 24000, // Gemini TTS uses 24kHz
                    languageCode = request.languageCode,
                    voiceName = request.voiceName,
                    speakingRate = request.speakingRate,
                    pitch = request.pitch,
                    volumeGain = request.volumeGain
                };
                
                Debug.Log($"[GeminiVoiceToggle] Converting Gemini TTS audio (LINEAR16 PCM, 24kHz) to AudioClip");
                return await ConvertLinear16ToAudioClip(audioBytes, geminiRequest);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GeminiVoiceToggle] Failed to convert base64 to audio clip: {e.Message}\nStack trace: {e.StackTrace}");
                LogError($"Failed to convert base64 to audio clip: {e.Message}");
                return null;
            }
        }
        
        private async Task<AudioClip> ConvertLinear16ToAudioClip(byte[] audioBytes, TTSRequest request)
        {
            Debug.Log($"[GeminiVoiceToggle] ConvertLinear16ToAudioClip starting with {audioBytes.Length} bytes");
            try
            {
                // Convert bytes to float array for Unity AudioClip
                float[] floatArray = new float[audioBytes.Length / 2];
                Debug.Log($"[GeminiVoiceToggle] Converting to float array of length: {floatArray.Length}");
                
                for (int i = 0; i < floatArray.Length; i++)
                {
                    // Convert 16-bit signed integer to float (-1.0 to 1.0)
                    short sample = BitConverter.ToInt16(audioBytes, i * 2);
                    floatArray[i] = sample / 32768.0f;
                }
                
                // Create AudioClip
                int sampleRate = request.sampleRateHertz > 0 ? request.sampleRateHertz : sampleRateHertz;
                int channels = audioChannels;
                
                Debug.Log($"[GeminiVoiceToggle] Creating AudioClip with sampleRate: {sampleRate}, channels: {channels}, samples: {floatArray.Length}");
                AudioClip clip = AudioClip.Create("TTSAudio", floatArray.Length / channels, channels, sampleRate, false);
                clip.SetData(floatArray, 0);
                
                // Remove unnecessary delay for faster audio processing
                
                Debug.Log($"[GeminiVoiceToggle] Successfully created AudioClip: {clip.length}s, {clip.frequency}Hz, {clip.channels} channels");
                LogDebug($"Successfully created AudioClip: {clip.length}s, {clip.frequency}Hz, {clip.channels} channels");
                return clip;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GeminiVoiceToggle] Failed to convert LINEAR16 to AudioClip: {e.Message}\nStack trace: {e.StackTrace}");
                LogError($"Failed to convert LINEAR16 to AudioClip: {e.Message}");
                return null;
            }
        }
        
        private async Task<AudioClip> ConvertMP3ToAudioClip(byte[] audioBytes, TTSRequest request)
        {
            // Note: Unity doesn't natively support MP3 decoding at runtime
            // This would require a third-party library like NAudio or similar
            LogError("MP3 decoding is not supported in this implementation. Use LINEAR16 encoding instead.");
            return null;
        }
        
        private async Task<AudioClip> ConvertOggOpusToAudioClip(byte[] audioBytes, TTSRequest request)
        {
            // Note: Unity doesn't natively support OGG Opus decoding at runtime
            // This would require a third-party library
            LogError("OGG Opus decoding is not supported in this implementation. Use LINEAR16 encoding instead.");
            return null;
        }
        
        #endregion
        
        #region Error Handling
        
        private TTSResponse CreateErrorResponse(string error, int statusCode, float responseTime = 0f)
        {
            Debug.LogError($"[GeminiVoiceToggle] Creating error response: {error} (Status: {statusCode})");
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