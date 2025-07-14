# Detailed Gemini Voice Button Setup for VR (Following Your AudioToggle Pattern)

## Understanding Your Current Pattern

Based on your AudioToggle setup, you have:
- `BasicPokeButtonPressAudio` component for VR interaction
- `AudioToggle` script with the actual functionality
- Runtime event calling `AudioToggle.ToggleAudio()`

## Key Differences for Gemini Voice Button

### **Interaction Pattern:**
- **AudioToggle**: Single click → toggle state
- **GeminiVoiceToggle**: Press and hold → record → release → process

### **Dependencies:**
- **AudioToggle**: Self-contained, no dependencies
- **GeminiVoiceToggle**: Requires 4 supporting GameObjects

### **Visual States:**
- **AudioToggle**: 2 materials (muted/unmuted)
- **GeminiVoiceToggle**: 4 materials (idle/recording/processing/playing)

---

## Step 1: Create Supporting GameObjects (Scene Setup)

### 1.1 Create SimpleGeminiClient
```
In Unity Hierarchy:
1. Right-click → Create Empty
2. Name: "SimpleGeminiClient"
3. Add Component → Search "SimpleGeminiClient"
4. Configure in Inspector:
   ┌─────────────────────────────────┐
   │ API Configuration               │
   │ ├─ Api Key: (leave empty)       │
   │ ├─ Model: "gemini-1.5-flash"    │
   │ ├─ Request Timeout: 30          │
   │ └─ Max Retries: 3               │
   │                                 │
   │ Debug                           │
   │ └─ Enable Debug Logs: ✓         │
   └─────────────────────────────────┘
```

### 1.2 Create SimpleWebCamManager
```
In Unity Hierarchy:
1. Right-click → Create Empty
2. Name: "SimpleWebCamManager"
3. Add Component → Search "SimpleWebCamManager"
4. Configure in Inspector:
   ┌─────────────────────────────────┐
   │ Camera Settings                 │
   │ ├─ Requested Width: 1280        │
   │ ├─ Requested Height: 720        │
   │ └─ Requested FPS: 30            │
   │                                 │
   │ Debug                           │
   │ └─ Enable Debug Logs: ✓         │
   └─────────────────────────────────┘
```

### 1.3 Create PassthroughScreenshotCapture
```
In Unity Hierarchy:
1. Right-click → Create Empty
2. Name: "PassthroughScreenshotCapture"
3. Add Component → Search "PassthroughScreenshotCapture"
4. Configure in Inspector:
   ┌─────────────────────────────────┐
   │ Image Optimization              │
   │ ├─ Max Image Size: 1024         │
   │ ├─ Enable Image Resize: ✓       │
   │ └─ Texture Format: RGBA32       │
   │                                 │
   │ References                      │
   │ └─ Web Cam Manager: [Drag       │
   │    "SimpleWebCamManager" from   │
   │    hierarchy here]              │
   └─────────────────────────────────┘
```

### 1.4 Create GeminiTTSManager
```
In Unity Hierarchy:
1. Right-click → Create Empty
2. Name: "GeminiTTSManager"
3. Add Component → Search "GeminiTTSManager"
4. Configure in Inspector:
   ┌─────────────────────────────────┐
   │ TTS Configuration               │
   │ ├─ Default Voice Name:          │
   │ │  "en-US-Neural2-J"            │
   │ ├─ Default Language Code:       │
   │ │  "en-US"                      │
   │ ├─ Default Speaking Rate: 1.0   │
   │ ├─ Default Pitch: 0.0           │
   │ └─ Default Volume Gain: 0.0     │
   │                                 │
   │ Audio Settings                  │
   │ ├─ Audio Encoding: LINEAR16     │
   │ ├─ Sample Rate Hertz: 22050     │
   │ └─ Audio Channels: 1            │
   │                                 │
   │ Debug                           │
   │ └─ Enable Debug Logs: ✓         │
   └─────────────────────────────────┘
```

---

## Step 2: Create the 4 Materials (Visual Feedback)

### 2.1 Create Materials Folder
```
In Project Window:
1. Right-click Assets → Create → Folder
2. Name: "GeminiVoiceMaterials"
```

