# Gemini Voice Toggle - UnityEvents Setup Guide

## ✅ **Issue Fixed!**

I've added public methods that show up in Unity's Inspector, just like your `AudioToggle.ToggleAudio()` method.

## 🎯 **Available Public Methods (Now Visible in Inspector)**

When you select a GameObject with `GeminiVoiceToggleSimple`, you'll now see these methods in UnityEvents:

### **1. StartVoiceRecording()**
```
What it does: Starts voice recording
When to use: On button press/down events
Visual feedback: Button turns RED
```

### **2. StopVoiceRecording()**
```
What it does: Stops recording and processes with AI
When to use: On button release/up events
Visual feedback: Button turns YELLOW → GREEN → GRAY
```

### **3. ToggleVoiceRecording()**
```
What it does: Single-click alternative (like your AudioToggle.ToggleAudio)
When to use: For single-click buttons instead of press-and-hold
Behavior: 
- If idle → start recording
- If recording → stop and process
- If playing → stop playback
```

### **4. StopAudioPlayback()**
```
What it does: Immediately stops any TTS audio playing
When to use: Emergency stop or cancel button
Visual feedback: Button returns to GRAY (idle)
```

---

## 🔧 **Setup Options - Choose Your Preferred Method**

### **Option A: Press and Hold (Recommended)**
```
Following the push-to-talk pattern:

BasicPokeButtonPressAudio Events:
├─ On Pointer Down → GeminiVoiceToggleSimple.StartVoiceRecording()
└─ On Pointer Up → GeminiVoiceToggleSimple.StopVoiceRecording()

User Experience:
1. Press and hold button → Starts recording (RED)
2. Speak your question
3. Release button → Processes and responds (YELLOW → GREEN → GRAY)
```

### **Option B: Single Click Toggle (Like AudioToggle)**
```
Following your AudioToggle pattern:

BasicPokeButtonPressAudio Events:
└─ On Pointer Click → GeminiVoiceToggleSimple.ToggleVoiceRecording()

User Experience:
1. Click button → Starts recording (RED)
2. Speak your question  
3. Click button again → Processes and responds (YELLOW → GREEN → GRAY)
```

### **Option C: Separate Start/Stop Buttons**
```
If you want separate buttons:

Start Button:
└─ On Pointer Click → GeminiVoiceToggleSimple.StartVoiceRecording()

Stop Button:
└─ On Pointer Click → GeminiVoiceToggleSimple.StopVoiceRecording()

Cancel Button (optional):
└─ On Pointer Click → GeminiVoiceToggleSimple.StopAudioPlayback()
```

---

## 📋 **Step-by-Step Setup (Following Your AudioToggle Pattern)**

### **Step 1: Create Your Button**
```
1. Create/find your VR button
2. Add these components:
   ├─ BasicPokeButtonPressAudio (for VR interaction)
   ├─ AudioSource (for TTS playback)
   └─ GeminiVoiceToggleSimple (for voice functionality)
```

### **Step 2: Configure Events (Choose Option A or B)**

#### **Option A: Press and Hold Setup**
```
Select BasicPokeButtonPressAudio component:

Pointer Events:
├─ On Pointer Down ()
│  └─ Add Event → GeminiVoiceToggleSimple.StartVoiceRecording
└─ On Pointer Up ()
   └─ Add Event → GeminiVoiceToggleSimple.StopVoiceRecording
```

#### **Option B: Single Click Setup (Like AudioToggle)**
```
Select BasicPokeButtonPressAudio component:

Pointer Events:
└─ On Pointer Click ()
   └─ Add Event → GeminiVoiceToggleSimple.ToggleVoiceRecording
```

### **Step 3: Configure Materials**
```
In GeminiVoiceToggleSimple component:

Voice Materials:
├─ Idle Material → Gray material (ready state)
├─ Recording Material → Red material (recording state)
├─ Processing Material → Yellow material (AI processing)
└─ Playing Material → Green material (TTS playing)
```

### **Step 4: Test Your Setup**
```
Press Play and test:

Option A (Press and Hold):
1. Press button → RED (recording starts)
2. Speak → Voice activity detected
3. Release → YELLOW (processing) → GREEN (playing) → GRAY (idle)

Option B (Single Click):
1. Click button → RED (recording starts)
2. Speak → Voice activity detected
3. Click again → YELLOW (processing) → GREEN (playing) → GRAY (idle)
```

---

## 🎮 **Visual State Feedback**

```
🔘 GRAY (Idle) → Ready to record
🔴 RED (Recording) → Currently recording audio
🟡 YELLOW (Processing) → AI is processing request
🟢 GREEN (Playing) → TTS response is playing
```

---

## 🛠️ **Inspector View**

When you select your button with `GeminiVoiceToggleSimple`, you'll now see:

```
GeminiVoiceToggleSimple Component:
├─ Voice Materials (4 material slots)
├─ References (auto-assigned)
├─ Audio Settings 
├─ API Settings
└─ [When adding to UnityEvents, you'll see:]
    ├─ StartVoiceRecording()
    ├─ StopVoiceRecording()
    ├─ ToggleVoiceRecording()
    └─ StopAudioPlayback()
```

---

## 📝 **Complete Example Setup**

### **Your Button GameObject Should Have:**
```
VoiceButton (GameObject)
├─ BasicPokeButtonPressAudio
│  ├─ On Pointer Down → GeminiVoiceToggleSimple.StartVoiceRecording
│  └─ On Pointer Up → GeminiVoiceToggleSimple.StopVoiceRecording
├─ AudioSource
├─ Image (for visual feedback)
└─ GeminiVoiceToggleSimple
   ├─ Voice Materials: [4 materials assigned]
   ├─ References: [auto-assigned]
   ├─ Audio Settings: [configured]
   └─ API Settings: [configured]
```

### **Your Scene Should Have:**
```
Scene
├─ VoiceButton (with above setup)
├─ SimpleGeminiClient
├─ SimpleWebCamManager
├─ PassthroughScreenshotCapture
├─ GeminiTTSManager
└─ [Other objects]
```

---

## 🔍 **Troubleshooting**

### **"I don't see the methods in UnityEvents"**
```
Solution: 
1. Make sure you've recompiled the script
2. Check that GeminiVoiceToggleSimple is attached to the GameObject
3. Look for: StartVoiceRecording, StopVoiceRecording, ToggleVoiceRecording, StopAudioPlayback
```

### **"Button doesn't respond"**
```
Solution:
1. Check that events are properly assigned in BasicPokeButtonPressAudio
2. Verify all 4 supporting GameObjects are in the scene
3. Check Console for initialization messages
```

### **"No visual feedback"**
```
Solution:
1. Assign all 4 materials in GeminiVoiceToggleSimple
2. Ensure button has Image or MeshRenderer component
3. Check that materials are using appropriate shaders
```

---

## 🎯 **Summary**

Now `GeminiVoiceToggleSimple` works exactly like your `AudioToggle.ToggleAudio()` method:

- **Public methods** visible in Unity Inspector
- **UnityEvent compatible** for VR interactions
- **Flexible setup** - choose press-and-hold or single-click
- **Same pattern** as your existing audio toggle system

You can now set up the Gemini voice button following the exact same pattern as your AudioToggle! 🎉