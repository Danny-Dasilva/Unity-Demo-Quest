using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static HoloLab.QuestCameraTools.AprilTagTracking.AprilTagWithFilterState;

namespace HoloLab.QuestCameraTools.AprilTagTracking
{
    public class AprilTagWithFilterState
    {
        public enum FilterStateType
        {
            NotDetected = 0,
            Valid,
            Ignored
        }

        public FilterStateType FilterState { get; }

        public AprilTagDetectedInfo AprilTagDetectedInfo { get; }

        public AprilTagWithFilterState(FilterStateType filterState, AprilTagDetectedInfo aprilTagDetectedInfo)
        {
            FilterState = filterState;
            AprilTagDetectedInfo = aprilTagDetectedInfo;
        }
    }

    public class AprilTagTracker : MonoBehaviour
    {
        private enum TrackingStateType
        {
            None = 0,
            Tracking,
            Lost
        }

        private enum AnchorPointType
        {
            Center = 0,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        [SerializeField]
        private int targetAprilTagID = 0;

        public int TargetAprilTagID
        {
            get => targetAprilTagID;
            set => targetAprilTagID = value;
        }

        [SerializeField]
        private AnchorPointType anchorPoint = AnchorPointType.Center;

        [SerializeField]
        private bool scaleByPhysicalSize = false;

        [SerializeField]
        private RotationConstraintType rotationConstraint = RotationConstraintType.AnyDirection;

        public RotationConstraintType RotationConstraint
        {
            get => rotationConstraint;
            set => rotationConstraint = value;
        }

        [SerializeField]
        private List<AbstractFilterComponent> filterComponents = new List<AbstractFilterComponent>();

        [SerializeField]
        private UnityEvent onAwake = new UnityEvent();

        [SerializeField]
        private UnityEvent<AprilTagDetectedInfo> onFirstDetected = new UnityEvent<AprilTagDetectedInfo>();

        [SerializeField]
        private UnityEvent<AprilTagDetectedInfo> onDetected = new UnityEvent<AprilTagDetectedInfo>();

        [SerializeField]
        private UnityEvent onLost = new UnityEvent();

        private QuestAprilTagTracking aprilTagTracking;

        private TrackingStateType trackingState = TrackingStateType.None;

        public event Action<AprilTagDetectedInfo> OnFirstDetected;
        public event Action<AprilTagDetectedInfo> OnDetected;
        public event Action OnLost;

        private void Awake()
        {
            aprilTagTracking = FindFirstObjectByType<QuestAprilTagTracking>();
            if (aprilTagTracking == null)
            {
                Debug.LogError($"{nameof(QuestAprilTagTracking)} not found in scene");
            }

            InvokeOnAwake();
        }

        private void Start()
        {
            if (aprilTagTracking != null)
            {
                aprilTagTracking.OnAprilTagDetected += OnAprilTagDetected;
            }
            else
            {
                Debug.LogError("AprilTagTracker: aprilTagTracking is null in Start()");
            }
        }

        private void OnAprilTagDetected(List<AprilTagDetectedInfo> infoList)
        {
            
            AprilTagWithFilterState tagWithState;

            var info = infoList.FirstOrDefault(x => x.ID == targetAprilTagID);
            if (info == null)
            {
                tagWithState = new AprilTagWithFilterState(FilterStateType.NotDetected, null);
            }
            else
            {
                tagWithState = new AprilTagWithFilterState(FilterStateType.Valid, info);
            }

            // Apply filters to the detected pose
            foreach (var filter in filterComponents)
            {
                tagWithState = filter.Process(tagWithState);
            }

            switch (tagWithState.FilterState)
            {
                case FilterStateType.NotDetected:
                    // Target AprilTag not detected
                    if (trackingState == TrackingStateType.Tracking)
                    {
                        trackingState = TrackingStateType.Lost;
                        InvokeOnLost();
                    }
                    break;
                case FilterStateType.Valid:
                    if (this == null || transform == null)
                    {
                        Debug.LogWarning("AprilTagTracker: Component or transform is null, skipping position update");
                        return;
                    }
                    
                    var detectedInfo = tagWithState.AprilTagDetectedInfo;
                    var anchorPose = GetAnchorPointPose(detectedInfo.Pose, detectedInfo.PhysicalSize, anchorPoint);
                    var rotation = RotationConstraintUtility.ApplyConstraint(anchorPose.rotation, rotationConstraint);
                    transform.SetPositionAndRotation(anchorPose.position, rotation);

                    if (scaleByPhysicalSize)
                    {
                        transform.localScale = detectedInfo.PhysicalSize * Vector3.one;
                    }

                    var firstDetected = trackingState == TrackingStateType.None;
                    trackingState = TrackingStateType.Tracking;

                    if (firstDetected)
                    {
                        InvokeOnFirstDetected(detectedInfo);
                    }

                    InvokeOnDetected(detectedInfo);
                    break;
                case FilterStateType.Ignored:
                    break;
            }
        }

        private void InvokeOnAwake()
        {
            try
            {
                onAwake.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void InvokeOnFirstDetected(AprilTagDetectedInfo info)
        {
            try
            {
                onFirstDetected.Invoke(info);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                OnFirstDetected?.Invoke(info);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void InvokeOnDetected(AprilTagDetectedInfo info)
        {
            try
            {
                onDetected.Invoke(info);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                OnDetected?.Invoke(info);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void InvokeOnLost()
        {
            try
            {
                onLost.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                OnLost?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static Pose GetAnchorPointPose(Pose centerPose, float markerSize, AnchorPointType anchorPoint)
        {
            if (anchorPoint == AnchorPointType.Center)
            {
                return centerPose;
            }

            var offset = anchorPoint switch
            {
                AnchorPointType.TopLeft => new Vector3(-markerSize / 2, 0, markerSize / 2),
                AnchorPointType.TopRight => new Vector3(markerSize / 2, 0, markerSize / 2),
                AnchorPointType.BottomLeft => new Vector3(-markerSize / 2, 0, -markerSize / 2),
                AnchorPointType.BottomRight => new Vector3(markerSize / 2, 0, -markerSize / 2),
                _ => Vector3.zero,
            };

            return new Pose(centerPose.position + centerPose.rotation * offset, centerPose.rotation);
        }
    }
}