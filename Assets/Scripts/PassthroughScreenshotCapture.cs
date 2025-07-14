using System;
using UnityEngine;

namespace QuestCameraTools.App
{
    /// <summary>
    /// Simplified screenshot capture for Quest passthrough camera
    /// Uses SimpleWebCamManager instead of external dependencies
    /// </summary>
    public class PassthroughScreenshotCapture : MonoBehaviour
    {
        [Header("Image Optimization")]
        [SerializeField] private int maxImageSize = 1024;
        [SerializeField] private bool enableImageResize = true;
        [SerializeField] private TextureFormat textureFormat = TextureFormat.RGBA32;
        
        [Header("References")]
        [SerializeField] private SimpleWebCamManager webCamManager;
        
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
            // Try to find SimpleWebCamManager if not assigned
            if (webCamManager == null)
            {
                webCamManager = FindObjectOfType<SimpleWebCamManager>();
                if (webCamManager == null)
                {
                    Debug.LogError($"{LOG_PREFIX} SimpleWebCamManager not found in scene. Please assign one or ensure it exists.");
                }
            }
        }
        
        /// <summary>
        /// Captures a screenshot from the current camera feed
        /// </summary>
        public ScreenshotData CaptureScreenshot()
        {
            try
            {
                if (!ValidateWebCamManager())
                {
                    return CreateInvalidScreenshotData();
                }
                
                if (!webCamManager.IsCameraReady())
                {
                    Debug.LogWarning($"{LOG_PREFIX} Camera is not ready for capture");
                    return CreateInvalidScreenshotData();
                }
                
                // Capture from the webcam manager
                var imageData = webCamManager.CaptureScreenshot();
                
                if (imageData == null || imageData.Length == 0)
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to capture image data");
                    return CreateInvalidScreenshotData();
                }
                
                // Get original resolution
                var resolution = webCamManager.GetResolution();
                
                // Optimize image size if needed
                if (enableImageResize && (resolution.x > maxImageSize || resolution.y > maxImageSize))
                {
                    imageData = ResizeImageData(imageData, resolution);
                }
                
                var screenshotData = new ScreenshotData
                {
                    pngData = imageData,
                    width = resolution.x,
                    height = resolution.y,
                    timestamp = DateTime.Now,
                    isValid = true
                };
                
                Debug.Log($"{LOG_PREFIX} Screenshot captured successfully: {screenshotData.width}x{screenshotData.height}, Size: {imageData.Length} bytes");
                return screenshotData;
            }
            catch (Exception e)
            {
                Debug.LogError($"{LOG_PREFIX} Exception during screenshot capture: {e.Message}");
                return CreateInvalidScreenshotData();
            }
        }
        
        /// <summary>
        /// Resize image data to fit within maxImageSize while maintaining aspect ratio
        /// </summary>
        private byte[] ResizeImageData(byte[] originalData, Vector2Int originalResolution)
        {
            try
            {
                // Load original texture
                var originalTexture = new Texture2D(2, 2);
                if (!originalTexture.LoadImage(originalData))
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to load original image data");
                    DestroyImmediate(originalTexture);
                    return originalData;
                }
                
                // Calculate new dimensions
                var scale = Mathf.Min(
                    maxImageSize / (float)originalTexture.width,
                    maxImageSize / (float)originalTexture.height
                );
                
                if (scale >= 1.0f)
                {
                    // No resize needed
                    DestroyImmediate(originalTexture);
                    return originalData;
                }
                
                var newWidth = Mathf.RoundToInt(originalTexture.width * scale);
                var newHeight = Mathf.RoundToInt(originalTexture.height * scale);
                
                // Create resized texture
                var resizedTexture = new Texture2D(newWidth, newHeight, textureFormat, false);
                
                // Simple point sampling resize
                var resizedPixels = new Color[newWidth * newHeight];
                for (int y = 0; y < newHeight; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        var sourceX = Mathf.FloorToInt(x / scale);
                        var sourceY = Mathf.FloorToInt(y / scale);
                        
                        sourceX = Mathf.Clamp(sourceX, 0, originalTexture.width - 1);
                        sourceY = Mathf.Clamp(sourceY, 0, originalTexture.height - 1);
                        
                        resizedPixels[y * newWidth + x] = originalTexture.GetPixel(sourceX, sourceY);
                    }
                }
                
                resizedTexture.SetPixels(resizedPixels);
                resizedTexture.Apply();
                
                // Encode to PNG
                var resizedData = resizedTexture.EncodeToPNG();
                
                // Clean up
                DestroyImmediate(originalTexture);
                DestroyImmediate(resizedTexture);
                
                Debug.Log($"{LOG_PREFIX} Image resized from {originalResolution.x}x{originalResolution.y} to {newWidth}x{newHeight}");
                return resizedData;
            }
            catch (Exception e)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to resize image: {e.Message}");
                return originalData;
            }
        }
        
        /// <summary>
        /// Validates that the WebCamManager is available and ready
        /// </summary>
        private bool ValidateWebCamManager()
        {
            if (webCamManager == null)
            {
                Debug.LogError($"{LOG_PREFIX} SimpleWebCamManager reference is null");
                return false;
            }
            
            if (!webCamManager.enabled)
            {
                Debug.LogError($"{LOG_PREFIX} SimpleWebCamManager is disabled");
                return false;
            }
            
            if (!webCamManager.IsInitialized)
            {
                Debug.LogError($"{LOG_PREFIX} SimpleWebCamManager is not initialized");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Creates an invalid screenshot data structure for error cases
        /// </summary>
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
        public bool IsCameraReady()
        {
            return ValidateWebCamManager() && webCamManager.IsCameraReady();
        }
        
        /// <summary>
        /// Gets the current camera resolution
        /// </summary>
        public Vector2Int GetCameraResolution()
        {
            if (!ValidateWebCamManager())
            {
                return Vector2Int.zero;
            }
            
            return webCamManager.GetResolution();
        }
        
        private void OnDestroy()
        {
            Debug.Log($"{LOG_PREFIX} Component destroyed");
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
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