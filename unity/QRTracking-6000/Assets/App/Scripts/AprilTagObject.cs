using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HoloLab.QuestCameraTools.AprilTagTracking.Samples
{
    [RequireComponent(typeof(HoloLab.QuestCameraTools.AprilTagTracking.AprilTagTracker))]
    public class AprilTagObject : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text aprilTagText;

        private HoloLab.QuestCameraTools.AprilTagTracking.AprilTagTracker aprilTagTracker;

        private void Awake()
        {
            aprilTagTracker = GetComponent<HoloLab.QuestCameraTools.AprilTagTracking.AprilTagTracker>();
        }

        public void SetAprilTagDetectedInfo(HoloLab.QuestCameraTools.AprilTagTracking.AprilTagDetectedInfo info)
        {
            Debug.Log($"AprilTagObject: SetAprilTagDetectedInfo called with ID: {info.ID}");
            
            if (aprilTagText != null)
            {
                aprilTagText.text = $"Tag ID: {info.ID}";
            }
            else
            {
                Debug.LogWarning("AprilTagObject: aprilTagText is null, cannot update text display");
            }
            
            if (aprilTagTracker != null)
            {
                aprilTagTracker.TargetAprilTagID = info.ID;
                Debug.Log($"AprilTagObject: Set target AprilTag ID to {info.ID}");
            }
            else
            {
                Debug.LogError("AprilTagObject: aprilTagTracker is null!");
            }
        }
    }
}