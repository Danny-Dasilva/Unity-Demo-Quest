# Enhanced Gemini Voice Toggle Setup Guide

## Overview
The enhanced `GeminiVoiceToggleSimple.cs` now includes full Quest passthrough camera integration, proper TTS support, and comprehensive voice activity detection. This guide will help you set up the component properly.

## Prerequisites

### 1. Required Components
The enhanced voice toggle requires these components to be present in your scene:

1. **WebCamTextureManager** (from QuestCameraTools)
2. **PassthroughScreenshotCapture** (included)
3. **GeminiTTSManager** (included)
4. **GeminiVoiceToggleSimple** (enhanced version)

### 2. API Keys
You need a Google Gemini API key. Set it using one of these methods:

**Environment Variable (Recommended):**
```bash
export GEMINI_API_KEY="your_api_key_here"
```

**Unity PlayerPrefs (Development):**
```csharp
PlayerPrefs.SetString("GeminiAPIKey", "your_api_key_here");
```

**Inspector (Testing only):**
Set the API key directly in the component inspector.

## Setup Instructions

### Step 1: Add Core Components

1. **Add WebCamTextureManager:**
   - Create an empty GameObject named "WebCamTextureManager"
   - Add the `WebCamTextureManager` component
   - Set the Eye to `Left` or `Right` 
   - Set RequestedResolution to `(0,0)` for highest quality

2. **Add PassthroughScreenshotCapture:**
   - Create an empty GameObject named "PassthroughScreenshotCapture"
   - Add the `PassthroughScreenshotCapture` component
   - Assign the WebCamTextureManager reference
   - Set maxImageSize to 1024 (recommended for API efficiency)

3. **Add GeminiTTSManager:**
   - Create an empty GameObject named "GeminiTTSManager"
   - Add the `GeminiTTSManager` component
   - Configure voice settings as needed

### Step 2: Set Up Voice Toggle Button

1. **Create UI Button:**
   - Add a UI Canvas if you don't have one
   - Create a Button under the Canvas
   - Position it where you want the voice toggle

2. **Add Voice Toggle Component:**
   - Add `GeminiVoiceToggleSimple` component to the button
   - The component will auto-find required references

3. **Configure Materials:**
   Create 4 materials for different states:
   - **Idle Material**: Default microphone icon
   - **Recording Material**: Active/highlighted microphone icon
   - **Processing Material**: Loading/spinner icon
   - **Playing Material**: Speaker/playing icon

4. **Configure References:**
   - **Target Renderer**: Button's Image component renderer
   - **Target Image**: Button's Image component
   - **Audio Source**: Add AudioSource component to button
   - **Screenshot Capture**: Will auto-find PassthroughScreenshotCapture
   - **TTS Manager**: Will auto-find GeminiTTSManager

### Step 3: Configure Settings

**Audio Settings:**
- **Recording Frequency**: 16000 Hz (recommended for speech)
- **Max Recording Time**: 30 seconds
- **Voice Activity Threshold**: 0.01 (adjust based on environment)

**API Settings:**
- **API Key**: Set your Gemini API key
- **Gemini Model**: "gemini-1.5-flash" (default)
- **Enable Voice Activity**: Enable for better UX

## Usage

### Basic Interaction Flow

1. **Press and Hold**: Start recording audio
2. **Release**: Stop recording and process with Gemini
3. **Processing**: Visual feedback shows API processing
4. **Response**: Audio response plays automatically
5. **Interrupt**: Press during playback to stop

### Visual States

- **Idle**: Default microphone icon
- **Recording**: Highlighted microphone with voice activity detection
- **Processing**: Loading animation while API processes
- **Playing**: Speaker icon during TTS playback

### Advanced Features

**Voice Activity Detection:**
- Real-time volume monitoring during recording
- Visual feedback for voice detection
- Configurable sensitivity threshold

**Quest Passthrough Integration:**
- Automatic Quest camera screenshot capture
- Proper Meta XR API integration
- Optimized image sizes for API efficiency

**Enhanced TTS:**
- Google Cloud TTS integration
- Multiple voice options
- Configurable speech parameters
- Proper audio format handling

## Troubleshooting

### Common Issues

1. **"PassthroughScreenshotCapture not found"**
   - Ensure PassthroughScreenshotCapture component is in the scene
   - Check that WebCamTextureManager is properly configured

2. **"GeminiTTSManager not found"**
   - Add GeminiTTSManager component to a GameObject in the scene
   - Ensure it's properly initialized with API key

3. **"No API key available"**
   - Set GEMINI_API_KEY environment variable
   - Or configure API key in PlayerPrefs or inspector

4. **"Quest passthrough camera not ready"**
   - Ensure running on Quest 3/3S with Horizon OS v74+
   - Check camera permissions are granted
   - Verify WebCamTextureManager is active and enabled

5. **"No microphone found"**
   - Check microphone permissions
   - Ensure running on device with microphone access
   - Verify Unity microphone access is enabled

### Performance Tips

1. **Image Optimization:**
   - Keep maxImageSize at 1024 or lower
   - Enable image resize in PassthroughScreenshotCapture

2. **Audio Quality:**
   - Use 16000 Hz for speech recognition
   - Adjust voice activity threshold based on environment

3. **API Efficiency:**
   - Use "gemini-1.5-flash" model for faster responses
   - Keep prompts concise and clear

## Testing

### In Unity Editor
- Component will log warnings about Quest features not available
- Basic recording and TTS functionality will work
- Screenshot capture will fall back to main camera

### On Quest Device
- Full Quest passthrough camera integration
- Real-time voice activity detection
- Complete multimodal AI interaction

## File Structure

```
Assets/Scripts/
├── GeminiVoiceToggleSimple.cs (Enhanced version)
├── PassthroughScreenshotCapture.cs (Quest camera integration)
├── GeminiTTSManager.cs (TTS integration)
└── Gemini/ (Additional Gemini utilities)
    ├── Audio/
    ├── GeminiAudioDemo.cs
    └── UGeminiIntegrationManager.cs
```

## Next Steps

1. Test the basic functionality in Unity Editor
2. Build and test on Quest device
3. Adjust materials and UI positioning as needed
4. Configure voice and TTS settings for your use case
5. Add additional visual feedback or customizations as needed

The enhanced implementation provides a complete, production-ready voice AI interaction system for Quest with proper passthrough camera integration and TTS support.