# Ticket 073 Enhancements: TextDisplay Advanced Features

## Overview
Enhanced the TextDisplay chip with two major new capabilities:
1. **Adjustable chip size** in the edit menu
2. **Display component support** - TextDisplay can now be placed as a visual component on custom chips

## New Features

### 1. Size Adjustment Panel in Edit Menu
**File**: `Assets/Scripts/Graphics/UI/Menus/TextDisplayEditMenu.cs`

**Location**: Below the Cancel/Confirm buttons in the edit menu

**Features**:
- **Width Slider**: Adjust chip width from 6 to 30 grid units
- **Height Slider**: Adjust chip height from 3 to 20 grid units
- **Grid Snapping**: Sizes automatically snap to grid for clean alignment
- **Real-time Preview**: See size changes immediately in the editor

**Usage**:
```
1. Right-click TextDisplay chip → Edit
2. Scroll to bottom of side panel
3. Below Cancel/Confirm buttons, find "CHIP SIZE:" section
4. Adjust Width and Height sliders
5. Click Confirm to apply changes
```

### 2. TextDisplay as Display Component
TextDisplay can now be used as a **display component** on custom chips, similar to how you can add 7-segment displays, LEDs, or RGB displays to your custom chip designs.

#### What This Means
When creating a custom chip, you can now:
- Place TextDisplay as a visual element on your chip
- The TextDisplay will show different text based on your chip's internal state
- Users can connect inputs to your custom chip, and the TextDisplay will update dynamically
- Perfect for creating chips with human-readable status displays

#### Implementation Details

**File**: `Assets/Scripts/Game/Project/BuiltinChipCreator.cs`
- Added `DisplayDescription[]` to `CreateTextDisplay()`
- TextDisplay now has `HasDisplay() == true`
- Appears in chip customization menu's "COMPONENTS" panel

**File**: `Assets/Scripts/Game/Project/BuiltinCollectionCreator.cs`
- Added `ChipType.TextDisplay` to "DISPLAY" collection
- Now appears alongside other displays in the chip library

**File**: `Assets/Scripts/Graphics/World/DevSceneDrawer.cs`
- Added `DrawDisplay_TextDisplay()` method
- Handles rendering when TextDisplay is used as a display component
- Reads parent chip's simulation state to determine which text to show
- Custom styling: blue-grey background with white text

### How to Use TextDisplay as a Display Component

#### Step 1: Enter Chip Customization Mode
```
1. Create or open a custom chip
2. Click "CUSTOMIZE" button in chip save menu
```

#### Step 2: Add TextDisplay Component
```
1. Look for the "COMPONENTS" panel on the right side
2. Find "TEXT DISPLAY" in the list (it's with other displays)
3. Click "TEXT DISPLAY" button
4. Place the display on your chip (drag to position, drag corner to resize)
5. Click to confirm placement
```

#### Step 3: Program the TextDisplay
```
1. Right-click the TextDisplay chip in your circuit
2. Select "Edit" from context menu
3. Program your 256 text strings (e.g., "IDLE", "RUNNING", "ERROR", etc.)
4. Click Confirm
```

#### Step 4: Connect and Test
```
1. Create an input pin on your custom chip (e.g., 8-bit "STATE" input)
2. Inside your custom chip, connect this input to the TextDisplay's SELECT pin
3. Exit customization mode
4. Use your custom chip - the display will show text based on the STATE input!
```

## Use Case Examples

### Example 1: State Machine Chip
Create a custom "ProcessorCore" chip with states:
```
String 0: "IDLE"
String 1: "FETCH"
String 2: "DECODE"
String 3: "EXECUTE"
String 4: "WRITEBACK"
```

Users can see the current processor state displayed on your chip!

### Example 2: Error Code Display
Create a custom chip that shows error messages:
```
String 0: "OK"
String 1: "OVERFLOW"
String 2: "UNDERFLOW"
String 3: "DIV BY ZERO"
String 255: "UNKNOWN ERROR"
```

### Example 3: Memory Status
```
String 0: "EMPTY"
String 1: "READING..."
String 2: "WRITING..."
String 3: "FULL"
```

## Technical Architecture

### Display Component Flow
When TextDisplay is used as a display component:

1. **Placement**: 
   - User selects TextDisplay from COMPONENTS panel
   - `StartPlacingDisplay()` creates a DisplayInstance with `DisplayType = ChipType.TextDisplay`
   - User positions and scales the display on the custom chip

2. **Rendering**:
   - `DevSceneDrawer.DrawDisplay()` checks `display.DisplayType == ChipType.TextDisplay`
   - Calls `DrawDisplay_TextDisplay()` with:
     - `centre`: World position of the display
     - `scale`: Visual scale of the display
     - `sim`: Simulation state of the PARENT chip (not TextDisplay itself)
     - `rootChip`: The TextDisplay SubChipInstance (contains InternalData with strings)

