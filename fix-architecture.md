# AprilTag Pose Estimation Architecture

## 1. The Core Problem: A Mismatch of "Camera Spaces"

After analyzing previous attempts, the evidence points to a critical mismatch between the coordinate system of the `tagPose` (from the AprilTag library) and the `cameraPose` (from the Quest Passthrough utilities).

There are two different "camera spaces" in play:

*   **`RawCameraSpace`**: The native coordinate system of the camera sensor, before any orientation fixes are applied. The `tagPose` from `jp.keijiro.apriltag` is relative to this space.
*   **`FlippedCameraSpace`**: The coordinate system *after* `PassthroughCameraUtils` applies its corrective `Quaternion.Euler(180, 0, 0)` rotation. The `cameraPose` transforms from this `FlippedCameraSpace` into world space.

The issue is that previous attempts have tried to transform a point from `RawCameraSpace` using a matrix (`cameraPose`) that expects points in `FlippedCameraSpace`. This conflict is the root cause of the errors, especially the prefab following head rotation.

```mermaid
graph TD
    subgraph "Input Poses"
        A(AprilTag Pose<br><i>in RawCameraSpace</i>)
        B(Camera Pose<br><i>transforms FlippedCameraSpace -> WorldSpace</i>)
    end

    subgraph "Incorrect Transformation (Current State)"
        style "Incorrect Transformation (Current State)" fill:#f99,stroke:#333,stroke-width:2px
        C(Transformation Logic)
        D[Incorrect World Pose]
        A --> C
        B --> C
        C -- "world = B * A<br>(Mismatched spaces)" --> D
    end

    subgraph "Correct Transformation (Proposed)"
         style "Correct Transformation (Proposed)" fill:#9f9,stroke:#333,stroke-width:2px
        E(Pre-transform Tag Pose<br><i>RawCameraSpace -> FlippedCameraSpace</i>)
        F(Apply World Transform)
        G[Correct World Pose]
        
        A -- "Apply 180° X Rotation" --> E
        E --> F
        B --> F
        F -- "world = B * E" --> G
    end
```

## 2. Architectural Solution: The "Pre-Transform" Method

Instead of trying to modify the `cameraPose`, we should first transform the `tagPose` from `RawCameraSpace` into the `FlippedCameraSpace` that the `cameraPose` expects. Once the `tagPose` is in the correct local space, the standard world transformation will work perfectly.

**The Logic:**

1.  We receive `tagPose`, which is a pose within `RawCameraSpace`.
2.  We know that `FlippedCameraSpace` is just `RawCameraSpace` rotated by `Quaternion.Euler(180, 0, 0)`.
3.  Therefore, we apply this 180-degree rotation to `tagPose` *first*, to bring it into `FlippedCameraSpace`. Let's call this `correctedTagPose`.
4.  Now that `correctedTagPose` is in the correct local space, we can use the unmodified `cameraPose` to transform it into world space.

## 3. Implementation Plan

This logic should be implemented in the `ConvertToDetectedInfo` method of `QuestAprilTagTracking.cs`.

**Proposed Code:**
```csharp
private AprilTagDetectedInfo ConvertToDetectedInfo(AprilTag.TagPose tagPose, Pose cameraPose)
{
    // The AprilTag pose is in the camera's local space BEFORE the 180-degree X-axis flip.
    // The cameraPose from PassthroughCameraUtils transforms from the Flipped space to world space.
    // We must first transform the tag's pose into that same Flipped space.

    // Define the corrective rotation that Quest applies to its camera.
    var cameraFlipRotation = Quaternion.Euler(180, 0, 0);

    // Step 1: Pre-transform the tag's pose into the "FlippedCameraSpace".
    var correctedTagPosition = cameraFlipRotation * tagPose.Position;
    var correctedTagRotation = cameraFlipRotation * tagPose.Rotation;

    // Step 2: Now that the tag pose is in the correct local space, apply the standard
    // camera-to-world transformation using the unmodified cameraPose.
    var worldPosition = cameraPose.position + cameraPose.rotation * correctedTagPosition;
    var worldRotation = cameraPose.rotation * correctedTagRotation;

    var tagWorldPose = new Pose(worldPosition, worldRotation);

    // The rest of your method...
    var physicalSize = tagSize; // Or your logic for size
    return new AprilTagDetectedInfo(tagWorldPose, physicalSize, physicalSize, physicalSize, tagPose.ID);
}