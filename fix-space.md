# AprilTag Coordinate Space Fix Analysis

## Problem Summary

The current AprilTag implementation has coordinate system issues where:
1. The prefab follows head movement instead of staying fixed on the physical AprilTag
2. Camera-to-world transformations don't account for Meta Quest's passthrough camera coordinate system
3. The environment raycasting approach works but the user wants to use direct pose estimation

## Understanding the Coordinate Systems

### 1. AprilTag Library Coordinate System (jp.keijiro.apriltag)

From `PoseEstimationJob.cs`:
```csharp
// Line 53: Position transformation
var pos = pose.t.AsFloat3() * math.float3(1, -1, 1);

// Line 55-56: Rotation transformation  
var rot = math.quaternion(pose.R.AsFloat3x3());
rot = rot.value * math.float4(-1, 1, -1, 1);
```

The AprilTag library:
- Flips Y-axis for position (multiply by -1)
- Flips X and Z quaternion components for rotation
- These transformations convert from AprilTag's native coordinate system to Unity's camera space

### 2. Meta Quest Passthrough Camera System

From `PassthroughCameraUtils.cs`:
```csharp
// Line 157: Critical 180-degree rotation
worldFromCamera.orientation *= Quaternion.Euler(180, 0, 0);
```

The Meta Quest passthrough camera:
- Applies a 180-degree rotation around X-axis to the camera pose
- This is necessary to align the camera's coordinate system with Unity's world space
- The camera looks "backwards" initially, hence the rotation

### 3. The Core Issue

The problem is that the AprilTag pose is in **camera space**, not world space. When the head moves:
- The camera pose changes
- The AprilTag's camera-space position remains constant relative to the camera
- This makes the virtual object appear to follow the head

## Correct Transformation Pipeline

### Step 1: Understand What We Have
- `tagPose.Position`: AprilTag position in camera space (after Keijiro's transformations)
- `tagPose.Rotation`: AprilTag rotation in camera space (after Keijiro's transformations)
- `cameraPose`: Camera position and rotation in world space (includes 180° X rotation)

### Step 2: The Correct Mathematical Transformation

The standard formula for camera-to-world transformation is:
```
worldPosition = cameraPosition + cameraRotation * localPosition
worldRotation = cameraRotation * localRotation
```

However, this assumes the coordinate systems are aligned. In our case, we need to account for:

1. **The 180-degree X rotation in the camera pose**
2. **Potential coordinate system mismatches**

### Step 3: Analysis of Previous Attempts

From `learnings.md`, the following approaches were tried and failed:

1. **Direct transformation**: Head rotation made prefab follow camera
2. **X-axis mirroring**: Fixed left/right but head rotation still broken
3. **Undoing 180° rotation**: Made prefab disappear (behind user)
4. **Various quaternion manipulations**: None fixed the fundamental issue

## The Proposed Fix

### Understanding the ArUco/OpenCV Approach

ArUco markers (similar to AprilTags) in OpenCV typically:
1. Detect marker corners in 2D image space
2. Use camera intrinsics to estimate 3D pose
3. Apply camera extrinsics to get world position

The key insight is that we need to properly handle the camera's coordinate system, which in Meta Quest's case includes that 180-degree rotation.

### The Correct Transformation

```csharp
private AprilTagDetectedInfo ConvertToDetectedInfo(AprilTag.TagPose tagPose, Pose cameraPose, PassthroughCameraEye eye)
{
    // The AprilTag position is in camera space after Keijiro's transformations
    // The camera pose already includes the 180-degree X rotation from PassthroughCameraUtils
    
    // Method 1: Direct transformation (should work if coordinate systems align)
    var worldPosition = cameraPose.position + cameraPose.rotation * tagPose.Position;
    var worldRotation = cameraPose.rotation * tagPose.Rotation;
    
    // Method 2: If Method 1 still has issues, we might need to account for the camera's view direction
    // The 180-degree rotation means the camera is looking in the -Z direction
    // We might need to flip the Z-axis of the AprilTag position
    var flippedPosition = new Vector3(tagPose.Position.x, tagPose.Position.y, -tagPose.Position.z);
    var worldPosition2 = cameraPose.position + cameraPose.rotation * flippedPosition;
    
    // Method 3: If coordinate handedness is an issue
    // Try applying the inverse of the 180-degree rotation to the tag pose first
    var unrotatedCameraRotation = cameraPose.rotation * Quaternion.Euler(-180, 0, 0);
    var worldPosition3 = cameraPose.position + unrotatedCameraRotation * tagPose.Position;
    var worldRotation3 = unrotatedCameraRotation * tagPose.Rotation;
    
    // Choose the appropriate method based on testing
    var tagWorldPose = new Pose(worldPosition, worldRotation);
    
    // Physical size remains the same
    var physicalSize = tagSize;
    var physicalWidth = tagSize;
    var physicalHeight = tagSize;
    
    return new AprilTagDetectedInfo(tagWorldPose, physicalSize, physicalWidth, physicalHeight, tagPose.ID);
}
```

## Why Previous Attempts Failed

1. **Not understanding the 180-degree rotation**: The camera pose from PassthroughCameraUtils already includes this rotation. Attempts to undo it were misguided.

2. **Coordinate system confusion**: The transformations in PoseEstimationJob.cs already convert to Unity's camera space convention.

3. **Testing methodology**: Small errors in transformation can make objects appear behind the user or at wrong orientations.

## Implementation Strategy

1. **Start with the simplest transformation**: Direct camera-to-world using the provided camera pose
2. **Add debug visualization**: Draw rays or debug spheres to understand where the transformation places objects
3. **Test systematically**: 
   - First test with camera at origin (0,0,0) facing forward
   - Then test with camera movement
   - Finally test with camera rotation
4. **Compare with QR implementation**: The QR code implementation works correctly, so we can use it as a reference

## Alternative Approach: Use Camera View Matrix

If the direct transformation doesn't work, we can try using the camera's view matrix approach:

```csharp
// Get the camera's view matrix (world-to-camera transformation)
var viewMatrix = Matrix4x4.TRS(cameraPose.position, cameraPose.rotation, Vector3.one).inverse;

// Get the inverse view matrix (camera-to-world transformation)
var cameraToWorld = viewMatrix.inverse;

// Transform the AprilTag position from camera space to world space
var worldPosition = cameraToWorld.MultiplyPoint3x4(tagPose.Position);
var worldRotation = cameraToWorld.rotation * tagPose.Rotation;
```

## Key Considerations

1. **Unity's coordinate system**: Left-handed, Y-up, Z-forward
2. **Camera space convention**: Camera looks down -Z axis
3. **Meta Quest specifics**: The 180-degree rotation is already applied in the camera pose
4. **AprilTag library**: Already converts to Unity's camera space conventions

## Testing Plan

1. Implement the basic transformation first
2. Add extensive debug logging to understand the coordinate values
3. Test with a stationary AprilTag and move only the head
4. Verify that the virtual object stays fixed in world space
5. Test rotation by rotating the physical AprilTag
6. Compare behavior with the working QR code implementation

## Conclusion

The fix should focus on properly transforming from camera space to world space while respecting:
1. The AprilTag library's coordinate transformations
2. Meta Quest's passthrough camera system (including the 180-degree rotation)
3. Unity's coordinate conventions

The environment raycasting approach works because it bypasses these coordinate system issues entirely, but direct pose estimation should also work with the correct transformation matrix.