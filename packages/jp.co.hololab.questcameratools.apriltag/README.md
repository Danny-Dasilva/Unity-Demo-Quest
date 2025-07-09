# Quest Camera Tools - AprilTag

High-performance AprilTag tracking for Meta Quest using passthrough cameras.

## Overview

This package provides AprilTag tracking capabilities for Meta Quest devices, offering significant performance improvements over QR code tracking. AprilTags are designed specifically for computer vision applications and provide faster, more robust tracking.

## Performance Benefits

- **2x+ faster detection** compared to QR codes
- **Optimized for tracking** rather than data storage
- **Better robustness** at various distances and angles
- **Lower CPU usage** due to simpler detection algorithms
- **Real-time performance** on mobile processors

## Key Components

### QuestAprilTagTracking
Main tracking component that detects AprilTags in the camera feed.

**Properties:**
- `DetectionFrameRate`: Frame rate for detection (0 = unlimited)
- `Decimation`: Quality vs speed trade-off (1 = full resolution, 2 = half resolution, etc.)
- `TagSize`: Physical size of AprilTag markers in meters

### AprilTagTracker
Tracks individual AprilTag markers and provides pose information.

**Properties:**
- `TargetAprilTagID`: ID of the AprilTag to track (0-586 for tagStandard41h12)
- `RotationConstraint`: Constraint for tag orientation
- `ScaleByPhysicalSize`: Scale object by tag size

### AprilTagDetector
Low-level detection component using the Keijiro AprilTag library.

## Usage

1. **Add QuestAprilTagTracking to scene**
   ```csharp
   var tracking = FindFirstObjectByType<QuestAprilTagTracking>();
   tracking.OnAprilTagDetected += OnTagDetected;
   ```

2. **Create AprilTag tracker**
   ```csharp
   var tracker = GetComponent<AprilTagTracker>();
   tracker.TargetAprilTagID = 0; // Track tag ID 0
   ```

3. **Handle detection events**
   ```csharp
   private void OnTagDetected(List<AprilTagDetectedInfo> tags)
   {
       foreach (var tag in tags)
       {
           Debug.Log($"Detected tag {tag.ID} at {tag.Pose.position}");
       }
   }
   ```

## Migration from QR Codes

| QR Code Feature | AprilTag Equivalent |
|----------------|-------------------|
| QRCodeDetectedInfo.Text | AprilTagDetectedInfo.ID |
| QuestQRTracking | QuestAprilTagTracking |
| QRTracker.TargetQRText | AprilTagTracker.TargetAprilTagID |

## Performance Tuning

- **Decimation**: Set to 2-4 for mobile devices to improve performance
- **Detection Rate**: Limit to 10-30 FPS for better battery life
- **Tag Size**: Larger tags are detected from further distances

## Marker Generation

AprilTags use the `tagStandard41h12` family with IDs 0-586. Generate markers at:
- https://github.com/AprilRobotics/apriltag-imgs
- Or use the AprilTag generator tools

## Requirements

- Unity 2021.3 or later
- Meta XR SDK
- jp.keijiro.apriltag package
- Quest Camera Tools Core package