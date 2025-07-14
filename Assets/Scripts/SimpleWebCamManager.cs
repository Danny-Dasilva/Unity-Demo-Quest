using UnityEngine;

namespace QuestCameraTools.App
{
    /// <summary>
    /// Simple WebCam manager for Quest passthrough camera access
    /// Uses centralized WebCamTextureManager for proper Quest camera integration
    /// </summary>
    public class SimpleWebCamManager : MonoBehaviour
    {
        [Header("Camera Reference")]
        [SerializeField] private MonoBehaviour webCamTextureManager;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        public WebCamTexture WebCamTexture => GetWebCamTexture();
        public bool IsInitialized => webCamTextureManager != null && GetWebCamTexture() != null;
        public bool IsPlaying => WebCamTexture != null && WebCamTexture.isPlaying;
        
        private void Start()
        {
            ValidateSetup();
        }
        
        private void ValidateSetup()
        {
            if (webCamTextureManager == null)
            {
                LogError("WebCamTextureManager not assigned! Please drag the WebCamTextureManager from your scene to this component.");
                return;
            }
            
            LogDebug("WebCamTextureManager reference found. Camera will be managed by WebCamTextureManager.");
        }
        
        private WebCamTexture GetWebCamTexture()
        {
            if (webCamTextureManager == null) return null;
            
            // Use reflection to access the WebCamTexture property
            var property = webCamTextureManager.GetType().GetProperty("WebCamTexture");
            if (property != null)
            {
                return property.GetValue(webCamTextureManager) as WebCamTexture;
            }
            
            LogError("WebCamTexture property not found on assigned component. Make sure you assigned the correct WebCamTextureManager component.");
            return null;
        }
        
        
        /// <summary>
        /// Capture a screenshot from the current camera feed
        /// </summary>
        public byte[] CaptureScreenshot()
        {
            return CaptureScreenshot(true); // Default to saving images for validation
        }
        
        /// <summary>
        /// Capture a screenshot from the current camera feed with optional local storage saving
        /// </summary>
        /// <param name="saveToStorage">Whether to save the captured image to local storage for validation</param>
        public byte[] CaptureScreenshot(bool saveToStorage)
        {
            if (!IsInitialized || !IsPlaying)
            {
                LogError("Camera not initialized or not playing");
                return new byte[0];
            }
            
            LogDebug($"[GeminiVoiceToggle] CaptureScreenshot - WebCamTexture info: {WebCamTexture.width}x{WebCamTexture.height}, isPlaying: {WebCamTexture.isPlaying}, didUpdateThisFrame: {WebCamTexture.didUpdateThisFrame}");
            
            try
            {
                // Get the current frame
                var pixels = WebCamTexture.GetPixels32();
                LogDebug($"[GeminiVoiceToggle] GetPixels32 returned {pixels?.Length ?? 0} pixels");
                
                if (pixels == null || pixels.Length == 0)
                {
                    LogError("[GeminiVoiceToggle] GetPixels32 returned null or empty array");
                    return new byte[0];
                }
                
                // Check if pixels are all zero (black)
                int nonBlackPixels = 0;
                for (int i = 0; i < Mathf.Min(1000, pixels.Length); i++)
                {
                    if (pixels[i].r > 0 || pixels[i].g > 0 || pixels[i].b > 0)
                    {
                        nonBlackPixels++;
                    }
                }
                LogDebug($"[GeminiVoiceToggle] Pixel sample check: {nonBlackPixels}/1000 non-black pixels");
                
                // Create a texture from the pixels
                var texture = new Texture2D(WebCamTexture.width, WebCamTexture.height, TextureFormat.RGBA32, false);
                texture.SetPixels32(pixels);
                texture.Apply();
                
                // Encode to PNG
                var pngData = texture.EncodeToPNG();
                
                // Save to local storage if requested
                if (saveToStorage && pngData != null && pngData.Length > 0)
                {
                    SaveImageToStorage(pngData, WebCamTexture.width, WebCamTexture.height);
                }
                
                // Clean up
                DestroyImmediate(texture);
                
                LogDebug($"[GeminiVoiceToggle] Screenshot captured: {WebCamTexture.width}x{WebCamTexture.height}, {pngData.Length} bytes");
                return pngData;
            }
            catch (System.Exception e)
            {
                LogError($"[GeminiVoiceToggle] Failed to capture screenshot: {e.Message}");
                return new byte[0];
            }
        }
        
        /// <summary>
        /// Save captured image to Quest's local storage for validation
        /// </summary>
        private void SaveImageToStorage(byte[] pngData, int width, int height)
        {
            try
            {
                // Create filename with timestamp
                string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                string filename = $"quest_camera_capture_{timestamp}_{width}x{height}.png";
                
                // Use Application.persistentDataPath which is accessible on Quest
                string directoryPath = System.IO.Path.Combine(Application.persistentDataPath, "CameraCaptures");
                string fullPath = System.IO.Path.Combine(directoryPath, filename);
                
                // Ensure directory exists
                if (!System.IO.Directory.Exists(directoryPath))
                {
                    System.IO.Directory.CreateDirectory(directoryPath);
                    Debug.Log($"[GeminiVoiceToggle] Created directory: {directoryPath}");
                }
                
                // Save the PNG file
                System.IO.File.WriteAllBytes(fullPath, pngData);
                
                Debug.Log($"[GeminiVoiceToggle] Image saved to storage: {fullPath}");
                Debug.Log($"[GeminiVoiceToggle] Image details: {width}x{height}, {pngData.Length} bytes");
                Debug.Log($"[GeminiVoiceToggle] Directory location: {directoryPath}");
                
                // Also log the base64 encoded version for easy extraction via logs
                if (pngData.Length < 100000) // Only for reasonably sized images
                {
                    string base64 = System.Convert.ToBase64String(pngData);
                    Debug.Log($"[GeminiVoiceToggle] Base64 image data: data:image/png;base64,{base64}");
                }
                else
                {
                    Debug.Log($"[GeminiVoiceToggle] Image too large ({pngData.Length} bytes) for base64 logging");
                }
            }
            catch (System.Exception e)
            {
                LogError($"[GeminiVoiceToggle] Failed to save image to storage: {e.Message}");
            }
        }
        
        /// <summary>
        /// Check if camera is ready for capture
        /// </summary>
        public bool IsCameraReady()
        {
            bool ready = IsInitialized && IsPlaying;
            LogDebug($"[GeminiVoiceToggle] IsCameraReady - IsInitialized: {IsInitialized}, IsPlaying: {IsPlaying}, didUpdateThisFrame: {WebCamTexture?.didUpdateThisFrame ?? false}");
            // Don't require didUpdateThisFrame as it might not be set immediately
            return ready;
        }
        
        /// <summary>
        /// Get current camera resolution
        /// </summary>
        public Vector2Int GetResolution()
        {
            if (!IsInitialized)
                return Vector2Int.zero;
            
            return new Vector2Int(WebCamTexture.width, WebCamTexture.height);
        }
        
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[SimpleWebCamManager] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[SimpleWebCamManager] {message}");
        }
    }
}