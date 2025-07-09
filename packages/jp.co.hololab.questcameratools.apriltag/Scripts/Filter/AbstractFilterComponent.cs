using UnityEngine;

namespace HoloLab.QuestCameraTools.AprilTagTracking
{
    public abstract class AbstractFilterComponent : MonoBehaviour
    {
        public abstract AprilTagWithFilterState Process(AprilTagWithFilterState tagWithState);
    }
}