using UnityEngine;

namespace HoloLab.QuestCameraTools.AprilTagTracking
{
    internal static class RotationConstraintUtility
    {
        private static readonly float cos45 = Mathf.Cos(45 * Mathf.Deg2Rad);

        public static Quaternion ApplyConstraint(Quaternion rotation, RotationConstraintType constraint)
        {
            return constraint switch
            {
                RotationConstraintType.Vertical => ToVerticalRotation(rotation),
                RotationConstraintType.Horizontal => ToHorizontalRotation(rotation),
                RotationConstraintType.VerticalOrHorizontal => ToVerticalOrHorizontalRotation(rotation),
                RotationConstraintType.AnyDirection => rotation,
                _ => rotation,
            };
        }

        private static Quaternion ToVerticalRotation(Quaternion rotation)
        {
            var up = rotation * Vector3.up;
            up.y = 0;
            return Quaternion.LookRotation(Vector3.up, up);
        }

        private static Quaternion ToHorizontalRotation(Quaternion rotation)
        {
            var forward = rotation * Vector3.forward;
            forward.y = 0;

            var up = rotation * Vector3.up;
            if (up.y > 0)
            {
                return Quaternion.LookRotation(forward, Vector3.up);
            }
            else
            {
                return Quaternion.LookRotation(forward, Vector3.down);
            }
        }

        private static Quaternion ToVerticalOrHorizontalRotation(Quaternion rotation)
        {
            var up = rotation * Vector3.up;
            if (Mathf.Abs(up.y) > cos45)
            {
                return ToHorizontalRotation(rotation);
            }
            else
            {
                return ToVerticalRotation(rotation);
            }
        }
    }
}