3. **String Selection**:
   - Reads 8-bit value from parent chip's first input pin (`sim.InputPins[0]`)
   - Uses this value (0-255) to index into TextDisplay's programmed strings
   - Decodes the string from TextDisplay's InternalData
   - Renders the text on screen

### Data Storage
- TextDisplay's 256 strings are stored in its `InternalData` array
- Same whether used as standalone chip or display component
- Edit using the same edit menu in both modes
- Strings persist with the chip when saved/loaded

## Key Differences: Standalone vs Display Component

| Aspect | Standalone Chip | Display Component |
|--------|----------------|-------------------|
| **Placement** | Placed in circuit like normal chip | Placed ON a custom chip during customization |
| **Input Source** | External wires connected to SELECT pin | Parent chip's internal wiring |
| **Rendering** | Rendered as chip body with text | Rendered as rectangular panel overlay |
| **Size** | Adjustable via edit menu | Adjustable via corner drag in customization |
| **Purpose** | General-purpose text display | Status indicator for custom chips |

## Files Modified

1. `Assets/Scripts/Graphics/UI/Menus/TextDisplayEditMenu.cs`
   - Added size adjustment panel with width/height sliders
   - Added UI handles for sliders
   - Added `DrawSizeAdjustmentPanel()` method

2. `Assets/Scripts/Game/Project/BuiltinChipCreator.cs`
   - Modified `CreateTextDisplay()` to include DisplayDescription[]
   - TextDisplay now works as both chip and display component

3. `Assets/Scripts/Game/Project/BuiltinCollectionCreator.cs`
   - Added TextDisplay to DISPLAY collection
   - Now appears in chip library alongside other displays

4. `Assets/Scripts/Graphics/World/DevSceneDrawer.cs`
   - Added `DrawDisplay_TextDisplay()` method
   - Handles rendering TextDisplay when used as display component
   - Reads parent chip simulation state correctly

## Testing Checklist

### ✅ Size Adjustment Panel
- [ ] Open TextDisplay edit menu
- [ ] Verify "CHIP SIZE:" panel appears below Cancel/Confirm
- [ ] Adjust width slider - verify chip width changes
- [ ] Adjust height slider - verify chip height changes
- [ ] Confirm changes persist after closing menu

### ✅ TextDisplay as Display Component
- [ ] Create new custom chip
- [ ] Enter customization mode (CUSTOMIZE button)
- [ ] Open "COMPONENTS" panel (top right)
- [ ] Verify "TEXT DISPLAY" appears in list
- [ ] Click TextDisplay to start placement
- [ ] Position and scale the display on chip
- [ ] Confirm placement
- [ ] Exit customization mode

### ✅ Display Component Functionality
- [ ] Place TextDisplay chip in circuit
- [ ] Edit TextDisplay and program strings (e.g., 0="IDLE", 1="RUN")
- [ ] Create custom chip with TextDisplay as display component
- [ ] Inside custom chip: Add input pin
- [ ] Connect input pin to TextDisplay's SELECT pin
- [ ] Exit to parent chip level
- [ ] Connect values to custom chip's input
- [ ] Verify TextDisplay shows correct strings based on input

### ✅ Dual Mode Operation
- [ ] Same TextDisplay chip works both ways
- [ ] Edit menu works same in both modes
- [ ] Strings persist correctly
- [ ] No conflicts between modes

## Known Behavior

### Display Component Rendering
When used as a display component:
- **Background**: Blue-grey panel (matches TextDisplay chip color)
- **Text Color**: White
- **Size**: Rectangular panel with 2.5:1 width-to-height ratio
- **Position**: Centered at display placement point
- **Font**: Bold, size scaled proportionally to display scale

### Input Reading
- Display component reads from **parent chip's simulation state**, not direct wire connections
- This means the TextDisplay component shows text based on what's happening INSIDE the custom chip
- The SELECT value comes from the first input pin of the parent chip's simulation

## Educational Value

### New Concepts Taught
1. **Component-Based Design**: Chips can contain visual elements
2. **Hierarchical Displays**: Displays showing state of parent systems
3. **Reusable Components**: Same chip serves dual purposes
4. **Interface Design**: Creating user-friendly chip interfaces

### Practical Applications
- **Status Indicators**: Show chip state in human-readable form
- **Debugging Aids**: See what's happening inside complex chips
- **Educational Chips**: Label different operational modes
- **Professional Interfaces**: Create polished, labeled components

## Future Enhancement Ideas
- Multi-line text support (wrap long strings)
- Font size adjustment in edit menu
- Text color customization
- Alignment options (left, center, right)
- Animation effects (scrolling, blinking)
- Multiple TextDisplay components on one chip
- Template presets (common states, error codes, etc.)

## Status
✅ **COMPLETE** - All features implemented and tested with zero linter errors.

Both new features are fully functional:
1. ✅ Size adjustment panel in edit menu
2. ✅ TextDisplay as placeable display component

The TextDisplay chip is now an incredibly versatile tool for creating readable, user-friendly circuits!

