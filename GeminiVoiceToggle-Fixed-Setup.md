# Fixed Unity Setup Guide for Gemini Voice Toggle

## ✅ Issues Fixed

The following compilation errors have been resolved:
- ❌ **Fixed**: Invalid GUID in .meta files
- ❌ **Fixed**: Missing 'Uralstech' namespace dependency
- ❌ **Fixed**: Missing 'GenerationConfig' type
- ❌ **Fixed**: Invalid YAML in Audio.meta file

## 🎯 Simplified Setup (No External Dependencies)

### **Step 1: Create Required GameObjects**

#### 1.1 Create SimpleGeminiClient
```
1. Right-click in Hierarchy → Create Empty
2. Name it "SimpleGeminiClient"
3. Add Component → Search "SimpleGeminiClient"
4. Configure:
   - Model: "gemini-1.5-flash"
   - Request Timeout: 30
   - Max Retries: 3
   - Enable Debug Logs: ✓ (for testing)
```

#### 1.2 Create WebCamTextureManager
```
1. Right-click in Hierarchy → Create Empty
2. Name it "WebCamTextureManager"
3. Add Component → Search "WebCamTextureManager"
4. Configure:
   - Eye: Left
   - Requested Resolution: (0,0)
```

#### 1.3 Create PassthroughScreenshotCapture
```
1. Right-click in Hierarchy → Create Empty
2. Name it "PassthroughScreenshotCapture"
3. Add Component → Search "PassthroughScreenshotCapture"
4. Configure:
   - Max Image Size: 1024
   - Enable Image Resize: ✓
   - Web Cam Texture Manager: Drag WebCamTextureManager from hierarchy
```

#### 1.4 Create GeminiTTSManager
```
1. Right-click in Hierarchy → Create Empty
2. Name it "GeminiTTSManager"
3. Add Component → Search "GeminiTTSManager"
4. Configure:
   - Default Voice Name: "en-US-Neural2-J"
   - Audio Encoding: LINEAR16
   - Sample Rate Hertz: 22050
```

### **Step 2: Create Voice Toggle UI**

#### 2.1 Create UI Canvas
```
1. Right-click in Hierarchy → UI → Canvas
2. Canvas settings:
   - Render Mode: World Space
   - Position: (0, 1.5, 2)
   - Scale: (0.001, 0.001, 0.001)
```

#### 2.2 Create Voice Toggle Button
```
1. Right-click on Canvas → UI → Button - TextMeshPro
2. Name it "VoiceToggleButton"
3. Add Component → Audio → Audio Source
4. Add Component → Search "GeminiVoiceToggleSimple"
```

### **Step 3: Create Materials**

Create 4 materials in `Assets/Materials/`:

#### 3.1 Idle Material
```
- Name: "VoiceToggle_Idle"
- Albedo: Light Gray (200, 200, 200, 255)
```

#### 3.2 Recording Material
```
- Name: "VoiceToggle_Recording"
- Albedo: Red (255, 100, 100, 255)
```

#### 3.3 Processing Material
```
- Name: "VoiceToggle_Processing"
- Albedo: Yellow (255, 255, 100, 255)
```

#### 3.4 Playing Material
```
- Name: "VoiceToggle_Playing"
- Albedo: Green (100, 255, 100, 255)
```

### **Step 4: Configure GeminiVoiceToggleSimple**

Select VoiceToggleButton and configure:

#### 4.1 Voice Materials
```
- Idle Material: VoiceToggle_Idle
- Recording Material: VoiceToggle_Recording
- Processing Material: VoiceToggle_Processing
- Playing Material: VoiceToggle_Playing
```

#### 4.2 References (Auto-assigned)
```
- Target Renderer: ✓ (auto-found)
- Target Image: ✓ (auto-found)
- Audio Source: ✓ (auto-found)
- Screenshot Capture: ✓ (auto-found)
- TTS Manager: ✓ (auto-found)
- Gemini Client: ✓ (auto-found)
```

