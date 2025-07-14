using System.Linq;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace QuestCameraTools.App
{
    /// <summary>
    /// Quest camera permission handler for passthrough camera access
    /// Based on PassthroughCameraPermissions.cs from Meta's sample
    /// </summary>
    public class QuestCameraPermissions : MonoBehaviour
    {
        public static readonly string[] CameraPermissions =
        {
            "android.permission.CAMERA",          // Required to use WebCamTexture object
            "horizonos.permission.HEADSET_CAMERA" // Required to access the Passthrough Camera API in Horizon OS v74+
        };

        public static bool? HasCameraPermission { get; private set; }
        private static bool s_askedOnce;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private void Start()
        {
            // Check and request permissions on startup
            AskCameraPermissions();
        }

#if UNITY_ANDROID
        /// <summary>
        /// Request camera permission if the permission is not authorized by the user
        /// </summary>
        public void AskCameraPermissions()
        {
            if (s_askedOnce)
            {
                LogDebug("[GeminiVoiceToggle] Camera permissions already requested");
                return;
            }
            
            s_askedOnce = true;
            
            if (IsAllCameraPermissionsGranted())
            {
                HasCameraPermission = true;
                LogDebug("[GeminiVoiceToggle] All camera permissions already granted");
            }
            else
            {
                LogDebug("[GeminiVoiceToggle] Requesting camera permissions...");
                
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionDenied += OnPermissionDenied;
                callbacks.PermissionGranted += OnPermissionGranted;
                callbacks.PermissionDeniedAndDontAskAgain += OnPermissionDenied;

                // Request all necessary camera permissions
                Permission.RequestUserPermissions(CameraPermissions, callbacks);
            }
        }

        /// <summary>
        /// Permission granted callback
        /// </summary>
        private void OnPermissionGranted(string permissionName)
        {
            LogDebug($"[GeminiVoiceToggle] Permission granted: {permissionName}");

            // Only consider permissions granted if ALL required permissions are granted
            if (IsAllCameraPermissionsGranted())
            {
                HasCameraPermission = true;
                LogDebug("[GeminiVoiceToggle] All camera permissions now granted - camera ready for use");
                
                // Notify other components that permissions are ready
                NotifyCameraPermissionsReady();
            }
        }

        /// <summary>
        /// Permission denied callback
        /// </summary>
        private void OnPermissionDenied(string permissionName)
        {
            LogError($"[GeminiVoiceToggle] Permission denied: {permissionName}");
            HasCameraPermission = false;
            s_askedOnce = false; // Allow retry
        }

        /// <summary>
        /// Check if all required camera permissions are granted
        /// </summary>
        public static bool IsAllCameraPermissionsGranted()
        {
            return CameraPermissions.All(Permission.HasUserAuthorizedPermission);
        }

        /// <summary>
        /// Notify other components that camera permissions are ready
        /// </summary>
        private void NotifyCameraPermissionsReady()
        {
            // Find and notify WebCamManager to reinitialize if needed
            var webCamManager = FindObjectOfType<SimpleWebCamManager>();
            if (webCamManager != null)
            {
                LogDebug("[GeminiVoiceToggle] Notifying WebCamManager that permissions are ready");
                // webCamManager.ReinitializeCamera();
            }
            else
            {
                LogDebug("[GeminiVoiceToggle] SimpleWebCamManager not found in scene");
            }
        }

#else
        /// <summary>
        /// Request camera permission (non-Android platforms)
        /// </summary>
        public void AskCameraPermissions()
        {
            // On non-Android platforms, assume permissions are granted
            HasCameraPermission = true;
            LogDebug("[GeminiVoiceToggle] Non-Android platform - assuming camera permissions granted");
        }

        public static bool IsAllCameraPermissionsGranted()
        {
            // On non-Android platforms, assume permissions are granted
            return true;
        }
#endif

        /// <summary>
        /// Get current permission status
        /// </summary>
        public static bool IsCameraPermissionGranted()
        {
            return HasCameraPermission == true;
        }

        /// <summary>
        /// Force a permission check (useful for debugging)
        /// </summary>
        public void ForcePermissionCheck()
        {
            s_askedOnce = false;
            AskCameraPermissions();
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[QuestCameraPermissions] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[QuestCameraPermissions] {message}");
        }
    }
}