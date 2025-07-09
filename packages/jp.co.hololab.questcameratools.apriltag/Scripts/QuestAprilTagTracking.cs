using Meta.XR;
using PassthroughCameraSamples;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using AprilTag;

namespace HoloLab.QuestCameraTools.AprilTagTracking
{
    public class QuestAprilTagTracking : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The frame rate at which the AprilTag detection is performed. If set to 0, the detection will be performed at the minimum interval.")]
        private float detectionFrameRate = 0;

        [SerializeField]
        [Tooltip("Decimation factor for AprilTag detection. Higher values improve speed but reduce accuracy. 1 = no decimation, 2 = half resolution, etc.")]
        private int decimation = 1;

        [SerializeField]
        [Tooltip("Physical size of AprilTag markers in meters")]
        private float tagSize = 0.1f;

        public float DetectionFrameRate
        {
            get => detectionFrameRate;
            set => detectionFrameRate = value;
        }

        public int Decimation
        {
            get => decimation;
            set => decimation = value;
        }

        public float TagSize
        {
            get => tagSize;
            set => tagSize = value;
        }

        private WebCamTextureManager webCamTextureManager;
        private EnvironmentRaycastManager environmentRaycastManager;
        private CancellationTokenSource trackingLoopTokenSource;
        private float lastDetectionTime = float.MinValue;

        private AprilTagDetector aprilTagDetector;

        private bool TrackingEnabled => gameObject != null && gameObject.activeInHierarchy && enabled;

        public event Action<List<AprilTagDetectedInfo>> OnAprilTagDetected;

        private void Awake()
        {
            webCamTextureManager = FindFirstObjectByType<WebCamTextureManager>();
            if (webCamTextureManager == null)
            {
                Debug.LogError("WebCamTextureManager not found in scene");
            }

            environmentRaycastManager = FindFirstObjectByType<EnvironmentRaycastManager>();
            if (environmentRaycastManager == null)
            {
                Debug.LogError("EnvironmentRaycastManager not found in scene. Please add it to the scene.");
            }

            aprilTagDetector = new AprilTagDetector(decimation, tagSize);
        }

        private async void Start()
        {
            trackingLoopTokenSource = new CancellationTokenSource();
            if (webCamTextureManager != null)
            {
                await TrackingLoop(trackingLoopTokenSource.Token);
            }
        }

        private void OnDestroy()
        {
            if (trackingLoopTokenSource != null)
            {
                trackingLoopTokenSource.Cancel();
                trackingLoopTokenSource.Dispose();
                trackingLoopTokenSource = null;
            }

            aprilTagDetector?.Dispose();
        }

        private async Task TrackingLoop(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (TrackingEnabled)
                {
                    if (detectionFrameRate <= 0 || Time.time >= lastDetectionTime + 1f / detectionFrameRate)
                    {
                        var webCamTexture = webCamTextureManager.WebCamTexture;
                        if (webCamTexture != null)
                        {
                            lastDetectionTime = Time.time;
                            DetectAprilTag(webCamTexture);
                        }
                    }
                }

                await Task.Yield();
            }
        }

        private void DetectAprilTag(WebCamTexture webCamTexture)
        {
            var eye = webCamTextureManager.Eye;
            var cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(eye);

            var results = aprilTagDetector.DetectMultiple(webCamTexture, eye);

            var detectedInfos = new List<AprilTagDetectedInfo>();
            foreach (var tagPose in results)
            {
                var detectedInfo = ConvertToDetectedInfo(tagPose, cameraPose, eye);
                if (detectedInfo != null)
                {
                    detectedInfos.Add(detectedInfo);
                }
            }

            if (TrackingEnabled)
            {
                try
                {
                    OnAprilTagDetected?.Invoke(detectedInfos);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private AprilTagDetectedInfo ConvertToDetectedInfo(AprilTag.TagPose tagPose, Pose cameraPose, PassthroughCameraEye eye)
        {
            // Correct the tag's rotation to face the camera.
            var correctedRotation = tagPose.Rotation * Quaternion.Euler(-90, 0, 0);

            // Transform the pose from camera space to world space without applying scene scale.
            var worldPosition = cameraPose.position + cameraPose.rotation * tagPose.Position;
            var worldRotation = cameraPose.rotation * correctedRotation;
            var worldPose = new Pose(worldPosition, worldRotation);

            // For AprilTags, the physical size is predetermined by the tagSize parameter.
            var physicalSize = tagSize;
            var physicalWidth = tagSize;
            var physicalHeight = tagSize;

            return new AprilTagDetectedInfo(worldPose, physicalSize, physicalWidth, physicalHeight, tagPose.ID);
        }
    }
}