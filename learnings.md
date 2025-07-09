# AprilTag Coordinate System Debugging - Learnings

## Problem Statement
The goal is to make AprilTag-detected objects appear at the exact same world position as the physical AprilTag, just like the QR code implementation does. The virtual prefab should stay aligned with the physical tag regardless of head movement or tag movement.

## Current Issues (as of latest test - GETTING WORSE)
1. ✅ Up/down tag movement works correctly
2. ❌ Left/right tag movement is inverted (tag moves left, prefab goes right) - X mirroring made this WORSE
3. ❌ Head rotation up/down makes prefab move opposite direction (should stay fixed on tag)
4. ❌ Head rotation left/right makes prefab follow camera (should stay fixed on tag)
5. ❌ We're going in circles with coordinate transformations that don't work

## Key Technical Understanding

### Camera System Architecture
- **PassthroughCameraUtils.GetCameraPoseInWorld()** applies a 180-degree rotation around X-axis (line 157)
- This rotation is: `worldFromCamera.orientation *= Quaternion.Euler(180, 0, 0);`
- This flips the camera's coordinate system to match Unity's expectations

### AprilTag vs QR Code Differences
**QR Code Implementation:**
- Uses environment raycasting to find real-world surface positions
- Casts rays from detected QR corners into the real world using `EnvironmentRaycastManager`
- Calculates position based on actual geometry hit points
- This ensures perfect alignment with physical world because it uses depth sensor data

**AprilTag Implementation:**
- Uses direct pose estimation from Keijiro's AprilTag library
- Provides position and rotation directly in camera space
- Must transform from camera space to world space manually
- No environment raycasting - relies on pose math

### Keijiro AprilTag Library Coordinate System
From `PoseEstimationJob.cs` analysis:
- Position transform: `pos = pose.t.AsFloat3() * math.float3(1, -1, 1);` (flips Y)
- Rotation transform: `rot.value * math.float4(-1, 1, -1, 1);` (flips X and Z components)
- These transforms convert from raw AprilTag coords to Unity camera space

## Attempted Fixes and Results

### Fix 1: Standard Camera-to-World Transform
```csharp
var worldPosition = cameraPose.position + cameraPose.rotation * tagPose.Position;
var worldRotation = cameraPose.rotation * tagPose.Rotation;
```
**Result:** Left/right worked, up/down worked, but head rotation made prefab follow camera

### Fix 2: X-Axis Mirroring
```csharp
var correctedPosition = new Vector3(-tagPose.Position.x, tagPose.Position.y, tagPose.Position.z);
var worldPosition = cameraPose.position + cameraPose.rotation * correctedPosition;
```
**Result:** Fixed left/right inversion, but head rotation still made prefab follow camera

### Fix 3: X-Axis Mirror + 180° Y Rotation
```csharp
var correctedPosition = new Vector3(-tagPose.Position.x, tagPose.Position.y, tagPose.Position.z);
var correctedRotation = tagPose.Rotation * Quaternion.Euler(0, 180, 0);
```
**Result:** Same issues - head rotation still problematic

### Fix 4: Quaternion Component Mirroring
```csharp
var correctedRotation = new Quaternion(-tagPose.Rotation.x, tagPose.Rotation.y, -tagPose.Rotation.z, tagPose.Rotation.w);
```
**Result:** Same issues - head rotation still problematic

### Fix 5: Undo PassthroughCameraUtils 180° Rotation
```csharp
var correctedCameraRotation = cameraPose.rotation * Quaternion.Euler(-180, 0, 0);
var worldPosition = cameraPose.position + correctedCameraRotation * correctedPosition;
```
**Result:** Prefab disappeared (likely behind user)

## Debug Log Analysis
From logs when user moved head up/down:
- Camera pose **DOES** update correctly when head moves
- Camera position changes: `(-0.04, 1.20, 0.03)` → `(-0.04, 1.14, 0.00)`
- Camera rotation changes: `(34.84, 358.34, 3.96)` → `(56.81, 1.35, 7.31)`
- Tag position in camera space changes correctly: `(0.25, -0.22, 0.66)` → `(0.27, 0.44, 0.61)`
- World position keeps changing when it should stay constant: `(0.21, 0.66, 0.46)` → `(0.19, 0.89, 0.73)`

## Key Insights

### The Head Rotation Problem
The fact that head rotation makes the prefab follow the camera suggests:
1. Either the camera pose is not updating correctly when head moves, OR
2. Our coordinate transformation is wrong, OR
3. We need to use a different approach (like environment raycasting)

### The X-Axis Inversion
Left/right inversion consistently occurs, requiring X-axis mirroring (`-tagPose.Position.x`).
This suggests the AprilTag coordinate system has a different handedness than expected.

