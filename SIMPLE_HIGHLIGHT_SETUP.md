# Simple Part Highlighting - Quick Setup for 456 Object

## Minimal Setup (2 Steps)

### Step 1: Add Highlighter to 456 Object
1. Find your `456` object under the BuildingBlock/Cube
2. Select the `456` GameObject
3. Add Component → `SimplePartHighlighter`
4. That's it! The script auto-detects all child parts

### Step 2: Create Highlight Button
1. Create any GameObject to act as button (Cube, UI Button, etc.)
2. Add Component → `SimpleHighlightButton`
3. It will auto-find your 456 object
4. Add a Collider if using 3D object

**Done!** Click the button in Play mode to cycle through parts.

## How It Works

- **First click**: Part 1 glows red, Parts 2-3 become 50% transparent
- **Second click**: Part 2 glows red, Parts 1,3 become 50% transparent  
- **Third click**: Part 3 glows red, Parts 1-2 become 50% transparent
- **Fourth click**: Cycles back to Part 1

## Auto-Detection Features

The `SimpleHighlightButton` automatically:
- Searches for object named "456"
- Looks under BuildingBlock/Cube first
- Adds SimplePartHighlighter if missing
- No manual linking required!

## Manual Setup (Optional)

If auto-detection doesn't work:
1. On SimpleHighlightButton, drag your 456 object to "Target Object" field
2. Or drag the SimplePartHighlighter component to "Target Highlighter" field

## Testing in Unity Editor

### Without Play Mode:
1. Select 456 object with SimplePartHighlighter
2. Right-click component → "Test Cycle"
3. Watch materials change in Scene view

### With Play Mode:
1. Enter Play mode
2. Click your button object
3. Parts cycle through highlight states

## Customization

### Change Colors (SimplePartHighlighter):
- **Highlight Color**: Default red (#FF0000)
- **Transparent Alpha**: Default 0.5 (50% opacity)

### Change Target (SimpleHighlightButton):
- **Target Object Name**: Change from "456" to any name
- **Search In Building Block**: Toggle to search globally

## Troubleshooting

### "No parts found"
- Make sure 456.fbx has separate child objects
- Each part needs its own MeshRenderer
- Check console for part detection logs

### Button not working
- Ensure object has a Collider
- Check EventSystem exists (for UI buttons)
- Verify 456 object is found (check console)

### Materials look wrong
- Original materials are cached on Start
- Use "Refresh Parts" context menu to reset
- Standard shader required for transparency

## Integration with Step System

Both systems can coexist:
- SimplePartHighlighter for manual 456 object
- PartHighlighter for step-based models
- Use different buttons for each system

## Console Commands for Testing

In Play mode, select components and use:
- SimplePartHighlighter → "Debug Info" 
- SimpleHighlightButton → "Debug State"
- Shows current highlight state and part names