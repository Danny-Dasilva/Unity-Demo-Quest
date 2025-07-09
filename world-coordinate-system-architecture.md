# World Coordinate System Architecture for AprilTag Tracking

## Overview

This document outlines the architecture for implementing a world coordinate system approach for AprilTag tracking, similar to how OpenCV ArUco implementations achieve stable tracking without environment raycasting.

## Problem Analysis

### Why Direct Camera-to-World Transformation Fails
Our previous approaches failed because they placed virtual objects **relative to the camera**, not in a **fixed world coordinate system**:

```csharp
// WRONG: Camera-relative positioning
var worldPosition = cameraPose.position + cameraPose.rotation * tagPose.Position;
```

When the head rotates:
- Camera pose changes
- Virtual object position recalculates relative to new camera position
- Object appears to move instead of staying fixed

### Why ArUco Succeeds
ArUco establishes a **world coordinate system** where:
1. **Markers have known world positions** - Each marker ID maps to a fixed world location
2. **Camera pose is world-relative** - Camera position calculated relative to the world, not marker
3. **Virtual objects exist in world space** - Objects placed at marker's world position

## Architectural Design

### Core Components

#### 1. World Coordinate System Manager
```csharp
public class WorldCoordinateSystem
{
    private static Dictionary<int, Pose> markerWorldPositions = new();
    private static bool isWorldEstablished = false;
    private static Pose worldOrigin;
}
```

#### 2. AprilTag World Mapper
```csharp
public class AprilTagWorldMapper
{
    public static void EstablishWorldCoordinateSystem(int originTagId, Pose tagPoseInCamera, Pose cameraPose);
    public static Pose GetTagWorldPosition(int tagId);
    public static void RegisterTagWorldPosition(int tagId, Pose worldPose);
}
```

#### 3. Camera World Pose Calculator
```csharp
public class CameraWorldPoseCalculator
{
    public static Pose CalculateCameraWorldPose(int referenceTagId, Pose tagPoseInCamera);
}
```

### Architecture Flow

```
1. First AprilTag Detection
   ├── Tag ID 0 detected at camera space position (x, y, z)
   ├── Establish tag as world origin (0, 0, 0)
   ├── Calculate initial camera world position
   └── Store tag world position in registry

2. Subsequent Detections
   ├── Tag detected in camera space
   ├── Use known tag world position as reference
   ├── Calculate current camera world position
   ├── Virtual objects remain at tag's world position
   └── Head rotation changes camera pose, not object position

3. Multiple Tags
   ├── Additional tags detected relative to established world
   ├── Calculate their world positions using camera world pose
   ├── Store new tag world positions
   └── All tags now have fixed world coordinates
```

## Implementation Strategy

### Phase 1: Single Tag World Establishment

1. **First Detection**: When the first AprilTag is detected:
   - Designate it as the world origin `(0, 0, 0)`
   - Calculate initial camera world position relative to this origin
   - Store the tag's world position

2. **Subsequent Detections**: For the same tag:
   - Use the tag's known world position as reference
   - Calculate current camera world position
   - Place virtual objects at the tag's fixed world position

### Phase 2: Multi-Tag World Expansion

1. **New Tag Detection**: When a new tag is detected:
   - Use current camera world position (calculated from known tags)
   - Calculate the new tag's world position
   - Store the new tag's world coordinates

2. **Cross-Validation**: When multiple known tags are visible:
   - Calculate camera world position from each tag
   - Average or filter results for stability
   - Update virtual object positions based on most reliable data

### Phase 3: World Persistence and Recovery

1. **World State Persistence**: Save world coordinate mappings
2. **Recovery on Restart**: Restore world coordinates when app restarts
3. **World Drift Correction**: Implement algorithms to correct accumulated errors

## Data Structures

### Tag World Registry
```csharp
public static class TagWorldRegistry
{
    private static Dictionary<int, TagWorldData> registeredTags = new();
    
    public struct TagWorldData
    {
        public Pose worldPose;
        public float confidence;
        public DateTime lastSeen;
        public int detectionCount;
    }
}
```

