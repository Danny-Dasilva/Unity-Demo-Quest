using System;
using UnityEngine;
using PassthroughCameraSamples;

namespace QuestCameraTools.Utilities
{
    /// <summary>
    /// Utility class for capturing screenshots from Quest passthrough camera feed.
    /// Integrates with WebCamTextureManager to access camera data and provides
    /// optimized image capture for API usage.
    /// </summary>
    public class PassthroughScreenshotCapture : MonoBehaviour
    {
        [Header("Image Optimization")]
        [SerializeField] private int maxImageSize = 1024;
        [SerializeField] private bool enableImageResize = true;
        [SerializeField] private TextureFormat textureFormat = TextureFormat.RGBA32;
        
        [Header("References")]
        [SerializeField] private WebCamTextureManager webCamTextureManager;
        
        private const string LOG_PREFIX = "[PassthroughScreenshotCapture]";
        
        /// <summary>
        /// Data structure containing screenshot information
        /// </summary>
        public struct ScreenshotData
        {
            public byte[] pngData;
            public int width;
            public int height;
            public DateTime timestamp;
            public bool isValid;
        }
        
        private void Awake()
        {
            // Try to find WebCamTextureManager if not assigned
            if (webCamTextureManager == null)
            {
                webCamTextureManager = FindObjectOfType<WebCamTextureManager>();
                if (webCamTextureManager == null)
                {
                    Debug.LogError($"{LOG_PREFIX} WebCamTextureManager not found in scene. Please assign one or ensure it exists.");
                }
            }
        }
        
