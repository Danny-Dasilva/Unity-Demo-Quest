# Part Highlighting System Setup Guide

## Quick Setup (Minimal Configuration)

### 1. Add Integration to StepBasedModelSwitcher GameObject
- Find the GameObject with `StepBasedModelSwitcher` component
- Add `ModelHighlightIntegration` component to the same GameObject
- This will automatically add highlighting to all models

### 2. Create Highlight Button
- Create a new UI Button or 3D object to act as button
- Add `HighlightButton` component
- Add a Collider if using 3D object
- The button will automatically find and control the active model's highlights

### 3. Test
- Enter Play mode
- Click the highlight button to cycle through parts
- Each part will glow red while others become 50% transparent

## How It Works

### PartHighlighter.cs
- Finds all MeshRenderer components in child objects
- Creates dynamic materials: transparent (50% opacity) and highlight (red)
- Applies materials based on selection
- No manual material setup needed

### HighlightButton.cs
- Implements IPointerClickHandler for VR interaction
- Cycles through parts on click
- Automatically finds the active model's PartHighlighter
- Works with Quest hand tracking and controllers

### ModelHighlightIntegration.cs
- Monitors step changes
- Automatically adds PartHighlighter to new models
- Links button to current model
- Handles cleanup between steps

## Advanced Configuration

### Custom Colors
In PartHighlighter inspector:
- `Highlight Color`: Change from red to any color
- `Transparent Alpha`: Adjust transparency (0.5 = 50%)

### Step-Specific Highlights
In ModelHighlightIntegration:
- Enable `Use Step Specific Highlights`
- Add entries for steps with:
  - Step number
  - Part name to highlight
  - Custom color for that step

### Manual Control via Code
```csharp
// Find the highlighter
PartHighlighter highlighter = model.GetComponent<PartHighlighter>();

// Highlight specific part by index
highlighter.HighlightPart(0);

// Highlight by name (partial match)
highlighter.HighlightPartByName("battery");

// Cycle through parts
highlighter.HighlightNextPart();
```

## AI Integration (Future)
The system is ready for AI control:
```csharp
// In your Gemini response handler:
string partName = geminiResponse; // e.g., "battery"
highlightButton.HighlightPartByName(partName);
```

## Troubleshooting

### No parts highlighting
- Check console for part count in debug logs
- Ensure model has separate child objects with MeshRenderers
- Verify materials are not using special shaders

### Materials look wrong
- Standard shader is required
- Custom shaders may not support transparency
- Check original material properties in debug logs

### Button not working
- Ensure EventSystem exists in scene (for UI)
- Add collider to 3D button objects
- Check HighlightButton finds the highlighter (see console)

## Testing with 456.fbx
1. Set step to use 456.fbx in StepBasedModelSwitcher
2. The model's 3 parts will be automatically detected
3. Click button to cycle: Part 1 (red) → Part 2 (red) → Part 3 (red)
4. Non-highlighted parts appear 50% transparent