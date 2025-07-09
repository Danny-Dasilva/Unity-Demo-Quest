# PassthroughScreenshotCapture Utility

## Overview

The `PassthroughScreenshotCapture` utility class provides a simple and efficient way to capture screenshots from the Meta Quest passthrough camera feed. It integrates seamlessly with the existing `WebCamTextureManager` from QuestCameraTools and includes optimizations for API usage.

## Features

- **Seamless Integration**: Works with existing `WebCamTextureManager`
- **Image Optimization**: Automatic resizing for API efficiency
- **Error Handling**: Comprehensive error handling for camera unavailability
- **Memory Management**: Proper texture cleanup and memory management
- **Performance Optimized**: Efficient pixel copying and texture handling
- **Configurable**: Adjustable image size limits and quality settings

## Usage

### Basic Setup

1. Add the `PassthroughScreenshotCapture` component to a GameObject in your scene
2. Ensure you have a `WebCamTextureManager` in your scene
3. The component will automatically find the `WebCamTextureManager` if not manually assigned

### Code Examples

#### Basic Screenshot Capture
```csharp
using QuestCameraTools.Utilities;

public class MyClass : MonoBehaviour
{
    private PassthroughScreenshotCapture screenshotCapture;
    
    void Start()
    {
        screenshotCapture = FindObjectOfType<PassthroughScreenshotCapture>();
    }
    
    void CaptureScreenshot()
    {
        var screenshotData = screenshotCapture.CaptureScreenshot();
        
        if (screenshotData.isValid)
        {
            // Use the PNG data
            var pngBytes = screenshotData.pngData;
            var width = screenshotData.width;
            var height = screenshotData.height;
            var timestamp = screenshotData.timestamp;
            
            // Send to API, save to file, etc.
        }
        else
        {
            Debug.LogError("Screenshot capture failed");
        }
    }
}
```

#### Check Camera Status
```csharp
void CheckCameraStatus()
{
    if (screenshotCapture.IsCameraReady())
    {
        var resolution = screenshotCapture.GetCameraResolution();
        var frameRate = screenshotCapture.GetCameraFrameRate();
        var estimatedMemory = screenshotCapture.EstimateMemoryUsage();
        
        Debug.Log($"Camera ready: {resolution.x}x{resolution.y} @ {frameRate}fps");
        Debug.Log($"Estimated memory usage: {estimatedMemory} bytes");
    }
    else
    {
        Debug.LogWarning("Camera not ready for capture");
    }
}
```

#### For API Integration (like Gemini)
```csharp
async void SendToGeminiAPI()
{
    var screenshotData = screenshotCapture.CaptureScreenshot();
    
    if (screenshotData.isValid)
    {
        var base64Image = System.Convert.ToBase64String(screenshotData.pngData);
        
        // Use with Gemini API
        var request = new GeminiRequest
        {
            imageData = base64Image,
            imageMimeType = "image/png"
        };
        
        // Send to API...
    }
}
```

## Configuration

### Inspector Settings

- **Max Image Size**: Maximum dimension for resized images (default: 1024)
- **Enable Image Resize**: Whether to automatically resize large images
- **Texture Format**: Unity texture format to use (default: RGBA32)
- **Web Cam Texture Manager**: Reference to the WebCamTextureManager (auto-found if null)

### Optimization Tips

1. **Image Size**: Keep `maxImageSize` at 1024 or lower for API efficiency
2. **Memory Usage**: Call `EstimateMemoryUsage()` to monitor memory consumption
3. **Frame Timing**: Check `didUpdateThisFrame` before capturing for best results
4. **Error Handling**: Always check `isValid` flag in returned `ScreenshotData`

## ScreenshotData Structure

```csharp
public struct ScreenshotData
{
    public byte[] pngData;      // PNG encoded image data
    public int width;           // Final image width
    public int height;          // Final image height
    public DateTime timestamp;  // Capture timestamp
    public bool isValid;        // Whether capture was successful
}
```

## Error Handling

The utility provides comprehensive error handling for common issues:

- **Camera Not Initialized**: WebCamTextureManager or WebCamTexture is null
- **Permission Denied**: Camera permissions not granted
- **Camera Not Playing**: Camera feed is not active
- **Memory Issues**: Texture creation or pixel access failures
- **Resize Failures**: Image optimization errors

All errors are logged with descriptive messages using the `[PassthroughScreenshotCapture]` prefix.

## Performance Considerations

- **Memory**: Each screenshot creates temporary textures that are cleaned up automatically
- **CPU**: Image resizing uses point sampling for better performance
- **Timing**: Best results when camera `didUpdateThisFrame` is true
- **Frequency**: Avoid capturing screenshots every frame - use reasonable intervals

## Testing

Use the included `PassthroughScreenshotCaptureTest` script to validate functionality:

1. Add the test script to the same GameObject
2. Press Space key to capture manual screenshots
3. Enable automatic testing in the inspector for interval-based captures
4. Check the console for detailed capture information

## Integration with QuestCameraTools

This utility is designed to work seamlessly with the existing QuestCameraTools ecosystem:

- Uses the same `WebCamTextureManager` for camera access
- Follows the same error handling and logging patterns
- Integrates with the existing namespace structure (`QuestCameraTools.Utilities`)
- Compatible with existing assembly definitions

## Common Issues

1. **"WebCamTexture is null"**: Ensure WebCamTextureManager is properly initialized
2. **"Camera not playing"**: Check camera permissions and initialization
3. **"No new frame available"**: Camera feed may be paused or not updating
4. **Large memory usage**: Reduce `maxImageSize` or enable image resize optimization

## Future Enhancements

- Async screenshot capture for better performance
- Multiple format support (JPEG, WebP)
- Batch screenshot capabilities
- Advanced image optimization options
- Integration with Unity's new render pipeline features