### 2.2 Create Each Material
```
In GeminiVoiceMaterials folder, create 4 materials:

1. VoiceToggle_Idle (Gray - Ready state):
   ┌─────────────────────────────────┐
   │ Material Settings               │
   │ ├─ Name: "VoiceToggle_Idle"     │
   │ ├─ Shader: Standard            │
   │ ├─ Albedo Color:               │
   │ │  R: 200, G: 200, B: 200, A: 255 │
   │ └─ Metallic: 0, Smoothness: 0.5 │
   └─────────────────────────────────┘

2. VoiceToggle_Recording (Red - Recording state):
   ┌─────────────────────────────────┐
   │ Material Settings               │
   │ ├─ Name: "VoiceToggle_Recording"│
   │ ├─ Shader: Standard            │
   │ ├─ Albedo Color:               │
   │ │  R: 255, G: 100, B: 100, A: 255 │
   │ └─ Metallic: 0, Smoothness: 0.5 │
   └─────────────────────────────────┘

3. VoiceToggle_Processing (Yellow - AI processing):
   ┌─────────────────────────────────┐
   │ Material Settings               │
   │ ├─ Name: "VoiceToggle_Processing"│
   │ ├─ Shader: Standard            │
   │ ├─ Albedo Color:               │
   │ │  R: 255, G: 255, B: 100, A: 255 │
   │ └─ Metallic: 0, Smoothness: 0.5 │
   └─────────────────────────────────┘

4. VoiceToggle_Playing (Green - TTS playing):
   ┌─────────────────────────────────┐
   │ Material Settings               │
   │ ├─ Name: "VoiceToggle_Playing"  │
   │ ├─ Shader: Standard            │
   │ ├─ Albedo Color:               │
   │ │  R: 100, G: 255, B: 100, A: 255 │
   │ └─ Metallic: 0, Smoothness: 0.5 │
   └─────────────────────────────────┘
```

---

## Step 3: Set Up the Button (Following Your Pattern)

### 3.1 Create/Configure Your Button
```
In Unity Hierarchy:
1. Find your existing button setup OR create new button
2. Make sure it has these components:
   ├─ BasicPokeButtonPressAudio (for VR interaction)
   ├─ AudioSource (for TTS playback)
   ├─ Image or MeshRenderer (for visual feedback)
   └─ GeminiVoiceToggleSimple (ADD THIS - main functionality)
```

### 3.2 Add GeminiVoiceToggleSimple Component
```
Select your button and:
1. Add Component → Search "GeminiVoiceToggleSimple"
2. Configure in Inspector:

   ┌─────────────────────────────────┐
   │ Voice Materials                 │
   │ ├─ Idle Material: [Drag         │
   │ │  VoiceToggle_Idle]            │
   │ ├─ Recording Material: [Drag    │
   │ │  VoiceToggle_Recording]       │
   │ ├─ Processing Material: [Drag   │
   │ │  VoiceToggle_Processing]      │
   │ └─ Playing Material: [Drag      │
   │    VoiceToggle_Playing]         │
   └─────────────────────────────────┘

   ┌─────────────────────────────────┐
   │ References                      │
   │ ├─ Target Renderer: (auto-assigned) │
   │ ├─ Target Image: (auto-assigned)    │
   │ ├─ Audio Source: (auto-assigned)    │
   │ ├─ Screenshot Capture: (auto-assigned) │
   │ ├─ TTS Manager: (auto-assigned)     │
   │ └─ Gemini Client: (auto-assigned)   │
   └─────────────────────────────────┘

   ┌─────────────────────────────────┐
   │ Audio Settings                  │
   │ ├─ Recording Frequency: 16000   │
   │ └─ Max Recording Time: 30       │
   └─────────────────────────────────┘

   ┌─────────────────────────────────┐
   │ API Settings                    │
   │ ├─ API Key: (leave empty)       │
   │ ├─ Gemini Model: "gemini-1.5-flash" │
   │ ├─ Enable Voice Activity: ✓     │
   │ └─ Voice Activity Threshold: 0.01 │
   └─────────────────────────────────┘
```

---

## Step 4: VR Interaction Setup (Key Difference from AudioToggle)

### 4.1 Understanding the Interaction Pattern
```
AudioToggle Pattern:
- Single click → ToggleAudio()
- Uses IPointerClickHandler

GeminiVoiceToggle Pattern:
- Press and hold → OnPointerDown() → Start recording
- Release → OnPointerUp() → Stop recording and process
- Uses IPointerDownHandler and IPointerUpHandler
```

### 4.2 VR Event Configuration Options

**Option A: Direct VR Interaction (Recommended)**
```
GeminiVoiceToggleSimple already implements:
- IPointerDownHandler
- IPointerUpHandler

Your VR system should automatically call:
- OnPointerDown() when button is pressed
- OnPointerUp() when button is released

No additional event setup needed!
```

**Option B: Manual Event Setup (If needed)**
```
If you need manual events on BasicPokeButtonPressAudio:

Press Events:
- OnPointerDown → GeminiVoiceToggleSimple.OnPointerDown()

Release Events:
- OnPointerUp → GeminiVoiceToggleSimple.OnPointerUp()
```

---

