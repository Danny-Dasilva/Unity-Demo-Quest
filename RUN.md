# AprilTag Migration Setup Guide

This guide walks you through setting up the new high-performance AprilTag tracking system in your Unity scene.

## Prerequisites

- Unity 6000.0 or later
- Meta XR SDK installed
- QuestCameraTools project already set up

## Step 1: Package Manager Setup

The packages should already be configured, but verify installation:

1. **Open Unity Package Manager** (`Window > Package Manager`)
2. **Click refresh** to detect new packages
3. **Verify these packages are installed**:
   - `jp.keijiro.apriltag` (v1.0.2)
   - `jp.co.hololab.questcameratools.apriltag` (v1.0.0)

If packages are missing, check that `manifest.json` contains:
```json
{
  "scopedRegistries": [
    {
      "name": "Keijiro",
      "url": "https://registry.npmjs.com",
      "scopes": ["jp.keijiro"]
    }
  ],
  "dependencies": {
    "jp.keijiro.apriltag": "1.0.2",
    "jp.co.hololab.questcameratools.apriltag": "file:../../../packages/jp.co.hololab.questcameratools.apriltag"
  }
}
```

## Step 2: Scene Setup

### Option A: Add to Existing QR Scene

1. **Open existing scene**: `ArbitraryQRTrackingSample.unity`

2. **Add AprilTag Tracking GameObject**:
   - Create Empty GameObject
   - Name it "AprilTag Tracking"
   - Add Component: `QuestAprilTagTracking`

3. **Configure AprilTag Settings**:
   ```
   Detection Frame Rate: 20
   Decimation: 2
   Tag Size: 0.1
   ```

4. **Add AprilTag Object Spawner**:
   - Create Empty GameObject
   - Name it "AprilTag Spawner"
   - Add Component: `AprilTagObjectSpawner`
   - Create AprilTag prefab (see Step 4)

### Option B: Create New AprilTag Scene

1. **Duplicate scene**: `ArbitraryQRTrackingSample.unity` → `AprilTagTrackingSample.unity`
2. **Replace QR components** with AprilTag equivalents:
   - `QuestQRTracking` → `QuestAprilTagTracking`
   - `QRObjectSpawner` → `AprilTagObjectSpawner`

## Step 3: Create AprilTag Prefab

1. **Create new GameObject**: "AprilTag Object"
2. **Add Components**:
   - `AprilTagTracker`
   - `AprilTagObject`
   - Add visual components (MeshRenderer, etc.)
3. **Configure AprilTagTracker**:
   ```
   Target AprilTag ID: 0
   Anchor Point: Center
   Scale By Physical Size: true
   Rotation Constraint: Any Direction
   ```
4. **Save as Prefab**: `Assets/App/Prefabs/AprilTagObject.prefab`
5. **Assign to Spawner**: Drag prefab to `AprilTagObjectSpawner.aprilTagObjectPrefab`

## Step 4: Generate AprilTag Markers

You need physical AprilTag markers (not QR codes):

1. **Download AprilTags**: https://github.com/AprilRobotics/apriltag-imgs/tree/master/tagStandard41h12
2. **Start with**: `tag41_12_00000.png`, `tag41_12_00001.png`, etc.
3. **Print correctly sized**: If Tag Size = 0.1 in Unity, print tags at 10cm x 10cm
4. **Test with Tag IDs**: 0, 1, 2, 3, 4

## Step 5: Script Integration (Optional)

If you have custom scripts, update them:

```csharp
using HoloLab.QuestCameraTools.AprilTag;

public class MyTrackingScript : MonoBehaviour
{
    private QuestAprilTagTracking aprilTagTracking;
    
    void Start()
    {
        aprilTagTracking = FindFirstObjectByType<QuestAprilTagTracking>();
        if (aprilTagTracking != null)
        {
            aprilTagTracking.OnAprilTagDetected += OnAprilTagDetected;
        }
    }
    
    private void OnAprilTagDetected(List<AprilTagDetectedInfo> tags)
    {
        foreach (var tag in tags)
        {
            Debug.Log($"Detected AprilTag ID: {tag.ID} at position: {tag.Pose.position}");
        }
    }
}
```

## Step 6: Performance Tuning

Optimize based on your needs:

### High Performance (Fast tracking)
```
Detection Frame Rate: 30
Decimation: 4
Tag Size: 0.15 (larger tags)
```

### High Quality (Accurate tracking)
```
Detection Frame Rate: 15
Decimation: 1
Tag Size: 0.1
```

### Balanced (Recommended)
```
Detection Frame Rate: 20
Decimation: 2
Tag Size: 0.1
```

## Step 7: Testing

1. **Build and Deploy** to Quest device
2. **Print test tags** (IDs 0-4) at correct size
3. **Point camera** at printed AprilTags
4. **Verify detection**: Objects should spawn and track smoothly

## Performance Comparison

After setup, you should notice:
- **2x+ faster detection** compared to QR codes
- **More stable tracking** at distance
- **Better performance** with camera movement
- **Lower battery usage**

## Troubleshooting

### No tags detected
- Check Tag Size matches physical markers
- Ensure good lighting
- Try different Decimation values
- Verify AprilTag IDs match (0-586 for tagStandard41h12)

### Poor performance
- Increase Decimation (2-4)
- Lower Detection Frame Rate (10-15)
- Use larger physical tags

### Tracking jitter
- Add filter components to AprilTagTracker
- Enable position/rotation smoothing
- Check lighting conditions

## Migration from QR Codes

| QR Code Component | AprilTag Component |
|-------------------|-------------------|
| `QuestQRTracking` | `QuestAprilTagTracking` |
| `QRTracker` | `AprilTagTracker` |
| `QRObjectSpawner` | `AprilTagObjectSpawner` |
| `TargetQRText` | `TargetAprilTagID` |
| `OnQRCodeDetected` | `OnAprilTagDetected` |

The AprilTag system maintains the same architectural patterns while providing significant performance improvements for spatial tracking applications.