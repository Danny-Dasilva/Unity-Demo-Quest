using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using Meta.XR;
using PassthroughCameraSamples;

namespace HoloLab.QuestCameraTools.AprilTagTracking
{
    internal class AprilTagDetector
    {
        private AprilTag.TagDetector tagDetector;
        private readonly int decimation;
        private readonly float tagSize;
        private Color32[] buffer;

        public AprilTagDetector(int decimation = 1, float tagSize = 0.1f)
        {
            this.decimation = decimation;
            this.tagSize = tagSize;
            tagDetector = null;
            buffer = null;
        }

        public System.Collections.Generic.IEnumerable<AprilTag.TagPose> DetectMultiple(WebCamTexture webCamTexture, PassthroughCameraEye eye)
        {
            try
            {
                var width = webCamTexture.width;
                var height = webCamTexture.height;
                
                // Create detector if not initialized or dimensions changed
                if (tagDetector == null)
                {
                    tagDetector = new AprilTag.TagDetector(width, height, decimation);
                    buffer = new Color32[width * height];
                }

                // Get pixels from WebCamTexture (must be on main thread)
                webCamTexture.GetPixels32(buffer);
                
                // Convert to ReadOnlySpan<Color32> as expected by ProcessImage
                var imageSpan = new ReadOnlySpan<Color32>(buffer);
                
                // Calculate the correct physical FOV from camera intrinsics.
                var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(eye);
                var fov = 2.0f * Mathf.Atan((height / 2.0f) / intrinsics.FocalLength.y);

                // Process image with AprilTag detector
                tagDetector.ProcessImage(imageSpan, fov, tagSize);
                
                // Return detected tags
                return tagDetector.DetectedTags;
            }
            catch (Exception e)
            {
                Debug.LogError($"AprilTag detection failed: {e.Message}");
                return Enumerable.Empty<AprilTag.TagPose>();
            }
        }

        public void Dispose()
        {
            tagDetector?.Dispose();
            tagDetector = null;
        }
    }
}