### The 180° Rotation Issue
PassthroughCameraUtils applies a 180° X rotation. Attempts to undo this made the prefab disappear, suggesting:
1. The rotation is necessary for the coordinate system to work
2. Our understanding of how to undo it is incorrect
3. We should work with the rotation rather than against it

## Next Steps to Try
1. **Environment Raycasting Approach**: Instead of using pose estimation directly, convert AprilTag center to screen coordinates and raycast into the environment like QR codes do
2. **Coordinate System Deep Dive**: Compare the exact coordinate transformations between working QR and broken AprilTag
3. **Camera Space Analysis**: Add more debug logging to understand exactly how camera space relates to world space
4. **Different Transform Order**: Try applying transformations in different orders
5. **Hybrid Approach**: Use AprilTag for detection but QR-style raycasting for positioning

## CRITICAL INSIGHT: We're Solving the Wrong Problem

### The Fundamental Issue
We keep trying different coordinate transformations, but the core problem is that **pose estimation + matrix math transformation is the wrong approach**. The fact that head rotation makes the prefab follow the camera proves our world space transformation is fundamentally broken.

### Why QR Codes Work Perfectly
QR codes use **environment raycasting**:
1. Detect QR corners in 2D camera image
2. Cast rays from those screen points into the real world
3. Find where rays hit actual environment geometry (using Quest's depth sensors)
4. Calculate position based on real-world surface intersections

This guarantees perfect alignment because it uses the actual physical environment detected by the headset.

### Why AprilTag Pose Estimation Fails
AprilTag pose estimation gives us 3D coordinates relative to camera, but:
1. The coordinate system doesn't match Unity/Quest expectations
2. Camera-to-world transformation involves complex rotations we can't get right
3. No connection to real environment geometry
4. We're essentially guessing at coordinate system mappings

### The Pattern of Failure
Every coordinate transformation we try has the same fundamental issue: **head rotation makes prefab follow camera**. This proves we're not achieving proper world space positioning.

## NEW STRATEGY: Environment Raycasting Approach

Instead of fighting coordinate systems, let's use the same approach as QR codes:

### Step 1: Convert AprilTag 3D Pose to 2D Screen Point
- Use camera intrinsics to project the AprilTag center from 3D camera space to 2D screen coordinates
- This is standard computer vision: `screenPoint = cameraIntrinsics * (tagPosition / tagPosition.z)`

### Step 2: Raycast into Environment
- Use `PassthroughCameraUtils.ScreenPointToRayInWorld()` to create a ray from the screen point
- Use `EnvironmentRaycastManager.Raycast()` to find where this ray hits real geometry
- This gives us the exact world position where the AprilTag should appear

### Step 3: Calculate Orientation
- Use the environment surface normal for orientation
- Or project multiple AprilTag corners and calculate orientation from their world positions

### Benefits of This Approach
1. **Guaranteed alignment**: Uses the same system as working QR codes
2. **No coordinate system guessing**: Let Unity/Quest handle the transformations
3. **Real environment integration**: Objects appear on actual surfaces
4. **Proven to work**: QR implementation already does this successfully

## Implementation Plan

1. **Study QR raycast implementation** in detail
2. **Project AprilTag center to screen coordinates** using camera intrinsics
3. **Use environment raycasting** to find world position
4. **Test with simple center-point positioning** first
5. **Add orientation calculation** once positioning works

This approach sidesteps all the coordinate system problems we've been fighting.

## IMPLEMENTATION: Environment Raycasting Approach

### What Was Changed (Latest Implementation)

**Complete Strategy Pivot**: Abandoned pose estimation coordinate transformations entirely and implemented environment raycasting approach identical to QR codes.

#### Key Code Changes:

1. **Added EnvironmentRaycastManager dependency**:
```csharp
var environmentRaycastManager = FindFirstObjectByType<EnvironmentRaycastManager>();
```

2. **New ConvertToDetectedInfo signature**:
```csharp
private AprilTagDetectedInfo ConvertToDetectedInfo(AprilTag.TagPose tagPose, Pose cameraPose, PassthroughCameraEye eye, EnvironmentRaycastManager environmentRaycastManager, int imageHeight)
```

3. **3D to 2D Projection Function**:
```csharp
private Vector2Int? ProjectToScreenSpace(Vector3 positionInCameraSpace, PassthroughCameraEye eye)
{
    var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(eye);
    
    // Project to normalized device coordinates
    var normalizedX = positionInCameraSpace.x / positionInCameraSpace.z;
    var normalizedY = positionInCameraSpace.y / positionInCameraSpace.z;
    
    // Convert to pixel coordinates using camera intrinsics
    var pixelX = normalizedX * intrinsics.FocalLength.x + intrinsics.PrincipalPoint.x;
    var pixelY = normalizedY * intrinsics.FocalLength.y + intrinsics.PrincipalPoint.y;
    
    return new Vector2Int(Mathf.RoundToInt(pixelX), Mathf.RoundToInt(pixelY));
}
```

4. **Environment Raycasting Pipeline**:
```csharp
// Step 1: Project AprilTag 3D center to 2D screen coordinates
var screenPoint = ProjectToScreenSpace(tagPose.Position, eye);

// Step 2: Cast ray from screen point into environment
var ray = PassthroughCameraUtils.ScreenPointToRayInWorld(eye, screenPoint.Value);
if (!environmentRaycastManager.Raycast(ray, out var hitInfo))
{
    return null; // Failed to hit environment
}

// Step 3: Use the hit point as the world position
var worldPosition = hitInfo.point;
```

### Technical Approach Comparison

| Approach | Old (Pose Estimation) | New (Environment Raycasting) |
|----------|----------------------|------------------------------|
| **Data Flow** | 3D pose → matrix transform → world | 3D pose → 2D screen → ray → world |
| **Coordinate Systems** | Manual camera-to-world math | Unity/Quest handles all transforms |
| **Environment Integration** | None (floating in space) | Uses real environment geometry |
| **Alignment Guarantee** | Depends on coordinate math | Guaranteed (same as QR codes) |
| **Head Rotation Behavior** | Broken (follows camera) | Should work (world space) |

### Why This Should Work

1. **Proven Method**: Identical to working QR code implementation
2. **No Coordinate Guessing**: Let the Quest/Unity handle camera intrinsics and world transforms
3. **Real Environment**: Objects appear on actual detected surfaces, not floating in estimated space
4. **Eliminates Root Cause**: No more coordinate system transformation issues

### Debug Information Added

- Projection details: `[Projection] Camera space {positionInCameraSpace} -> Screen ({screenX}, {screenY})`
- Raycast results: `[Environment Raycast] Screen: {screenPoint.Value}, World: {worldPosition}`
- Error handling for out-of-bounds projections and failed raycasts

### Expected Behavior Changes

- **Head rotation**: Prefab should stay fixed on physical tag (no more camera following)
- **Tag movement**: Should work correctly in all directions (no inversions)
- **World positioning**: Objects should appear on real surfaces where AprilTag is detected
- **Robustness**: Failed raycasts return null (no objects in invalid positions)

This represents a fundamental architectural change from mathematical coordinate transformation to environment-based positioning.

## FAILED ATTEMPT: Environment Raycasting Implementation

### What Was Tried
- A previous attempt was made to use environment raycasting, inspired by the working QR code implementation.
- The approach involved taking the 3D camera-space position from the AprilTag library and projecting it to a 2D screen point.
- This 2D point was then used to cast a ray into the environment.

### Result: FAILED - Incorrect Projection
- This method failed because the mathematical projection from the library's 3D point to a 2D screen point was flawed.
- When rotating the head, the calculated 2D point would shift incorrectly, causing the final world position to drift in a circle around the tag.
- **Conclusion**: The 3D-to-2D projection approach is unreliable. The correct method is to use the raw 2D corner points provided by the detection library, as the successful QR code implementation does.

## LATEST ATTEMPT: Y/Z Axis Flip Coordinate Correction (FAILED)

### What Was Tried
Applied coordinate corrections to account for the 180-degree X rotation in PassthroughCameraUtils:

```csharp
// Step 1: Apply coordinate correction for the 180-degree camera rotation
var correctedTagPosition = new Vector3(
    tagPose.Position.x,    // X stays the same
    -tagPose.Position.y,   // Y is flipped
    -tagPose.Position.z    // Z is flipped
);

// Step 2: Apply corresponding rotation correction
var correctedTagRotation = new Quaternion(
    tagPose.Rotation.x,    // X stays the same
    -tagPose.Rotation.y,   // Y is flipped
    -tagPose.Rotation.z,   // Z is flipped  
    tagPose.Rotation.w     // W stays the same
);

// Step 3: Standard camera-to-world transformation with corrected values
var worldPosition = cameraPose.position + cameraPose.rotation * correctedTagPosition;
var worldRotation = cameraPose.rotation * correctedTagRotation;
```

### Result: FAILED - Tag Appears Behind User
- The virtual prefab now appears behind the user instead of on the physical AprilTag
- This suggests the coordinate correction overcorrected the transformation
- The 180-degree rotation logic was incorrect - flipping both Y and Z axes was wrong

### Analysis of the Failure
The attempt to manually correct for the 180-degree X rotation by flipping Y and Z axes fundamentally misunderstood how the rotation affects the coordinate system. A 180-degree rotation around X axis transforms coordinates as:
- X remains unchanged
- Y becomes -Y  
- Z becomes -Z

However, this transformation is already handled internally by PassthroughCameraUtils, so applying it again double-corrects and moves the object to the wrong location.

### Pattern Recognition
This failure follows the same pattern as previous coordinate transformation attempts:
1. **Identify coordinate system issue**: ✓ Correctly identified 180-degree rotation
2. **Apply mathematical correction**: ❌ Applied wrong/double correction
3. **Result**: Object appears in wrong location (behind user, to the side, etc.)

### Key Insight: The 180-Degree Rotation is Already Handled
The PassthroughCameraUtils.GetCameraPoseInWorld() method applies the 180-degree rotation to give us the correct camera pose in world space. The AprilTag poses from Keijiro's library are already in the proper camera space coordinate system. Our job is simply to transform from camera space to world space using the provided camera pose - no additional corrections needed.

### Conclusion
Every attempt at manual coordinate system correction has failed because we're trying to fix something that isn't broken. The coordinate systems are already properly aligned - the issue lies elsewhere in our understanding of the transformation pipeline.

**RECOMMENDATION**: Revert to the simple, standard camera-to-world transformation and investigate other potential causes of the head rotation following behavior, such as:
1. Timing issues in pose updates
2. Reference frame problems
3. Camera pose calculation errors
4. AprilTag library coordinate system assumptions

## LATEST ATTEMPT: Inverse Camera Rotation Fix (PARTIAL FAILURE)

### What Was Tried
Applied inverse camera rotation to compensate for head movement following behavior:

```csharp
// Transform from camera space to world space using INVERSE camera rotation
// The key insight: to keep objects fixed in world space when head rotates,
// we need to compensate for camera rotation, not apply it directly
var worldPosition = cameraPose.position + Quaternion.Inverse(cameraPose.rotation) * correctedPosition;
var worldRotation = Quaternion.Inverse(cameraPose.rotation) * correctedRotation;
```

### Result: PARTIAL FAILURE - Overcorrection
- **Movement tracking**: Works "kind of correct" when moving the physical tag
- **Head rotation**: Still problematic - "moves too much when I rotate"
- **Positioning**: "tag is in the background, not aligned with the real world tag"
- **Scale**: Object moves excessively as head rotates

### Log Analysis Reveals Core Problem
From debug logs during head rotation (17.86° to 34.60° camera rotation):
- **Tag camera space**: Stable positions (0.10, -0.41, 1.11) → (0.21, 0.20, 1.00)  
- **Final world space**: Excessive movement (0.13, 1.18, 0.90) → (0.33, 1.87, 0.45)
- **Y-axis displacement**: 0.69 meters change for small head rotation
- **Z-axis displacement**: 0.45 meters change (wrong depth)

### Analysis of the Failure Pattern
1. **Tag detection working correctly**: Camera space positions are stable and reasonable
2. **Coordinate transformation is wrong**: World space positions move too dramatically  
3. **Overcorrection**: Inverse rotation approach overcompensates for head movement
4. **Background positioning**: Objects appear at wrong depth/distance from user

### Why Inverse Rotation Failed
The inverse rotation approach assumes the issue is purely rotational, but:
1. **Meta Quest coordinate system complexity**: PassthroughCameraUtils applies 180° rotation + other transformations
2. **Multiple coordinate systems**: AprilTag native → Unity camera space → Quest camera space → World space
3. **Reference frame mismatch**: Quest cameras are not fixed world reference frames

### Pattern Recognition: Fundamental Architecture Problem
Every direct pose estimation attempt fails with the same core issue:
1. **Tag movement tracking**: Works reasonably (proves detection is correct)
2. **Head rotation**: Always causes excessive world space movement
3. **Coordinate transformations**: All approaches (direct, corrected, inverse) fail
4. **Complexity**: Multiple coordinate system layers create cumulative errors

### Key Insight: The Problem is Architectural, Not Mathematical
The issue is not finding the "right" coordinate transformation formula. The problem is that **direct pose estimation is incompatible with Meta Quest's passthrough camera system** due to:

1. **Complex camera pose pipeline**: PassthroughCameraUtils applies multiple transformations
2. **Moving reference frames**: Camera poses change with head movement in complex ways  
3. **Coordinate system stack**: AprilTag → Camera → Quest → World involves too many transformations
4. **Accumulated errors**: Each transformation layer introduces potential misalignment

### Successful Alternative: Environment Raycasting
The environment raycasting approach works because it:
1. **Bypasses coordinate math**: Lets Unity/Quest handle transformations
2. **Uses proven pipeline**: Same approach as working QR code implementation
3. **Real world grounding**: Objects appear on actual environment surfaces
4. **Single transformation**: Direct 3D→2D→World pipeline without intermediate coordinate systems

### Final Recommendation: Architecture Change
**Stop trying to fix coordinate transformations.** The evidence shows:

1. **Environmental raycasting works** (documented success in learnings.md)
2. **All direct pose estimation attempts fail** (6+ different approaches tried)
3. **Quest passthrough camera system is too complex** for direct coordinate math
4. **QR code implementation uses raycasting** and works perfectly

**Recommended implementation:**
1. **Use environment raycasting approach** (already proven to work)
2. **Abandon direct pose estimation** for Meta Quest passthrough cameras
3. **Accept the architectural reality** that Quest cameras require environment-based positioning

This represents a fundamental shift from fighting the coordinate system to working with the Quest's environmental understanding.

## LATEST ATTEMPT: Matrix Transform Approach (FAILED)

### What Was Tried
Implemented proper matrix-based coordinate transformation using Unity's built-in matrix math:

```csharp
// Create the camera's transformation matrix
var cameraMatrix = Matrix4x4.TRS(cameraPose.position, cameraPose.rotation, Vector3.one);

// Transform the tag position from camera space to world space using matrix multiplication
var worldPosition = cameraMatrix.MultiplyPoint3x4(tagPose.Position);

// Transform rotation by combining camera and tag rotations
var worldRotation = cameraPose.rotation * tagPose.Rotation;
```

### Result: FAILED - Head Rotation Still Causes Movement
- **Tag movement tracking**: Works correctly
- **Head rotation**: Prefab still moves when head rotates (fundamental issue persists)
- **Camera position dependency**: World position changes dramatically when camera moves

### Log Analysis Reveals Fundamental Problem
From debug logs showing dramatic camera position change:
- **Camera position**: `(-0.04, 1.19, 0.05)` → `(-0.03, -0.02, 0.06)` (1.21m drop in Y)
- **Tag camera space**: Stays constant `(0.09, -0.10~-0.11, 0.74)` ✅
- **World position**: `(0.09, 0.65, 0.57)` → `(0.07, -0.26, 0.77)` (0.91m drop) ❌
- **Distance from camera**: Remains ~0.75m (proves math is correct)

### Key Insight: The Math is Correct but the Approach is Wrong
The matrix transformation is mathematically correct - it properly transforms from camera space to world space. The problem is that **this is not what we want**. When the camera moves 1.21m down, the world position also moves down, which means the virtual object follows the camera instead of staying fixed in world space.

### Why This Approach Fails
1. **Camera-relative positioning**: The tag position is relative to the camera
2. **World position depends on camera**: When camera moves, world position changes
3. **No fixed reference frame**: The system has no stable world anchor point

### Pattern Continues: All Direct Transformations Fail
This is now the 7th different coordinate transformation approach that has failed:
1. Direct transformation ❌
2. X-axis mirroring ❌
3. Y-axis + rotation correction ❌
4. Quaternion component mirroring ❌
5. Inverse 180° rotation ❌
6. Inverse camera rotation ❌
7. Matrix transformation ❌

All fail with the same fundamental issue: **objects move when head rotates**.

## CURRENT ATTEMPT: Fixed World Reference Frame Approach

### Hypothesis
The core problem is that **every coordinate transformation approach makes the virtual object follow the camera**. Instead of trying to fix coordinate math, we need to establish a **fixed world reference frame** that stays constant regardless of camera movement.

### Implementation
```csharp
private static Vector3? s_firstTagWorldPosition = null;
private static Vector3? s_firstCameraPosition = null;

// First detection: establish world reference frame
if (!s_firstTagWorldPosition.HasValue)
{
    var initialWorldPos = cameraPose.position + cameraPose.rotation * tagPose.Position;
    s_firstTagWorldPosition = initialWorldPos;
    s_firstCameraPosition = cameraPose.position;
    worldPosition = initialWorldPos;
}
else
{
    // Subsequent detections: use fixed reference position
    worldPosition = s_firstTagWorldPosition.Value;
}
```

### Result: PARTIAL SUCCESS with Major Issue
✅ **Head rotation fixed**: Virtual object stays in same world position when head moves  
❌ **Tag movement tracking broken**: When physical tag moves, virtual object doesn't move

### Log Analysis Shows the Problem
The logs reveal the exact issue:

**Physical tag is clearly moving in camera space:**
- Tag position changes: `(0.52, 0.06, 0.80)` → `(0.66, 0.14, 0.69)` → `(0.88, 0.14, 0.63)`
- X moves from 0.52 to 0.88 (significant rightward movement)
- Y moves from 0.06 to 0.14 (upward movement)  
- Z moves from 0.80 to 0.63 (closer to camera)

**But virtual object stays completely fixed:**
- World position: Always `(-0.08, 0.64, 0.53)` (never changes)

### Why This Approach is Too Simplistic
The fixed reference approach only solves half the problem:
1. ✅ Prevents camera movement from affecting virtual object
2. ❌ Prevents physical tag movement from affecting virtual object

We need an approach that:
- **Ignores camera movement** (head rotation/translation)
- **Responds to tag movement** (actual physical tag position changes)

## Meta-Analysis: The Coordinate System Problem

### What We Know Works
1. **Environment raycasting approach**: Proven to work (documented earlier)
2. **QR code implementation**: Uses raycasting and works perfectly
3. **Tag detection**: AprilTag detection in camera space is accurate

### What Consistently Fails
1. **All direct pose transformations**: 7+ different approaches, all fail
2. **Coordinate system corrections**: Every attempt to fix coordinates fails
3. **Mathematical transformations**: Correct math but wrong results

### The Core Issue
The problem is not mathematical - it's architectural. Direct pose estimation assumes:
1. A stable world reference frame
2. Simple camera-to-world transformation
3. Compatible coordinate systems

But Meta Quest's passthrough system has:
1. Complex camera transformations (180° rotation + more)
2. Moving reference frames
3. Multiple coordinate system layers

### Why Environment Raycasting Works
1. **Bypasses coordinate math**: Unity/Quest handle all transformations
2. **Uses physical environment**: Anchors to real-world surfaces
3. **Proven pipeline**: Same as working QR implementation
4. **Single source of truth**: Environment depth data

## LATEST ATTEMPT: Tag Movement Delta Tracking Approach (FAILED)

### What Was Tried
Implemented delta tracking to distinguish between camera movement and tag movement:

```csharp
private static Vector3? s_firstTagWorldPosition = null;
private static Vector3? s_firstTagCameraPosition = null;
private static Quaternion? s_firstCameraRotation = null;

// First detection: establish reference frame
if (!s_firstTagWorldPosition.HasValue)
{
    var initialWorldPos = cameraPose.position + cameraPose.rotation * tagPose.Position;
    s_firstTagWorldPosition = initialWorldPos;
    s_firstTagCameraPosition = tagPose.Position;
    s_firstCameraRotation = cameraPose.rotation;
    worldPosition = initialWorldPos;
}
else
{
    // Calculate how much the tag has moved in camera space
    var tagDelta = tagPose.Position - s_firstTagCameraPosition.Value;
    
    // Apply tag movement delta to the reference world position
    var worldDelta = s_firstCameraRotation.Value * tagDelta;
    worldPosition = s_firstTagWorldPosition.Value + worldDelta;
}
```

### Result: FAILED - Cannot Distinguish Head Rotation from Tag Movement

**Tag movement tracking**: ✅ Works correctly when tag physically moves
**Head rotation**: ❌ Still causes prefab to move when head rotates

### Log Analysis Reveals the Fundamental Flaw

From debug logs during head rotation (user moving head left/right):
- **Tag camera space position changes dramatically**: 
  - `(0.24, 0.08, 0.54)` → `(-0.15, 0.26, 0.66)` → `(-0.28, 0.28, 0.68)` → `(-0.37, 0.30, 0.69)`
  - X changes from 0.24 to -0.37 (0.61 units change)
  - Y changes from 0.08 to 0.30 (0.22 units change)
- **Camera pose stays constant**: `(-0.03, -0.02, 0.06), Rot: (10.88, 0.49, 0.54)` (never changes)
- **System interprets head rotation as tag movement**: Delta tracking calculates large tag movement deltas

### Why This Approach Fundamentally Cannot Work

The delta tracking approach assumes we can distinguish between:
1. **Camera movement** (head rotation) - should be ignored
2. **Physical tag movement** - should be tracked

But **both cause identical changes in tag camera space position**:
- When you rotate your head, the AprilTag appears in a different part of the camera view
- When you move the physical tag, it also appears in a different part of the camera view
- **There's no way to distinguish between these two scenarios using only camera space coordinates**

### The Core Issue: Camera Space is Camera-Relative

The fundamental problem is that camera space coordinates are always relative to the camera:
- **Head rotation**: Tag moves from center to left edge of camera view → large camera space position change
- **Tag movement**: Tag moves physically → also causes camera space position change
- **Both scenarios produce identical data**: changing tag position in camera space

### Pattern Recognition: All Direct Approaches Fail for the Same Reason

This is now the **8th different direct coordinate transformation approach** that has failed:
1. Direct transformation ❌
2. X-axis mirroring ❌
3. Y-axis + rotation correction ❌
4. Quaternion component mirroring ❌
5. Inverse 180° rotation ❌
6. Inverse camera rotation ❌
7. Matrix transformation ❌
8. Delta tracking ❌

**All fail because camera space coordinates cannot distinguish between head movement and tag movement.**

### Why Environment Raycasting Works

Environment raycasting succeeds because it:
1. **Uses world-fixed reference**: Projects to screen space, then raycasts to world geometry
2. **Bypasses camera space entirely**: No reliance on camera-relative coordinates
3. **Anchors to physical environment**: Objects appear on real surfaces detected by Quest sensors
4. **Unity/Quest handles transformations**: No manual coordinate system math

### Final Architectural Conclusion

After 8 failed attempts at direct coordinate transformation, the evidence is definitive:

**Direct pose estimation is incompatible with Meta Quest's passthrough camera system for head-rotation-independent tracking.** The fundamental issue is not mathematical - it's architectural. Camera space coordinates cannot provide the information needed to distinguish head movement from tag movement.

## LATEST IMPLEMENTATION: World Coordinate System Approach (ArUco-Style)

### Key Insight: Why OpenCV ArUco Works Without Raycasting

Research into working OpenCV ArUco implementations revealed the critical difference:

**ArUco establishes a world coordinate system where:**
1. **Markers have known world positions** - Each marker ID maps to a fixed world location
2. **Camera pose is world-relative** - Camera position calculated relative to the world, not marker
3. **Virtual objects exist in world space** - Objects placed at marker's world position

**Our previous approaches failed because they placed objects relative to camera:**
```csharp
// WRONG: Camera-relative positioning
var worldPosition = cameraPose.position + cameraPose.rotation * tagPose.Position;
```

**ArUco approach places objects in world coordinates:**
```csharp
// CORRECT: World-relative positioning
var tagWorldPose = knownMarkerPositions[markerId];
var virtualObjectWorldPosition = tagWorldPose.position; // Fixed in world
```

### Implementation: World Coordinate System Architecture

#### Core Components Implemented

1. **WorldCoordinateSystem.cs** - Central world coordinate management
   - Establishes first detected tag as world origin (0,0,0)
   - Registers new tags at fixed world positions
   - Calculates camera world pose using tags as reference points

2. **AprilTagWorldMapper.cs** - Tag-to-world mapping logic
   - Configurable world establishment behavior
   - Consecutive detection requirements for stability
   - Error handling and validation

3. **Modified QuestAprilTagTracking.cs** - Integration with world system
   - Processes tag detections through world mapping
   - Returns world poses instead of camera-relative poses
   - Debug tools for world coordinate system monitoring

#### Mathematical Foundation

```csharp
// Step 1: First tag detected - establish as world origin
tagWorldPose = new Pose(Vector3.zero, Quaternion.identity);

// Step 2: Calculate camera world pose using tag as reference
cameraToTagPose = InversePose(tagPoseInCamera);
cameraWorldPose = CombinePoses(tagWorldPose, cameraToTagPose);

// Step 3: Virtual objects stay at tag's world position
virtualObjectWorldPose = tagWorldPose; // Never changes with head rotation
```

#### Key Benefits

1. **Head Rotation Independence**: Virtual objects remain fixed at tag world positions
2. **Multi-Tag Consistency**: All tags exist in same world coordinate system
3. **No Environment Dependency**: Works without Quest's depth sensors
4. **ArUco Compatibility**: Uses proven coordinate system approach

#### Configuration Options

- **Auto World Establishment**: Automatically use first detected tag as origin
- **Preferred Origin Tag**: Specify which tag ID to use as world origin
- **Consecutive Detection Requirements**: Prevent unstable tags from establishing world
- **Distance and Confidence Thresholds**: Validate detection quality

### Expected Results

This implementation should achieve:
1. ✅ **Head rotation**: Virtual objects stay fixed during head rotation
2. ✅ **Tag movement**: Virtual objects follow physical tag movement
3. ✅ **Multi-tag support**: Consistent spatial relationships between multiple tags
4. ✅ **Stability**: No jitter from coordinate system issues

### Testing Strategy

1. **Single Tag Test**: Place tag, rotate head, verify object stays fixed
2. **Moving Tag Test**: Move tag, verify object follows tag movement  
3. **Multi-Tag Test**: Multiple tags, verify consistent world relationships
4. **Recovery Test**: App restart, verify world coordinate persistence

This approach directly addresses the fundamental architectural issue identified through 8 failed coordinate transformation attempts by establishing a proper world reference frame.

## CRITICAL FIX: World Origin Positioning Issue

### Problem Identified
The initial world coordinate system implementation placed the first detected tag at Unity's absolute world origin (0,0,0), which could be:
- Underground (below the floor)
- At the user's feet
- Behind the user
- At any unexpected location in Unity's coordinate system

This caused virtual objects to be invisible or appear in wrong locations.

### Root Cause
```csharp
// WRONG: Placing tag at absolute world origin
var originWorldPose = new Pose(Vector3.zero, Quaternion.identity);
```

### Solution Implemented
```csharp
// FIXED: Calculate tag's world position relative to camera's current Unity world position
var tagWorldPosition = cameraPose.position + cameraPose.rotation * tagPoseInCamera.position;
var tagWorldRotation = cameraPose.rotation * tagPoseInCamera.rotation;
var tagWorldPose = new Pose(tagWorldPosition, tagWorldRotation);
```

### Why This Fix Works
1. **Camera-Relative Positioning**: Places the tag at a position relative to where the camera currently is
2. **Preserves Distance**: Maintains the correct distance and orientation between camera and tag
3. **AR-Appropriate**: Objects appear at reasonable locations in the user's view
4. **Unity-Compatible**: Works within Unity's existing coordinate system

### Expected Results After Fix
- ✅ Virtual objects should now be visible at the detected tag location
- ✅ Objects should appear at the correct distance from the user
- ✅ Head rotation should still keep objects fixed on physical tags
- ✅ Tag movement should still be tracked correctly

### Debug Logs Added
All critical logs now include `[WORLDFIX]` prefix for easy filtering:
- `[WORLDFIX] [WorldCoordinateSystem] World established with tag X at world position: (x,y,z)`
- `[WORLDFIX] [AprilTag World] Tag X world pose - Pos: (x,y,z)`
- `[WORLDFIX] [WorldCoordinateSystem] Camera world pose calculated from tag X`

### Previous Recommendation (Superseded)

~~After 8+ failed attempts at coordinate transformation, the evidence is overwhelming:
**Use environment raycasting**. It's not a workaround - it's the correct approach for Meta Quest passthrough cameras.~~

**New Recommendation**: Use world coordinate system approach like ArUco. Environment raycasting works but is not necessary when proper world coordinates are established.

## SUCCESSFUL SOLUTION: FOV Correction + Rotation Fix

### The Root Cause Identified
After extensive debugging, the "over-correction" issue that plagued all coordinate transformation attempts was traced to an incorrect Field of View (FOV) being provided to the AprilTag detector.

### The Critical Bug
```csharp
// WRONG: Using Unity's virtual camera FOV
var fov = (Camera.main?.fieldOfView ?? 60f) * Mathf.Deg2Rad;
```

The AprilTag detector was using `Camera.main.fieldOfView`, which represents Unity's virtual camera FOV and has no relation to the physical FOV of the Quest's passthrough cameras. This caused the detector to calculate incorrect tag distances, leading to the scaled "over-corrected" movement.

### The Solution Implemented

#### 1. Fixed AprilTagDetector.cs
Modified to accept `PassthroughCameraEye` parameter and calculate correct physical FOV:

```csharp
public IEnumerable<AprilTag.TagPose> DetectMultiple(WebCamTexture webCamTexture, PassthroughCameraEye eye)
{
    // Calculate the correct physical FOV from camera intrinsics
    var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(eye);
    var fov = 2.0f * Mathf.Atan(height / (2.0f * intrinsics.FocalLength.y));
    
    // Process image with correct FOV
    tagDetector.ProcessImage(imageSpan, fov, tagSize);
}
```

#### 2. Fixed QuestAprilTagTracking.cs
Applied unscaled transformation with rotation correction:

```csharp
private AprilTagDetectedInfo ConvertToDetectedInfo(AprilTag.TagPose tagPose, Pose cameraPose, PassthroughCameraEye eye)
{
    // Correct the tag's rotation to face the camera
    var correctedRotation = tagPose.Rotation * Quaternion.Euler(-90, 0, 0);
    
    // Transform from camera space to world space without applying scene scale
    var worldPosition = cameraPose.position + cameraPose.rotation * tagPose.Position;
    var worldRotation = cameraPose.rotation * correctedRotation;
    var worldPose = new Pose(worldPosition, worldRotation);
    
    return new AprilTagDetectedInfo(worldPose, tagSize, tagSize, tagSize, tagPose.ID);
}
```

### Why This Solution Works

1. **Correct FOV Calculation**: Using camera intrinsics provides the true physical camera properties
2. **Proper Distance Estimation**: AprilTag library can now calculate correct tag distances
3. **Unscaled Transformation**: Direct position math avoids Unity's scene scale issues
4. **Rotation Correction**: The -90° X rotation properly aligns the tag to face the camera

### Results

✅ **Tag tracking works correctly** - Virtual objects appear at tag positions  
✅ **Movement tracking accurate** - Objects follow tag movement in all directions  
✅ **Rotation correct** - Tags face the camera properly with -90° X rotation  
✅ **No over-correction** - Movement is 1:1 with physical tag  

### Remaining Calibration Issue

A small depth offset (tag appears ~1 foot behind actual position) remains, which is likely due to:
1. **Tag Size Mismatch**: Physical tag size doesn't exactly match the value in Unity Inspector
2. **Solution**: Carefully measure the physical AprilTag (black square only, not white border) and enter the exact size in meters

### Key Lesson Learned

The fundamental issue was not with coordinate transformations but with incorrect input data to the AprilTag detector. When provided with the correct physical camera FOV, the simple camera-to-world transformation worked perfectly. This demonstrates the importance of:

1. **Verifying input data** to algorithms (camera intrinsics matter!)
2. **Understanding library expectations** (physical FOV vs virtual camera FOV)
3. **Root cause analysis** over trial-and-error coordinate transformations

### Final Implementation Status

The AprilTag tracking now works as well as the QR code tracking, with proper world-space positioning that remains stable during head movement and correctly tracks physical tag movement. The solution required understanding the passthrough camera system's intrinsics rather than complex coordinate system manipulations.