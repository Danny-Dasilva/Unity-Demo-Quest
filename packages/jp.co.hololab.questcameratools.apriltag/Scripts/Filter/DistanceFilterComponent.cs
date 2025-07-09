using UnityEngine;

namespace HoloLab.QuestCameraTools.AprilTagTracking
{
    public class DistanceFilterComponent : AbstractFilterComponent
    {
        [SerializeField]
        private float minDistance = 0.1f;

        [SerializeField]
        private float maxDistance = 10f;

        public override AprilTagWithFilterState Process(AprilTagWithFilterState tagWithState)
        {
            if (tagWithState.FilterState != AprilTagWithFilterState.FilterStateType.Valid)
            {
                return tagWithState;
            }

            var distance = Vector3.Distance(Camera.main.transform.position, tagWithState.AprilTagDetectedInfo.Pose.position);
            
            if (distance < minDistance || distance > maxDistance)
            {
                return new AprilTagWithFilterState(AprilTagWithFilterState.FilterStateType.Ignored, tagWithState.AprilTagDetectedInfo);
            }

            return tagWithState;
        }
    }
}