### World Coordinate System State
```csharp
public class WorldCoordinateSystemState
{
    public bool IsEstablished { get; private set; }
    public int OriginTagId { get; private set; }
    public Pose WorldOrigin { get; private set; }
    public Dictionary<int, Pose> TagWorldPositions { get; private set; }
    public Pose CurrentCameraWorldPose { get; private set; }
}
```

## Mathematical Foundations

### Coordinate System Transformation

#### From Camera Space to World Space
```
Given:
- tagPoseInCamera: Position/rotation of tag in camera space
- tagWorldPose: Known position/rotation of tag in world space
- cameraToTag: Transformation from camera to tag

Calculate:
1. tagToCamera = Inverse(cameraToTag)
2. cameraWorldPose = tagWorldPose * tagToCamera
3. virtualObjectWorldPose = tagWorldPose (fixed in world)
```

#### Mathematical Implementation
```csharp
// Step 1: Get tag-to-camera transformation
var tagToCameraPose = new Pose(tagPoseInCamera.position, tagPoseInCamera.rotation);

// Step 2: Invert to get camera-to-tag transformation
var cameraToTagPose = InversePose(tagToCameraPose);

// Step 3: Calculate camera world pose
var cameraWorldPose = CombinePoses(tagWorldPose, cameraToTagPose);

// Step 4: Virtual object stays at tag's world position
var virtualObjectWorldPose = tagWorldPose;
```

## Benefits of This Approach

### 1. **Head Rotation Independence**
- Virtual objects remain fixed at tag world positions
- Camera world pose updates, but objects don't move relative to world

### 2. **Multi-Tag Consistency**
- All tags exist in the same world coordinate system
- Consistent spatial relationships between tags
- Robust tracking when multiple tags are visible

### 3. **No Environment Dependency**
- Works without environment raycasting
- No dependency on Quest's depth sensors
- Pure pose estimation approach

### 4. **Scalability**
- Easy to add new tags to the world
- World coordinate system expands dynamically
- Supports complex multi-tag scenarios

## Implementation Files

### Core Implementation
- `WorldCoordinateSystem.cs` - Central world coordinate management
- `AprilTagWorldMapper.cs` - Tag-to-world mapping logic
- `CameraWorldPoseCalculator.cs` - Camera pose calculation
- `TagWorldRegistry.cs` - Tag position storage and retrieval

### Modified Existing Files
- `QuestAprilTagTracking.cs` - Integration with world coordinate system
- `AprilTagDetectedInfo.cs` - Include world pose data
- `AprilTagObjectSpawner.cs` - Use world positions for object placement

## Testing Strategy

### Phase 1 Testing: Single Tag
1. **Static Tag Test**: Place tag, rotate head, verify object stays fixed
2. **Moving Tag Test**: Move tag, verify object follows tag movement
3. **Distance Test**: Test at various distances from tag

### Phase 2 Testing: Multiple Tags
1. **Two Tag Test**: Establish world with two tags, verify consistent positioning
2. **Tag Occlusion Test**: Cover/uncover tags, verify world persistence
3. **Cross-Validation Test**: Multiple tags visible, verify consistent camera pose calculation

### Phase 3 Testing: Robustness
1. **App Restart Test**: Verify world coordinate persistence
2. **Long Session Test**: Check for drift over extended periods
3. **Edge Case Test**: Very close/far distances, extreme angles

## Success Criteria

1. **Head Rotation**: Virtual objects remain fixed during head rotation
2. **Tag Movement**: Virtual objects follow physical tag movement accurately
3. **Multi-Tag Consistency**: All tags maintain consistent world relationships
4. **Stability**: No jitter or drift during normal usage
5. **Performance**: Minimal computational overhead vs current implementation

This architecture provides a foundation for achieving stable, ArUco-like tracking using AprilTags without environment raycasting, by establishing a proper world coordinate system that separates camera movement from object positioning.