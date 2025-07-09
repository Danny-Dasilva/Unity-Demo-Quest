# AprilTag vs ArUco Marker Tracking: Implementation Comparison

This document compares the AprilTag implementation in this repository (using `jp.keijiro.apriltag`) with the OpenCV-based ArUco implementation from [QuestArUcoMarkerTracking](https://github.com/TakashiYoshinaga/QuestArUcoMarkerTracking).

## Overview

Both implementations aim to track fiducial markers in Meta Quest passthrough AR, but they use different marker systems and libraries:

- **This Repository**: AprilTag markers using Keijiro's native Unity package
- **QuestArUcoMarkerTracking**: ArUco markers using OpenCV for Unity

## Key Similarities

### 1. Meta Quest Passthrough Camera Integration
Both implementations:
- Use Meta Quest's Passthrough Camera API
- Access camera frames via Unity's `WebCamTexture`
- Require camera permissions and initialization
- Support both left and right eye cameras

### 2. Architecture Pattern
Both follow a similar architectural pattern:
- **Main Tracking Component**: Manages detection loop and events
- **Detector Module**: Handles marker detection from camera frames
- **Tracker Components**: Individual GameObjects that follow specific markers
- **Camera Utils**: Helper classes for camera pose and intrinsics

### 3. Coordinate Transformation Pipeline
Both must transform from camera space to world space:
```
Camera Frame → Marker Detection → Camera Space Pose → World Space Transform → GameObject Position
```

### 4. Frame Rate Control
Both implementations support configurable detection frame rates to balance performance and responsiveness.

## Key Differences

### 1. Marker System and Library

| Aspect | AprilTag (This Repo) | ArUco (OpenCV) |
|--------|---------------------|-----------------|
| **Marker Type** | AprilTag (tagStandard41h12) | ArUco (DICT_4X4_50, etc.) |
| **Library** | jp.keijiro.apriltag (Native C) | OpenCV for Unity (C++) |
| **Dependencies** | Minimal, native Unity | Heavy, requires OpenCV |
| **Package Size** | ~1MB | ~100MB+ |
| **Platform Support** | Android ARM64 only | Multi-platform |

### 2. Detection Implementation

**AprilTag (jp.keijiro.apriltag):**
```csharp
// Direct detection with native performance
var results = aprilTagDetector.DetectMultiple(webCamTexture);
foreach (var tagPose in results)
{
    // tagPose already contains Position and Rotation in camera space
    var worldPose = TransformToWorld(tagPose, cameraPose);
}
```

**ArUco (OpenCV):**
```csharp
// OpenCV detection pipeline
Aruco.detectMarkers(grayMat, dictionary, corners, ids, detectorParams);
if (ids.total() > 0)
{
    // Estimate pose from corners
    Aruco.estimatePoseSingleMarkers(corners, markerSize, camMatrix, distCoeffs, rvecs, tvecs);
    // Convert rotation vectors to quaternions
    // Transform to world space
}
```

### 3. Coordinate System Handling

**AprilTag Implementation:**
- Keijiro's library pre-applies coordinate transformations:
  ```csharp
  // From PoseEstimationJob.cs
  pos = pose.t.AsFloat3() * math.float3(1, -1, 1);  // Flip Y
  rot.value * math.float4(-1, 1, -1, 1);  // Flip X,Z quaternion components
  ```
- Returns Unity-compatible camera space coordinates directly

**ArUco Implementation:**
- OpenCV uses its own coordinate system (Y-down, Z-forward)
- Requires manual conversion to Unity's coordinate system (Y-up, Z-forward)
- Additional transformations needed for rotation matrices

### 4. Pose Estimation Approach

**AprilTag:**
```csharp
// Simple direct transformation
var worldPosition = cameraPose.position + cameraPose.rotation * tagPose.Position;
var worldRotation = cameraPose.rotation * tagPose.Rotation;
```

**ArUco (Typical OpenCV approach):**
```csharp
// More complex due to coordinate system differences
// 1. Convert tvec/rvec to Unity coordinates
Vector3 position = new Vector3((float)tvec[0], -(float)tvec[1], (float)tvec[2]);
// 2. Convert rotation vector to matrix, then to quaternion
Mat rotMat = new Mat();
Calib3d.Rodrigues(rvec, rotMat);
Quaternion rotation = ConvertRotationMatrixToQuaternion(rotMat);
// 3. Apply additional transformations for Unity
rotation = Quaternion.Euler(0, 180, 0) * rotation;  // Common adjustment
// 4. Transform to world space
```

### 5. Performance Characteristics

| Metric | AprilTag | ArUco |
|--------|----------|--------|
| **Detection Speed** | Very fast (native C) | Moderate (OpenCV overhead) |
| **Memory Usage** | Low | High (OpenCV libraries) |
| **Accuracy** | High | High |
| **Robustness** | Good | Excellent (more error correction) |

### 6. Camera Intrinsics Handling

**AprilTag:**
- Uses simplified intrinsics from PassthroughCameraUtils
- FOV-based estimation:
  ```csharp
  _focalLength = height / 2 / math.tan(fov / 2);
  _focalCenter = math.double2(width, height) / 2;
  ```

**ArUco:**
- Can use full camera calibration matrix
- Supports distortion coefficients
- More accurate but requires calibration data

### 7. Multi-Marker Support

**AprilTag:**
- Simple ID-based tracking
- Each tracker component specifies target ID
- No built-in multi-marker pose estimation

**ArUco:**
- Supports ChArUco boards (checkerboard + ArUco)
- Can estimate single pose from multiple markers
- Better for large tracking areas

## Implementation Recommendations

### When to Use AprilTag (jp.keijiro.apriltag):
- Need lightweight, fast detection
- Simple single-marker tracking
- Minimal dependencies important
- Android-only deployment

### When to Use ArUco (OpenCV):
- Need multi-platform support
- Require advanced features (boards, refinement)
- Have existing OpenCV pipeline
- Need specific ArUco dictionary compatibility

## Code Structure Comparison

### AprilTag Structure (This Repository):
```
QuestAprilTagTracking.cs     // Main detection loop
├── AprilTagDetector.cs      // Wrapper for native detection
├── AprilTagTracker.cs       // Individual marker tracking
└── PassthroughCameraUtils   // Camera pose utilities
```

### Typical ArUco Structure:
```
ArUcoMarkerTracking.cs       // Main detection loop
├── OpenCV detection         // Direct OpenCV calls
├── Coordinate conversion    // Manual transform logic
└── Marker management        // ID-based object spawning
```

## Migration Considerations

### From ArUco to AprilTag:
1. Generate AprilTag markers (tagStandard41h12 family)
2. Replace OpenCV detection with AprilTagDetector
3. Simplify coordinate transformation (no manual conversion needed)
4. Remove OpenCV dependencies

### From AprilTag to ArUco:
1. Generate ArUco markers with desired dictionary
2. Add OpenCV for Unity package
3. Implement coordinate system conversion
4. Add camera calibration if needed

## Conclusion

Both implementations achieve similar results but with different trade-offs:

- **AprilTag**: Simpler, faster, lighter, Unity-native
- **ArUco**: More features, better documentation, cross-platform

The choice depends on specific project requirements, with AprilTag being ideal for Quest-specific projects prioritizing performance and simplicity, while ArUco suits projects needing advanced features or multi-platform compatibility.