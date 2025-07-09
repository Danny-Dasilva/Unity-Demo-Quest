using UnityEngine;

namespace HoloLab.QuestCameraTools.AprilTagTracking
{
    public class AprilTagDetectedInfo
    {
        public Pose Pose { get; }

        public float PhysicalSize { get; }

        public float PhysicalWidth { get; }

        public float PhysicalHeight { get; }

        public int ID { get; }

        public AprilTagDetectedInfo(Pose pose, float physicalSize, float physicalWidth, float physicalHeight, int id)
        {
            Pose = pose;
            PhysicalSize = physicalSize;
            PhysicalWidth = physicalWidth;
            PhysicalHeight = physicalHeight;
            ID = id;
        }
    }
}