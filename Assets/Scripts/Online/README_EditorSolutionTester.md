# Editor Solution Tester

A Unity Editor tool for testing solution loading without deploying to mobile devices.

## Quick Setup

### Method 1: Automatic Setup
1. Create an empty GameObject in your scene
2. Add the `EditorSolutionTesterSetup` component to it
3. The tester will automatically be added and appear in the Scene view

### Method 2: Manual Setup
1. Create an empty GameObject in your scene
2. Add the `EditorSolutionTester` component to it
3. The tester GUI will appear in the Scene view

## Features

### 🔥 Firebase Testing
- **Test Load Solution from Firebase**: Loads a real solution from Firebase using a solution ID
- **Real Firebase Connection**: Uses the same Firebase setup as the mobile app
- **Live Data**: Tests with actual solutions from the leaderboard

### 🏠 Local Testing
- **Test Create Local Solution**: Creates a solution from the current project state
- **Serialization Testing**: Tests JSON serialization/deserialization
- **Project Integration**: Tests loading solutions into the current project

### 🎮 Level Mode Testing
- **Test Exit Level Mode**: Tests the level mode exit functionality
- **State Verification**: Checks level manager state before/after operations

## Usage

### 1. Firebase Solution Testing
1. Set the `testSolutionId` to a real solution ID from your Firebase database
2. Click "Test Load Solution from Firebase"
3. Watch the console for detailed logs
4. The solution will be loaded into your current project

### 2. Local Solution Testing
1. Make sure you have a project with some components
2. Click "Test Create Local Solution"
3. This tests the complete serialization/deserialization cycle

### 3. Level Mode Testing
1. Start a level in your project
2. Click "Test Exit Level Mode"
3. Verify that the level banner disappears and you can edit freely

## Configuration

### Test Solution ID
- Default: `gx6Aty5qinR7YsXhaynl` (from your recent logs)
- Change this to test different solutions
- Use the context menu "Set Test Solution ID from Logs" to update

### Test Level ID
- Default: `NOT Gate`
- Used for local solution creation
- Should match your test level

## Debugging

### Console Logs
The tester provides detailed console logging:
- `[EditorSolutionTester]` - General test operations
- `[SolutionSerializer]` - Solution loading operations
- `[Leaderboard]` - Firebase operations
- `[LevelBannerUI]` - Level state changes

### GUI Status
The tester GUI shows:
- Current test status
- Last test result
- Any errors that occurred
- Real-time feedback

## Context Menu Options

Right-click on the `EditorSolutionTester` component for:
- **Clear Test Results**: Clears the GUI status
- **Set Test Solution ID from Logs**: Updates the test solution ID

## Troubleshooting

### Firebase Connection Issues
- Make sure Firebase is properly configured
- Check that `google-services.json` is in the project
- Verify Firebase initialization in console logs

### Solution Loading Issues
- Check that the solution ID exists in Firebase
- Verify the solution has valid JSON data
- Check console logs for detailed error messages

### Level Mode Issues
- Make sure you're in a level when testing level mode exit
- Check that LevelManager is properly initialized
- Verify the level banner disappears after exit

## Integration with Development Workflow

1. **Before Mobile Testing**: Use this to verify solution loading works
2. **Debug Issues**: Test specific solutions without mobile deployment
3. **Validate Changes**: Test your code changes in the editor first
4. **Firebase Testing**: Test with real Firebase data without mobile builds

## Notes

- This tool only works in the Unity Editor
- It uses the same Firebase configuration as your mobile app
- All operations are logged to the console for debugging
- The GUI overlay only appears in the Scene view, not in Game view