#### 4.3 Audio Settings
```
- Recording Frequency: 16000
- Max Recording Time: 30
```

#### 4.4 API Settings
```
- API Key: (leave empty - uses environment variable)
- Gemini Model: "gemini-1.5-flash"
- Enable Voice Activity: ✓
- Voice Activity Threshold: 0.01
```

### **Step 5: Set API Key**

#### Method 1: Environment Variable (Recommended)
```bash
# Windows Command Prompt:
set GEMINI_API_KEY=your_actual_api_key_here

# Windows PowerShell:
$env:GEMINI_API_KEY="your_actual_api_key_here"

# Mac/Linux Terminal:
export GEMINI_API_KEY="your_actual_api_key_here"
```

#### Method 2: PlayerPrefs (Development)
```csharp
// Create a temporary script with this code and run once:
PlayerPrefs.SetString("GeminiAPIKey", "your_actual_api_key_here");
PlayerPrefs.Save();
```

### **Step 6: Test Setup**

#### 6.1 Verify Hierarchy
Your hierarchy should look like:
```
Scene
├── Canvas (World Space)
│   └── VoiceToggleButton
│       ├── AudioSource
│       └── GeminiVoiceToggleSimple
├── SimpleGeminiClient
├── WebCamTextureManager
├── PassthroughScreenshotCapture
├── GeminiTTSManager
└── Main Camera
```

#### 6.2 Test in Editor
```
1. Press Play
2. Check Console for initialization messages:
   - "[SimpleGeminiClient] Gemini client initialized successfully"
   - "[GeminiVoiceToggle] Initialized on VoiceToggleButton"
3. Click and hold button → Should turn red
4. Speak something → Release button → Should turn yellow
5. Wait for response → Should turn green and play audio
```

### **Step 7: Common Issues & Solutions**

#### "SimpleGeminiClient not found"
```
Solution: Ensure you created the SimpleGeminiClient GameObject with the component
```

#### "No API key found"
```
Solution: Set GEMINI_API_KEY environment variable or use PlayerPrefs method
```

#### "Quest passthrough camera not ready"
```
Solution: This is normal in Unity Editor. Will work properly on Quest device
```

#### "PassthroughScreenshotCapture not found"
```
Solution: Ensure you created the PassthroughScreenshotCapture GameObject
```

### **Step 8: Build for Quest**

#### 8.1 Platform Settings
```
1. File → Build Settings → Android
2. Switch Platform
```

#### 8.2 Player Settings
```
1. Edit → Project Settings → Player
2. Android Settings:
   - Minimum API Level: 29
   - Target API Level: 34
   - Scripting Backend: IL2CPP
   - Target Architectures: ARM64 ✓
```

#### 8.3 XR Settings
```
1. Project Settings → XR Plug-in Management
2. Android tab → OpenXR ✓
3. OpenXR Settings:
   - Meta Quest Support ✓
   - Passthrough ✓
```

## 🎮 Usage Flow

1. **Press & Hold** button → 🔴 Red (recording)
2. **Speak** → Voice activity detected
3. **Release** → 🟡 Yellow (processing with Gemini)
4. **Wait** → AI processes audio + Quest camera image
5. **Listen** → 🟢 Green (TTS plays response)
6. **Done** → ⚪ Gray (idle, ready for next interaction)

## 📂 Files Structure

```
Assets/Scripts/
├── GeminiVoiceToggleSimple.cs (Enhanced)
├── SimpleGeminiClient.cs (New - self-contained)
├── PassthroughScreenshotCapture.cs (Quest camera integration)
├── GeminiTTSManager.cs (TTS integration)
└── [Other existing scripts]
```

## ✅ What's Fixed

- **No external dependencies** - All code is self-contained
- **Proper error handling** - Clear error messages and fallbacks
- **Quest integration** - Proper passthrough camera capture
- **TTS integration** - Full Google Cloud TTS support
- **Voice activity detection** - Real-time audio monitoring
- **Material-based feedback** - Visual state indication

This setup now works without any external package dependencies and provides a complete voice AI interaction system for Quest!