## Step 5: API Key Configuration

### 5.1 Environment Variable Method (Recommended for Production)
```
In Terminal/Command Prompt before starting Unity:

Windows Command Prompt:
set GEMINI_API_KEY=your_actual_api_key_here

Windows PowerShell:
$env:GEMINI_API_KEY="your_actual_api_key_here"

Mac/Linux Terminal:
export GEMINI_API_KEY="your_actual_api_key_here"
```

### 5.2 PlayerPrefs Method (Development/Testing)
```
Create a temporary script and run once:

using UnityEngine;

public class SetAPIKey : MonoBehaviour
{
    void Start()
    {
        PlayerPrefs.SetString("GeminiAPIKey", "your_actual_api_key_here");
        PlayerPrefs.Save();
        Debug.Log("API Key set in PlayerPrefs");
    }
}
```

### 5.3 Inspector Method (Testing Only - Not Secure)
```
In GeminiVoiceToggleSimple component:
- API Key field: Paste your key directly
- WARNING: This will be visible in builds!
```

---

## Step 6: Final Hierarchy Check

### 6.1 Your Scene Should Look Like:
```
Scene Hierarchy:
├── [Your existing VR setup]
├── [Your UI Canvas/Button with:]
│   ├── BasicPokeButtonPressAudio
│   ├── AudioSource
│   ├── Image (or MeshRenderer)
│   └── GeminiVoiceToggleSimple
├── SimpleGeminiClient
├── SimpleWebCamManager
├── PassthroughScreenshotCapture
├── GeminiTTSManager
└── [Other scene objects]
```

### 6.2 Materials Should Be:
```
Project Window:
Assets/
├── GeminiVoiceMaterials/
│   ├── VoiceToggle_Idle.mat
│   ├── VoiceToggle_Recording.mat
│   ├── VoiceToggle_Processing.mat
│   └── VoiceToggle_Playing.mat
└── [Other assets]
```

---

## Step 7: Testing & Verification

### 7.1 Editor Test
```
Press Play in Unity Editor:

Console Should Show:
✓ "[SimpleGeminiClient] Gemini client initialized successfully"
✓ "[SimpleWebCamManager] Camera initialized successfully"
✓ "[GeminiVoiceToggle] Initialized on [ButtonName]"
✓ All components should auto-assign references
```

### 7.2 Functionality Test
```
Interaction Flow:
1. Press and hold button → Button turns RED (recording)
2. Speak something → Voice activity detected (console logs)
3. Release button → Button turns YELLOW (processing)
4. Wait for AI → API processes audio + camera image
5. Response ready → Button turns GREEN (TTS playing)
6. Audio finishes → Button returns to GRAY (idle)
```

---

## Step 8: Usage Instructions

### 8.1 How Users Interact:
```
1. PRESS AND HOLD button (like push-to-talk)
2. SPEAK your question/command
3. RELEASE button
4. WAIT for AI response
5. LISTEN to TTS response
6. REPEAT as needed
```

### 8.2 Visual Feedback:
```
🔘 GRAY (Idle) → Ready to record
🔴 RED (Recording) → Currently recording audio
🟡 YELLOW (Processing) → AI is processing request
🟢 GREEN (Playing) → TTS response is playing
```

---

## Common Issues & Solutions

### Issue 1: "Component not found" errors
```
Solution: Ensure all 4 supporting GameObjects are in the scene:
- SimpleGeminiClient
- SimpleWebCamManager  
- PassthroughScreenshotCapture
- GeminiTTSManager
```

### Issue 2: "No API key available" error
```
Solution: Set API key using one of the 3 methods above
Check Console for: "[SimpleGeminiClient] Gemini client initialized successfully"
```

### Issue 3: Button doesn't respond to VR interaction
```
Solution: Ensure button has:
- BasicPokeButtonPressAudio component
- GeminiVoiceToggleSimple component
- Both should work together automatically
```

### Issue 4: No visual feedback
```
Solution: Check that all 4 materials are:
- Created and assigned in GeminiVoiceToggleSimple
- Button has Image or MeshRenderer component
- Materials are using Standard shader
```

---

## Summary: Key Differences from AudioToggle

| Aspect | AudioToggle | GeminiVoiceToggle |
|--------|-------------|-------------------|
| **Interaction** | Single click | Press and hold |
| **Dependencies** | None | 4 supporting GameObjects |
| **Materials** | 2 (muted/unmuted) | 4 (idle/recording/processing/playing) |
| **Functionality** | Volume control | AI voice interaction |
| **Setup Complexity** | Simple | Moderate |

The setup is more complex than AudioToggle, but follows the same component-based pattern you're already using. Once set up, it provides a complete voice AI interaction system for your VR application!