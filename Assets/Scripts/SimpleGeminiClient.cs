using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace QuestCameraTools.App
{
    /// <summary>
    /// Simple, self-contained Gemini API client for Unity
    /// Handles text, image, and audio multimodal requests
    /// </summary>
    public class SimpleGeminiClient : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string apiKey = "";
        [SerializeField] private string model = "gemini-2.5-flash";
        [SerializeField] private float requestTimeout = 30f;
        [SerializeField] private int maxRetries = 3;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        private const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models";
        
        [Serializable]
        public class GeminiRequest
        {
            public string textPrompt;
            public byte[] imageData;
            public byte[] audioData;
            public string imageMimeType = "image/png";
            public string audioMimeType = "audio/wav";
        }
        
        [Serializable]
        public class GeminiResponse
        {
            public bool success;
            public string content;
            public string error;
            public int statusCode;
            public float responseTime;
        }
        
        private void Start()
        {
            InitializeClient();
        }
        
        private void InitializeClient()
        {
            // Get API key from multiple sources
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                {
                    apiKey = PlayerPrefs.GetString("GeminiAPIKey", "");
                }
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                LogError("No API key found! Set GEMINI_API_KEY environment variable or assign in inspector.");
            }
            else
            {
                LogDebug("Gemini client initialized successfully");
            }
        }
        
        /// <summary>
        /// Send a multimodal request to Gemini API
        /// </summary>
        public void SendRequest(GeminiRequest request, Action<GeminiResponse> callback)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                callback?.Invoke(CreateErrorResponse("No API key available", 401));
                return;
            }
            
            StartCoroutine(SendRequestCoroutine(request, callback));
        }
        
        private IEnumerator SendRequestCoroutine(GeminiRequest request, Action<GeminiResponse> callback)
        {
            float startTime = Time.time;
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                LogDebug($"[GeminiVoiceToggle] Sending Gemini request attempt {attempt + 1}/{maxRetries}");
                
                var jsonData = BuildRequestJson(request);
                var url = $"{BASE_URL}/{model}:streamGenerateContent?key={apiKey}";
                
                // Log the request details (using LogError to ensure visibility)
                Debug.LogError($"[GeminiVoiceToggle] [SimpleGeminiClient] Request URL: {apiKey} | JSON size: {jsonData.Length} bytes | JSON: {jsonData.Substring(0, Math.Min(500, jsonData.Length))}...");
                
                using (var webRequest = new UnityWebRequest(url, "POST"))
                {
                    webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    webRequest.SetRequestHeader("Content-Type", "application/json");
                    webRequest.timeout = Mathf.RoundToInt(requestTimeout);
                    
                    yield return webRequest.SendWebRequest();
                    
                    float responseTime = Time.time - startTime;
                    
                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        var responseText = webRequest.downloadHandler.text;
                        LogDebug($"[GeminiVoiceToggle] Gemini API response received: {responseText.Substring(0, Math.Min(200, responseText.Length))}...");
                        
                        var parsedResponse = ParseGeminiResponse(responseText);
                        
                        if (!string.IsNullOrEmpty(parsedResponse))
                        {
                            callback?.Invoke(new GeminiResponse
                            {
                                success = true,
                                content = parsedResponse,
                                error = null,
                                statusCode = (int)webRequest.responseCode,
                                responseTime = responseTime
                            });
                            yield break;
                        }
                        else
                        {
                            LogError("[GeminiVoiceToggle] Failed to parse Gemini response");
                        }
                    }
                    else
                    {
                        string errorResponse = webRequest.downloadHandler?.text ?? "No response body";
                        LogError($"[GeminiVoiceToggle] API request failed: {webRequest.error} | Response code: {webRequest.responseCode} | Response body: {errorResponse}");
                        
                        // Check if we should retry
                        if (!ShouldRetry((int)webRequest.responseCode) || attempt == maxRetries - 1)
                        {
                            callback?.Invoke(CreateErrorResponse(
                                $"Request failed: {webRequest.error}. Response: {errorResponse}",
                                (int)webRequest.responseCode,
                                responseTime
                            ));
                            yield break;
                        }
                    }
                }
                
                // Wait before retry
                if (attempt < maxRetries - 1)
                {
                    float delay = Mathf.Pow(2, attempt); // Exponential backoff
                    LogDebug($"[GeminiVoiceToggle] Waiting {delay}s before retry...");
                    yield return new WaitForSeconds(delay);
                }
            }
            
            callback?.Invoke(CreateErrorResponse("[GeminiVoiceToggle] All retry attempts failed", 0, Time.time - startTime));
        }
        
        private string BuildRequestJson(GeminiRequest request)
        {
            // Build JSON manually to avoid JsonUtility issues with anonymous objects
            var jsonBuilder = new System.Text.StringBuilder();
            jsonBuilder.Append("{\"contents\":[{\"role\":\"user\",\"parts\":[");
            
            bool hasContent = false;
            
            // Add text prompt
            if (!string.IsNullOrEmpty(request.textPrompt))
            {
                if (hasContent) jsonBuilder.Append(",");
                jsonBuilder.Append("{\"text\":\"");
                jsonBuilder.Append(request.textPrompt.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r"));
                jsonBuilder.Append("\"}");
                hasContent = true;
            }
            
            // Add image data
            if (request.imageData != null && request.imageData.Length > 0)
            {
                if (hasContent) jsonBuilder.Append(",");
                jsonBuilder.Append("{\"inline_data\":{\"mime_type\":\"");
                jsonBuilder.Append(request.imageMimeType);
                jsonBuilder.Append("\",\"data\":\"");
                jsonBuilder.Append(Convert.ToBase64String(request.imageData));
                jsonBuilder.Append("\"}}");
                hasContent = true;
            }
            
            // Add audio data
            if (request.audioData != null && request.audioData.Length > 0)
            {
                if (hasContent) jsonBuilder.Append(",");
                jsonBuilder.Append("{\"inline_data\":{\"mime_type\":\"");
                jsonBuilder.Append(request.audioMimeType);
                jsonBuilder.Append("\",\"data\":\"");
                jsonBuilder.Append(Convert.ToBase64String(request.audioData));
                jsonBuilder.Append("\"}}");
                hasContent = true;
            }
            
            jsonBuilder.Append("]}],\"generationConfig\":{");
            jsonBuilder.Append("\"thinkingConfig\":{\"thinkingBudget\":-1},");
            jsonBuilder.Append("\"responseMimeType\":\"text/plain\",");
            jsonBuilder.Append("\"temperature\":0.7,");
            jsonBuilder.Append("\"topK\":40,");
            jsonBuilder.Append("\"topP\":0.95,");
            jsonBuilder.Append("\"maxOutputTokens\":1024");
            jsonBuilder.Append("}}");
            
            return jsonBuilder.ToString();
        }
        
        private string ParseGeminiResponse(string jsonResponse)
        {
            try
            {
                // Handle streaming response format - may contain multiple JSON objects
                // Look for the first text response in the stream
                var textIndex = jsonResponse.IndexOf("\"text\":");
                if (textIndex != -1)
                {
                    var startIndex = jsonResponse.IndexOf("\"", textIndex + 7) + 1;
                    var endIndex = jsonResponse.IndexOf("\"", startIndex);
                    
                    // Handle escaped quotes within the text
                    while (endIndex > 0 && jsonResponse[endIndex - 1] == '\\')
                    {
                        endIndex = jsonResponse.IndexOf("\"", endIndex + 1);
                    }
                    
                    if (startIndex > 0 && endIndex > startIndex)
                    {
                        var text = jsonResponse.Substring(startIndex, endIndex - startIndex);
                        // Unescape common JSON escape sequences
                        text = text.Replace("\\n", "\n")
                                  .Replace("\\\"", "\"")
                                  .Replace("\\\\", "\\")
                                  .Replace("\\r", "\r")
                                  .Replace("\\t", "\t");
                        
                        // Combine multiple text parts if streaming response contains multiple chunks
                        var allText = text;
                        var nextTextIndex = jsonResponse.IndexOf("\"text\":", endIndex);
                        while (nextTextIndex != -1)
                        {
                            startIndex = jsonResponse.IndexOf("\"", nextTextIndex + 7) + 1;
                            endIndex = jsonResponse.IndexOf("\"", startIndex);
                            while (endIndex > 0 && jsonResponse[endIndex - 1] == '\\')
                            {
                                endIndex = jsonResponse.IndexOf("\"", endIndex + 1);
                            }
                            
                            if (startIndex > 0 && endIndex > startIndex)
                            {
                                text = jsonResponse.Substring(startIndex, endIndex - startIndex);
                                text = text.Replace("\\n", "\n")
                                          .Replace("\\\"", "\"")
                                          .Replace("\\\\", "\\")
                                          .Replace("\\r", "\r")
                                          .Replace("\\t", "\t");
                                allText += text;
                            }
                            nextTextIndex = jsonResponse.IndexOf("\"text\":", endIndex);
                        }
                        
                        return allText;
                    }
                }
            }
            catch (Exception e)
            {
                LogError($"[GeminiVoiceToggle] Error parsing Gemini response: {e.Message}");
            }
            
            return "";
        }
        
        private bool ShouldRetry(int statusCode)
        {
            // Retry on server errors and rate limits
            return statusCode >= 500 || statusCode == 429 || statusCode == 0;
        }
        
        private GeminiResponse CreateErrorResponse(string error, int statusCode, float responseTime = 0f)
        {
            return new GeminiResponse
            {
                success = false,
                content = null,
                error = error,
                statusCode = statusCode,
                responseTime = responseTime
            };
        }
        
        public bool IsReady()
        {
            return !string.IsNullOrEmpty(apiKey);
        }
        
        public void SetAPIKey(string key)
        {
            apiKey = key;
            LogDebug("[GeminiVoiceToggle] API key updated");
        }
        
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[GeminiVoiceToggle] [SimpleGeminiClient] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[GeminiVoiceToggle] [SimpleGeminiClient] {message}");
        }
    }
}