using System.Collections;
using UnityEngine;
using QuestCameraTools.Utilities;

namespace QuestCameraTools.App
{
    /// <summary>
    /// Test script for PassthroughScreenshotCapture functionality.
    /// This script demonstrates how to use the screenshot capture utility
    /// and provides basic testing capabilities.
    /// </summary>
    public class PassthroughScreenshotCaptureTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runAutomaticTests = false;
        [SerializeField] private float testInterval = 5f;
        [SerializeField] private int maxTestScreenshots = 5;
        
        [Header("References")]
        [SerializeField] private PassthroughScreenshotCapture screenshotCapture;
        [SerializeField] private KeyCode testKey = KeyCode.Space;
        
        private int testCount = 0;
        private const string LOG_PREFIX = "[PassthroughScreenshotCaptureTest]";
        
        private void Start()
        {
            // Find PassthroughScreenshotCapture if not assigned
            if (screenshotCapture == null)
            {
                screenshotCapture = FindObjectOfType<PassthroughScreenshotCapture>();
                if (screenshotCapture == null)
                {
                    Debug.LogError($"{LOG_PREFIX} PassthroughScreenshotCapture not found in scene");
                    return;
                }
            }
            
            Debug.Log($"{LOG_PREFIX} Test initialized. Press {testKey} to capture screenshot manually");
            
            // Run initial status check
            CheckCameraStatus();
            
            // Start automatic testing if enabled
            if (runAutomaticTests)
            {
                StartCoroutine(RunAutomaticTests());
            }
        }
        
        private void Update()
        {
            // Manual screenshot capture
            if (Input.GetKeyDown(testKey))
            {
                CaptureTestScreenshot();
            }
        }
        
        /// <summary>
        /// Captures a test screenshot and logs the results
        /// </summary>
        public void CaptureTestScreenshot()
        {
            if (screenshotCapture == null)
            {
                Debug.LogError($"{LOG_PREFIX} Screenshot capture is not available");
                return;
            }
            
            Debug.Log($"{LOG_PREFIX} Capturing test screenshot...");
            
            var screenshotData = screenshotCapture.CaptureScreenshot();
            
            if (screenshotData.isValid)
            {
                Debug.Log($"{LOG_PREFIX} Screenshot captured successfully!");
                Debug.Log($"{LOG_PREFIX} - Resolution: {screenshotData.width}x{screenshotData.height}");
                Debug.Log($"{LOG_PREFIX} - Data size: {screenshotData.pngData.Length} bytes");
                Debug.Log($"{LOG_PREFIX} - Timestamp: {screenshotData.timestamp}");
                
                // Optionally save to file in development builds
                #if DEVELOPMENT_BUILD || UNITY_EDITOR
                SaveScreenshotToFile(screenshotData);
                #endif
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX} Screenshot capture failed - invalid data");
            }
        }
        
        /// <summary>
        /// Checks and logs the current camera status
        /// </summary>
        private void CheckCameraStatus()
        {
            if (screenshotCapture == null) return;
            
            Debug.Log($"{LOG_PREFIX} Camera Status Check:");
            Debug.Log($"{LOG_PREFIX} - Camera Ready: {screenshotCapture.IsCameraReady()}");
            Debug.Log($"{LOG_PREFIX} - Camera Resolution: {screenshotCapture.GetCameraResolution()}");
            Debug.Log($"{LOG_PREFIX} - Camera Frame Rate: {screenshotCapture.GetCameraFrameRate()}");
            Debug.Log($"{LOG_PREFIX} - Estimated Memory Usage: {screenshotCapture.EstimateMemoryUsage()} bytes");
        }
        
        /// <summary>
        /// Runs automatic screenshot tests at intervals
        /// </summary>
        private IEnumerator RunAutomaticTests()
        {
            Debug.Log($"{LOG_PREFIX} Starting automatic tests (interval: {testInterval}s, max: {maxTestScreenshots})");
            
            while (testCount < maxTestScreenshots)
            {
                yield return new WaitForSeconds(testInterval);
                
                if (screenshotCapture != null && screenshotCapture.IsCameraReady())
                {
                    testCount++;
                    Debug.Log($"{LOG_PREFIX} Automatic test {testCount}/{maxTestScreenshots}");
                    CaptureTestScreenshot();
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX} Skipping automatic test - camera not ready");
                }
            }
            
            Debug.Log($"{LOG_PREFIX} Automatic tests completed");
        }
        
        /// <summary>
        /// Saves screenshot to file for debugging (development builds only)
        /// </summary>
        private void SaveScreenshotToFile(PassthroughScreenshotCapture.ScreenshotData screenshotData)
        {
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            try
            {
                var fileName = $"passthrough_screenshot_{screenshotData.timestamp:yyyy-MM-dd_HH-mm-ss}.png";
                var filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                
                System.IO.File.WriteAllBytes(filePath, screenshotData.pngData);
                Debug.Log($"{LOG_PREFIX} Screenshot saved to: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to save screenshot: {e.Message}");
            }
            #endif
        }
        
        /// <summary>
        /// Public method to trigger screenshot capture (for UI buttons, etc.)
        /// </summary>
        public void TriggerScreenshotCapture()
        {
            CaptureTestScreenshot();
        }
        
        /// <summary>
        /// Public method to check camera status (for UI display, etc.)
        /// </summary>
        public void RefreshCameraStatus()
        {
            CheckCameraStatus();
        }
        
        private void OnGUI()
        {
            if (screenshotCapture == null) return;
            
            // Simple GUI for testing
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"PassthroughScreenshotCapture Test", GUI.skin.box);
            
            if (GUILayout.Button("Capture Screenshot"))
            {
                CaptureTestScreenshot();
            }
            
            if (GUILayout.Button("Check Camera Status"))
            {
                CheckCameraStatus();
            }
            
            GUILayout.Label($"Camera Ready: {screenshotCapture.IsCameraReady()}");
            GUILayout.Label($"Resolution: {screenshotCapture.GetCameraResolution()}");
            GUILayout.Label($"Test Count: {testCount}");
            
            GUILayout.EndArea();
        }
    }
}