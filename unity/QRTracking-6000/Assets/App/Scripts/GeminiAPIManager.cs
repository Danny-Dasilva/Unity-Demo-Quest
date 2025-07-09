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
    /// Singleton manager for Gemini API requests with multimodal support (audio, image, text).
    /// Handles authentication, retry logic, timeout handling, and proper error management.
    /// </summary>
    public class GeminiAPIManager : MonoBehaviour
    {
        private static GeminiAPIManager instance;
        
        /// <summary>
        /// Static singleton instance property
        /// </summary>
        public static GeminiAPIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Try to find existing instance
                    instance = FindObjectOfType<GeminiAPIManager>();
                    
                    if (instance == null)
                    {
                        // Create new instance
                        GameObject go = new GameObject("GeminiAPIManager");
                        instance = go.AddComponent<GeminiAPIManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        [Header("API Configuration")]
        [SerializeField] private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro-vision:generateContent";
        [SerializeField] private bool useEnvironmentKey = true;
        [SerializeField] private string apiKeyOverride; // For testing only
        
        [Header("Request Settings")]
        [SerializeField] private int maxRetries = 3;
        [SerializeField] private float requestTimeout = 30f;
        [SerializeField] private float retryDelay = 1f;
        
        [Header("Generation Config")]
        [SerializeField] private float temperature = 0.7f;
        [SerializeField] private int topK = 40;
        [SerializeField] private float topP = 0.95f;
        [SerializeField] private int maxOutputTokens = 1024;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool logRequestDetails = false;
        
        private string apiKey;
        private bool isInitialized = false;
        
        /// <summary>
        /// Structure for multimodal API requests containing audio, image, and text data
        /// </summary>
        [Serializable]
        public struct MultimodalRequest
        {
            public byte[] audioData;
            public string audioMimeType;
            public byte[] imageData;
            public string imageMimeType;
            public string textPrompt;
            public GenerationConfig generationConfig;
        }
        
        /// <summary>
        /// Configuration for AI generation parameters
        /// </summary>
        [Serializable]
        public struct GenerationConfig
        {
            public float temperature;
            public int topK;
            public float topP;
            public int maxOutputTokens;
            
            public static GenerationConfig Default => new GenerationConfig
            {
                temperature = 0.7f,
                topK = 40,
                topP = 0.95f,
                maxOutputTokens = 1024
            };
        }
        
        /// <summary>
        /// Response structure for API results
        /// </summary>
        [Serializable]
        public struct APIResponse
        {
            public bool success;
            public string content;
            public string error;
            public int statusCode;
            public float responseTime;
        }
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Ensure singleton pattern
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAPI();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (instance == this && !isInitialized)
            {
                InitializeAPI();
            }
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeAPI()
        {
            if (isInitialized) return;
            
            // Initialize API key based on priority order
            InitializeAuthentication();
            
            if (string.IsNullOrEmpty(apiKey))
            {
                LogError("API initialization failed - no API key available");
                return;
            }
            
            isInitialized = true;
            
            if (enableDebugLogs)
                Debug.Log("[GeminiAPIManager] Initialized successfully");
        }
        
        private void InitializeAuthentication()
        {
            // Priority order for API key acquisition
            
            // 1. Environment variable (recommended for production)
            if (useEnvironmentKey)
            {
                apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    LogDebug("Using environment variable API key");
                    return;
                }
            }
            
            // 2. PlayerPrefs (for development persistence)
            apiKey = PlayerPrefs.GetString("GeminiAPIKey", "");
            if (!string.IsNullOrEmpty(apiKey))
            {
                LogDebug("Using PlayerPrefs API key");
                return;
            }
            
            // 3. Inspector override (testing only)
            if (!string.IsNullOrEmpty(apiKeyOverride))
            {
                apiKey = apiKeyOverride;
                LogWarning("Using override API key - not recommended for production");
                return;
            }
            
            LogError("No API key found. Please set GEMINI_API_KEY environment variable or configure in PlayerPrefs");
        }
        
        #endregion
        
        #region Public API Methods
        
        /// <summary>
        /// Send a multimodal request to Gemini API asynchronously
        /// </summary>
        /// <param name="request">The multimodal request data</param>
        /// <returns>API response with content or error information</returns>
        public async Task<APIResponse> SendMultimodalRequestAsync(MultimodalRequest request)
        {
            if (!isInitialized)
            {
                return CreateErrorResponse("API not initialized", 0);
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                return CreateErrorResponse("No API key available", 401);
            }
            
            return await SendRequestWithRetry(request);
        }
        
        /// <summary>
        /// Send a multimodal request using coroutines (for non-async contexts)
        /// </summary>
        /// <param name="request">The multimodal request data</param>
        /// <param name="callback">Callback to handle the response</param>
        public void SendMultimodalRequest(MultimodalRequest request, Action<APIResponse> callback)
        {
            if (!isInitialized)
            {
                callback?.Invoke(CreateErrorResponse("API not initialized", 0));
                return;
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                callback?.Invoke(CreateErrorResponse("No API key available", 401));
                return;
            }
            
            StartCoroutine(SendRequestCoroutine(request, callback));
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
        /// Check if the API manager is properly initialized and ready
        /// </summary>
        /// <returns>True if ready to process requests</returns>
        public bool IsReady()
        {
            return isInitialized && !string.IsNullOrEmpty(apiKey);
        }
        
        #endregion
        
        #region Request Processing
        
        private async Task<APIResponse> SendRequestWithRetry(MultimodalRequest request)
        {
            APIResponse lastResponse = CreateErrorResponse("Unknown error", 0);
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    LogDebug($"Sending request attempt {attempt + 1}/{maxRetries}");
                    
                    var response = await SendSingleRequest(request);
                    
                    if (response.success)
                    {
                        LogDebug($"Request successful on attempt {attempt + 1}");
                        return response;
                    }
                    
                    lastResponse = response;
                    
                    // Check if we should retry based on status code
                    if (!ShouldRetry(response.statusCode))
                    {
                        LogDebug($"Not retrying due to status code: {response.statusCode}");
                        break;
                    }
                    
                    // Wait before retry (exponential backoff)
                    if (attempt < maxRetries - 1)
                    {
                        float delay = retryDelay * Mathf.Pow(2, attempt);
                        LogDebug($"Waiting {delay}s before retry...");
                        await Task.Delay(Mathf.RoundToInt(delay * 1000));
                    }
                }
                catch (Exception e)
                {
                    LogError($"Request attempt {attempt + 1} failed: {e.Message}");
                    lastResponse = CreateErrorResponse($"Exception: {e.Message}", 0);
                }
            }
            
            LogError($"All {maxRetries} attempts failed. Last error: {lastResponse.error}");
            return lastResponse;
        }
        
        private async Task<APIResponse> SendSingleRequest(MultimodalRequest request)
        {
            float startTime = Time.time;
            
            try
            {
                string requestJson = BuildRequestJson(request);
                
                if (logRequestDetails)
                {
                    LogDebug($"Request JSON: {requestJson}");
                }
                
                using (UnityWebRequest webRequest = new UnityWebRequest(apiEndpoint, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    
                    // Set headers
                    webRequest.SetRequestHeader("Content-Type", "application/json");
                    webRequest.SetRequestHeader("x-goog-api-key", apiKey);
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
                            LogDebug($"Response: {responseText}");
                        }
                        
                        string content = ParseResponseContent(responseText);
                        
                        if (!string.IsNullOrEmpty(content))
                        {
                            return new APIResponse
                            {
                                success = true,
                                content = content,
                                error = null,
                                statusCode = (int)webRequest.responseCode,
                                responseTime = responseTime
                            };
                        }
                        else
                        {
                            return CreateErrorResponse("Empty or invalid response content", (int)webRequest.responseCode, responseTime);
                        }
                    }
                    else
                    {
                        return CreateErrorResponse(
                            $"Request failed: {webRequest.error}", 
                            (int)webRequest.responseCode, 
                            responseTime
                        );
                    }
                }
            }
            catch (Exception e)
            {
                float responseTime = Time.time - startTime;
                return CreateErrorResponse($"Exception during request: {e.Message}", 0, responseTime);
            }
        }
        
        private IEnumerator SendRequestCoroutine(MultimodalRequest request, Action<APIResponse> callback)
        {
            bool completed = false;
            APIResponse result = default;
            
            // Start async operation
            _ = Task.Run(async () =>
            {
                try
                {
                    result = await SendRequestWithRetry(request);
                }
                catch (Exception e)
                {
                    result = CreateErrorResponse($"Async operation failed: {e.Message}", 0);
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
        
        private string BuildRequestJson(MultimodalRequest request)
        {
            var parts = new List<object>();
            
            // Add text prompt
            if (!string.IsNullOrEmpty(request.textPrompt))
            {
                parts.Add(new { text = request.textPrompt });
            }
            else
            {
                parts.Add(new { text = "Analyze what you see in this image and respond to my voice query. Be conversational and helpful." });
            }
            
            // Add image data
            if (request.imageData != null && request.imageData.Length > 0)
            {
                parts.Add(new {
                    inline_data = new {
                        mime_type = !string.IsNullOrEmpty(request.imageMimeType) ? request.imageMimeType : "image/png",
                        data = Convert.ToBase64String(request.imageData)
                    }
                });
            }
            
            // Add audio data
            if (request.audioData != null && request.audioData.Length > 0)
            {
                parts.Add(new {
                    inline_data = new {
                        mime_type = !string.IsNullOrEmpty(request.audioMimeType) ? request.audioMimeType : "audio/wav",
                        data = Convert.ToBase64String(request.audioData)
                    }
                });
            }
            
            // Use provided generation config or default
            var genConfig = request.generationConfig.temperature > 0 ? request.generationConfig : new GenerationConfig
            {
                temperature = this.temperature,
                topK = this.topK,
                topP = this.topP,
                maxOutputTokens = this.maxOutputTokens
            };
            
            var requestObject = new {
                contents = new[] {
                    new {
                        role = "user",
                        parts = parts.ToArray()
                    }
                },
                generationConfig = new {
                    temperature = genConfig.temperature,
                    topK = genConfig.topK,
                    topP = genConfig.topP,
                    maxOutputTokens = genConfig.maxOutputTokens
                }
            };
            
            return JsonUtility.ToJson(requestObject);
        }
        
        private string ParseResponseContent(string responseJson)
        {
            try
            {
                var response = JsonUtility.FromJson<GeminiResponse>(responseJson);
                
                if (response?.candidates != null && response.candidates.Length > 0)
                {
                    var candidate = response.candidates[0];
                    if (candidate?.content?.parts != null && candidate.content.parts.Length > 0)
                    {
                        return candidate.content.parts[0].text;
                    }
                }
                
                LogError("Response parsing failed: No valid content found");
                return null;
            }
            catch (Exception e)
            {
                LogError($"Response parsing failed: {e.Message}");
                return null;
            }
        }
        
        #endregion
        
        #region Error Handling
        
        private APIResponse CreateErrorResponse(string error, int statusCode, float responseTime = 0f)
        {
            return new APIResponse
            {
                success = false,
                content = null,
                error = error,
                statusCode = statusCode,
                responseTime = responseTime
            };
        }
        
        private bool ShouldRetry(int statusCode)
        {
            // Retry on server errors and rate limits
            return statusCode >= 500 || statusCode == 429 || statusCode == 0;
        }
        
        private void HandleError(string error, string context = "")
        {
            string fullMessage = string.IsNullOrEmpty(context) ? error : $"{context}: {error}";
            LogError(fullMessage);
        }
        
        #endregion
        
        #region Logging
        
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[GeminiAPIManager] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[GeminiAPIManager] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[GeminiAPIManager] {message}");
        }
        
        #endregion
        
        #region Data Structures
        
        [Serializable]
        private class GeminiResponse
        {
            public GeminiCandidate[] candidates;
        }
        
        [Serializable]
        private class GeminiCandidate
        {
            public GeminiContent content;
        }
        
        [Serializable]
        private class GeminiContent
        {
            public GeminiPart[] parts;
        }
        
        [Serializable]
        private class GeminiPart
        {
            public string text;
        }
        
        #endregion
    }
}