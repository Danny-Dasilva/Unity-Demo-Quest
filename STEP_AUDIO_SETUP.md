# Step-Based Audio Playback Setup Guide

This guide explains how to configure the new audio playback system that plays WAV files for each step with a 1-second delay.

## Overview

The system adds audio playback capabilities to the existing step navigation system. Each step can have its own audio file that plays automatically when the user navigates to that step.

## Components Added

### 1. StepAudioPlayer.cs
A new component that handles audio playback with the following features:
- 1-second default delay before playing audio
- Configurable delay per step
- Automatic cancellation when changing steps quickly
- Fade out option for smooth transitions

### 2. Enhanced StepModel in StepBasedModelSwitcher.cs
Added an `audioClip` field to the existing StepModel class to associate audio with each step.

## Setup Instructions

### Method 1: Using StepAudioPlayer with StepCounter

1. **Find the GameObject with StepCounter component**
   - This is typically named "StepCounter" or similar in your scene

2. **Add Components:**
   - Add `StepAudioPlayer` component
   - Add `AudioSource` component (if not already present)

3. **Configure StepAudioPlayer:**
   - **Step Counter**: Should auto-detect (leave empty)
   - **Audio Source**: Should auto-detect (leave empty)
   - **Default Audio Delay**: 1.0 (seconds)
   - **Stop Audio On Step Change**: ✓ (checked)
   - **Fade Out Duration**: 0.2 (seconds)

4. **Add Step Audio Clips:**
   - Expand "Step Audio Clips"
   - Set Size to match your number of steps
   - For each element:
     - **Step Number**: Enter the step number (1, 2, 3, etc.)
     - **Audio Clip**: Drag your .wav file here
     - **Custom Delay**: Leave at -1 to use default, or set specific delay

### Method 2: Using Audio in StepBasedModelSwitcher

1. **Find the GameObject with StepBasedModelSwitcher**

2. **In the Step Models list:**
   - Each step now has an "Audio Clip" field
   - Drag your .wav files to the appropriate steps

3. **Add StepAudioPlayer to sync:**
   - Add the StepAudioPlayer component as described above
   - It will automatically detect and play audio from StepBasedModelSwitcher

## Audio File Requirements

- **Format**: WAV files recommended (MP3, OGG also supported)
- **Location**: Place in Assets/Audio/ or similar folder
- **Import Settings**: Unity's default audio import settings work fine

## Testing

1. **Enter Play Mode**
2. **Use navigation buttons** to change steps
3. **Audio should play** 1 second after arriving at each step
4. **Check Console** for debug messages if audio doesn't play

## Troubleshooting

### Audio Not Playing
- Check Console for error messages
- Ensure AudioSource component is present
- Verify audio files are assigned to steps
- Check AudioSource volume and mute settings

### Audio Plays Immediately
- Verify "Default Audio Delay" is set to 1.0
- Check if specific step has custom delay override

### Multiple Audio Playing
- Ensure "Stop Audio On Step Change" is checked
- Verify only one StepAudioPlayer component exists

## Advanced Configuration

### Per-Step Custom Delays
Set different delays for specific steps:
- Step 1: 0.5 seconds (quick intro)
- Step 5: 2.0 seconds (dramatic pause)
- Others: Use default 1.0 second

### Audio Source Settings
Configure the AudioSource component for:
- **Spatial Blend**: 0 for 2D, 1 for 3D sound
- **Volume**: Adjust master volume
- **Priority**: Set importance (0-255)

## Debug Tools

### Runtime Debugging
- Select GameObject with StepAudioPlayer
- Right-click component header → "Debug Audio Configuration"
- View all configured audio clips and settings in Console

### Testing Specific Steps
- In StepCounter, manually set "Current Step" value
- Audio for that step will play after delay

## No Breaking Changes

All existing functionality remains intact:
- Model switching works as before
- Navigation buttons function normally
- Step counter displays unchanged
- Audio is purely additive feature

Steps without audio clips assigned will simply not play any sound.