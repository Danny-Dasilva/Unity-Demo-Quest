# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

QuestCameraTools-Unity is a Unity library for Meta Quest Passthrough Camera API integration, enabling spatial alignment using QR codes or Immersal localization. Built with Unity 6000.1.8f1.

## Development Commands

### Unity Setup
- Open project in Unity 6000.0 or later
- Target platform: Android (Meta Quest)
- Ensure Android build support is installed
- Enable Depth API in XR settings

### Package Installation
Add to manifest.json:
```json
"scopedRegistries": [
  {
    "name": "Keijiro",
    "url": "https://registry.npmjs.com",
    "scopes": ["jp.keijiro"]
  }
],
"dependencies": {
  "jp.co.hololab.questcameratools.core": "https://github.com/HoloLabInc/QuestCameraTools-Unity.git?path=packages/jp.co.hololab.questcameratools.core",
  "jp.co.hololab.questcameratools.qr": "https://github.com/HoloLabInc/QuestCameraTools-Unity.git?path=packages/jp.co.hololab.questcameratools.qr",
  "jp.co.hololab.questcameratools.qr.libraries": "https://github.com/HoloLabInc/QuestCameraTools-Unity.git?path=packages/jp.co.hololab.questcameratools.qr.libraries",
  "jp.co.hololab.questcameratools.apriltag": "file:../../../packages/jp.co.hololab.questcameratools.apriltag",
  "jp.keijiro.apriltag": "1.0.2"
}
```

### Building
- Build for Android platform
- Main scenes: ArbitraryQRTrackingSample.unity, SpecificQRTrackingSample.unity
- Ensure Meta XR SDK dependencies are resolved

## Architecture

### Package Structure
```
packages/
├── jp.co.hololab.questcameratools.core/     # Core passthrough camera functionality
├── jp.co.hololab.questcameratools.qr/        # QR code tracking implementation
├── jp.co.hololab.questcameratools.qr.libraries/  # ZXing libraries
├── jp.co.hololab.questcameratools.apriltag/  # AprilTag tracking implementation
└── jp.co.hololab.questcameratools.immersal/  # Immersal integration
```

### Key Components

#### QR Code Tracking (Legacy)
- **QuestQRTracking**: Main QR tracking component with event-based detection system
- **QRDetector**: Handles QR code detection using ZXing library with async processing
- **QRTracker**: Individual QR code tracker with position stabilization and filtering

#### AprilTag Tracking (Recommended)
- **QuestAprilTagTracking**: High-performance AprilTag tracking with 2x+ speed improvement
- **AprilTagDetector**: Optimized AprilTag detection using Keijiro's native implementation
- **AprilTagTracker**: Individual AprilTag tracker with ID-based matching
- **AprilTagObjectSpawner**: Spawns objects based on detected AprilTag IDs

#### Core Components
- **WebCamTextureManager**: Manages passthrough camera texture access and lifecycle
- **PassthroughCameraViewer**: Visualization component for camera feed display

### Architectural Patterns
- Event-driven architecture with OnQRCodeDetected callbacks
- Async/await pattern for non-blocking detection loops
- Component-based design with modular prefabs
- Filter chain pattern for QR validation (AspectRatioFilter, ZScoreFilter)
- Proper resource cleanup with CancellationToken usage

### Extension Points
- **QR Code System**: Extend AbstractFilterComponent, modify QRObject prefabs, subscribe to OnQRCodeDetected events
- **AprilTag System**: Extend AprilTag.AbstractFilterComponent, modify AprilTagObject prefabs, subscribe to OnAprilTagDetected events
- **Performance Tuning**: Adjust AprilTag decimation factor and detection frame rate

### Performance Comparison
| Feature | QR Codes | AprilTags |
|---------|----------|-----------|
| Detection Speed | Baseline | 2x+ faster |
| Data Storage | URLs, text | IDs only (0-586) |
| Robustness | Good | Better at distance |
| CPU Usage | Higher | Lower |
| Use Case | Data encoding | Spatial tracking |

### Migration Guide: QR to AprilTag
1. Replace `QuestQRTracking` with `QuestAprilTagTracking`
2. Replace `QRTracker.TargetQRText` with `AprilTagTracker.TargetAprilTagID`
3. Replace QR code markers with AprilTag markers (tagStandard41h12)
4. Update event handlers from `OnQRCodeDetected` to `OnAprilTagDetected`

## Configuration

### Required Settings
- **Platform**: Android
- **XR Settings**: Meta OpenXR with Depth API enabled
- **Render Pipeline**: Universal Render Pipeline (Mobile preset)
- **Input System**: Both legacy and new input system supported

### Key Configuration Files
- unity/QRTracking-6000/Assets/XR/Settings/OpenXR Package Settings.asset
- unity/QRTracking-6000/Assets/OculusProjectConfig.asset
- unity/QRTracking-6000/Assets/Settings/Mobile_RPAsset.asset
- unity/QRTracking-6000/ProjectSettings/ProjectSettings.asset