        /// <summary>
        /// Captures a screenshot from the current passthrough camera feed
        /// </summary>
        /// <returns>ScreenshotData containing PNG data and metadata</returns>
        public ScreenshotData CaptureScreenshot()
        {
            try
            {
                if (!ValidateWebCamTexture())
                {
                    return CreateInvalidScreenshotData();
                }
                
                var webCamTexture = webCamTextureManager.WebCamTexture;
                
                // Check if the camera is playing and has valid data
                if (!webCamTexture.isPlaying || !webCamTexture.didUpdateThisFrame)
                {
                    Debug.LogWarning($"{LOG_PREFIX} Camera is not playing or no new frame available");
                    return CreateInvalidScreenshotData();
                }
                
                var originalWidth = webCamTexture.width;
                var originalHeight = webCamTexture.height;
                
                // Capture pixels from the camera
                var pixels = webCamTexture.GetPixels32();
                if (pixels == null || pixels.Length == 0)
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to get pixels from camera texture");
                    return CreateInvalidScreenshotData();
                }
                
                // Create texture from camera data
                var capturedTexture = new Texture2D(originalWidth, originalHeight, textureFormat, false);
                try
                {
                    capturedTexture.SetPixels32(pixels);
                    capturedTexture.Apply();
                    
                    // Optimize image if enabled
                    var finalTexture = enableImageResize ? 
                        OptimizeImageSize(capturedTexture) : capturedTexture;
                    
                    // Encode to PNG
                    var pngData = finalTexture.EncodeToPNG();
                    
                    // Clean up textures
                    if (finalTexture != capturedTexture)
                    {
                        DestroyImmediate(finalTexture);
                    }
                    
                    var screenshotData = new ScreenshotData
                    {
                        pngData = pngData,
                        width = finalTexture.width,
                        height = finalTexture.height,
                        timestamp = DateTime.Now,
                        isValid = pngData != null && pngData.Length > 0
                    };
                    
                    Debug.Log($"{LOG_PREFIX} Screenshot captured successfully: {screenshotData.width}x{screenshotData.height}, Size: {pngData?.Length ?? 0} bytes");
                    return screenshotData;
                }
                finally
                {
                    // Always clean up the captured texture
                    if (capturedTexture != null)
                    {
                        DestroyImmediate(capturedTexture);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{LOG_PREFIX} Exception during screenshot capture: {e.Message}");
                return CreateInvalidScreenshotData();
            }
        }
        
        /// <summary>
        /// Captures a screenshot asynchronously (for use with async/await patterns)
        /// </summary>
        /// <returns>ScreenshotData containing PNG data and metadata</returns>
        public ScreenshotData CaptureScreenshotAsync()
        {
            // For now, this is synchronous, but structure allows for async implementation
            // if needed for future Unity versions or optimization
            return CaptureScreenshot();
        }
        
        /// <summary>
        /// Validates that the WebCamTexture is available and ready for capture
        /// </summary>
        /// <returns>True if camera is ready for capture</returns>
        private bool ValidateWebCamTexture()
        {
            if (webCamTextureManager == null)
            {
                Debug.LogError($"{LOG_PREFIX} WebCamTextureManager reference is null");
                return false;
            }
            
            if (!webCamTextureManager.enabled)
            {
                Debug.LogError($"{LOG_PREFIX} WebCamTextureManager is disabled");
                return false;
            }
            
            if (webCamTextureManager.WebCamTexture == null)
            {
                Debug.LogError($"{LOG_PREFIX} WebCamTexture is null - camera may not be initialized or permission denied");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Optimizes image size for API efficiency by resizing if needed
        /// </summary>
        /// <param name="originalTexture">The original texture to optimize</param>
        /// <returns>Optimized texture (may be the same as original if no resize needed)</returns>
        private Texture2D OptimizeImageSize(Texture2D originalTexture)
        {
            var originalWidth = originalTexture.width;
            var originalHeight = originalTexture.height;
            
            // Check if resize is needed
            if (originalWidth <= maxImageSize && originalHeight <= maxImageSize)
            {
                return originalTexture;
            }
            
            // Calculate new dimensions maintaining aspect ratio
            var scale = Mathf.Min(
                maxImageSize / (float)originalWidth,
                maxImageSize / (float)originalHeight
            );
            
            var newWidth = Mathf.RoundToInt(originalWidth * scale);
            var newHeight = Mathf.RoundToInt(originalHeight * scale);
            
            // Create resized texture
            var resizedTexture = new Texture2D(newWidth, newHeight, textureFormat, false);
            
            try
            {
                // Use point filtering for better performance
                var resizedPixels = new Color32[newWidth * newHeight];
                
                for (int y = 0; y < newHeight; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        var sourceX = Mathf.RoundToInt(x / scale);
                        var sourceY = Mathf.RoundToInt(y / scale);
                        
                        // Clamp to avoid out-of-bounds access
                        sourceX = Mathf.Clamp(sourceX, 0, originalWidth - 1);
                        sourceY = Mathf.Clamp(sourceY, 0, originalHeight - 1);
                        
                        resizedPixels[y * newWidth + x] = originalTexture.GetPixel(sourceX, sourceY);
                    }
                }
                
                resizedTexture.SetPixels32(resizedPixels);
                resizedTexture.Apply();
                
                Debug.Log($"{LOG_PREFIX} Image resized from {originalWidth}x{originalHeight} to {newWidth}x{newHeight}");
                return resizedTexture;
            }
            catch (Exception e)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to resize image: {e.Message}");
                DestroyImmediate(resizedTexture);
                return originalTexture;
            }
        }
        
        /// <summary>
        /// Creates an invalid screenshot data structure for error cases
        /// </summary>
        /// <returns>Invalid ScreenshotData</returns>
        private ScreenshotData CreateInvalidScreenshotData()
        {
            return new ScreenshotData
            {
                pngData = null,
                width = 0,
                height = 0,
                timestamp = DateTime.Now,
                isValid = false
            };
        }
        
        /// <summary>
        /// Checks if the camera is currently available and ready for capture
        /// </summary>
        /// <returns>True if camera is ready</returns>
        public bool IsCameraReady()
        {
            return ValidateWebCamTexture() && 
                   webCamTextureManager.WebCamTexture.isPlaying;
        }
        
        /// <summary>
        /// Gets the current camera resolution
        /// </summary>
        /// <returns>Camera resolution as Vector2Int, or zero if camera not available</returns>
        public Vector2Int GetCameraResolution()
        {
            if (!ValidateWebCamTexture())
            {
                return Vector2Int.zero;
            }
            
            var webCamTexture = webCamTextureManager.WebCamTexture;
            return new Vector2Int(webCamTexture.width, webCamTexture.height);
        }
        
        /// <summary>
        /// Gets the current camera frame rate
        /// </summary>
        /// <returns>Camera frame rate, or 0 if camera not available</returns>
        public float GetCameraFrameRate()
        {
            if (!ValidateWebCamTexture())
            {
                return 0f;
            }
            
            return webCamTextureManager.WebCamTexture.requestedFPS;
        }
        
        /// <summary>
        /// Estimates the memory usage of a screenshot capture
        /// </summary>
        /// <returns>Estimated memory usage in bytes</returns>
        public long EstimateMemoryUsage()
        {
            if (!ValidateWebCamTexture())
            {
                return 0;
            }
            
            var resolution = GetCameraResolution();
            var width = enableImageResize ? Mathf.Min(resolution.x, maxImageSize) : resolution.x;
            var height = enableImageResize ? Mathf.Min(resolution.y, maxImageSize) : resolution.y;
            
            // Estimate: RGBA32 texture + PNG compression (roughly 30-50% of original)
            var textureMemory = width * height * 4; // 4 bytes per pixel for RGBA32
            var pngMemory = textureMemory * 0.4f; // Estimated PNG compression
            
            return (long)(textureMemory + pngMemory);
        }
        
        private void OnDestroy()
        {
            // Cleanup any remaining resources
            Debug.Log($"{LOG_PREFIX} Component destroyed");
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Validate settings in editor
            if (maxImageSize <= 0)
            {
                maxImageSize = 1024;
            }
            
            if (maxImageSize > 4096)
            {
                Debug.LogWarning($"{LOG_PREFIX} Max image size of {maxImageSize} is very large and may cause performance issues");
            }
        }
        #endif
    }
}