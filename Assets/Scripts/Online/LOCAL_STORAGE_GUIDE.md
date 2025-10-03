# 🏠 Local Storage Testing Guide

## Overview
The local storage system simulates Firebase behavior in the Unity Editor, allowing you to test the complete solution sharing workflow without Firebase dependencies.

## 🚀 Quick Start

### 1. Setup Editor Solution Tester
1. **Add the component**: Add `EditorSolutionTester` to any GameObject in your scene
2. **Enable GUI**: Make sure "Show GUI" is checked in the Inspector
3. **Run the test**: Click "🔄 Test Local Storage Workflow" button

### 2. Test the Complete Workflow

#### Step 1: Create and Save a Solution
1. **Build a circuit** in your project
2. **Upload score** with "Share Solution" checked
3. **Solution gets saved** to local storage automatically

#### Step 2: View Solutions in Leaderboard
1. **Open leaderboard** for any level
2. **Click "View"** on any score entry
3. **Solution loads** from local storage and applies to your project

## 🔧 How It Works

### Local Storage Location
- **Path**: `Application.persistentDataPath/EditorLocalStorage/`
- **Files**: `scores.json`, `solutions.json`
- **Format**: JSON files that persist between Unity sessions

### Simulated Firebase Operations
- **Save Score**: `EditorLocalStorage.SaveScore()` → saves to local JSON
- **Save Solution**: `EditorLocalStorage.SaveCompleteSolution()` → saves to local JSON  
- **Get Scores**: `EditorLocalStorage.GetTopScores()` → loads from local JSON
- **Get Solution**: `EditorLocalStorage.GetCompleteSolution()` → loads from local JSON

## 🧪 Testing Features

### Editor Solution Tester GUI
- **🔥 Firebase**: Test loading from local storage (simulates Firebase)
- **🏠 Local**: Test creating local solutions
- **🎮 Level**: Test exiting level mode
- **🔄 Workflow**: Test complete save/load workflow

### Test Results
- **✅ Success**: Green status messages
- **❌ Errors**: Red error messages  
- **⏳ Running**: Yellow progress indicators

## 📊 What Gets Stored

### Score Data
```json
{
  "id": "unique-score-id",
  "levelId": "NOT Gate",
  "score": 42,
  "userName": "TestUser",
  "completeSolutionId": "solution-id-reference",
  "submittedAt": "2025-01-02T00:00:00.000Z",
  "userId": "EditorTestUser"
}
```

### Solution Data
```json
{
  "LevelId": "NOT Gate",
  "UserName": "TestUser", 
  "Score": 42,
  "CustomChips": [...],
  "MainSolution": {...}
}
```

## 🎯 Benefits

### ✅ Fast Testing
- **No Firebase setup** required
- **No network delays** 
- **Instant save/load** operations
- **Persistent data** between sessions

### ✅ Complete Workflow
- **Upload solutions** → saves locally
- **View leaderboard** → loads from local storage
- **Load solutions** → applies to project
- **Test all features** without Firebase

### ✅ Debug Friendly
- **Clear logging** for all operations
- **Error handling** with detailed messages
- **GUI feedback** for test results
- **Easy troubleshooting**

## 🔄 Workflow Example

1. **Create circuit** in Unity
2. **Upload score** with solution sharing
3. **Check leaderboard** - see your score
4. **Click "View"** - solution loads and applies
5. **Modify circuit** and repeat

## 🛠️ Troubleshooting

### Common Issues
- **"No active project"**: Make sure you have a project loaded
- **"Solution not found"**: Run the workflow test first to create test data
- **"GUI not showing"**: Check "Show GUI" in Inspector

### Debug Steps
1. **Check Console** for detailed logs
2. **Run workflow test** to create sample data
3. **Verify local storage** files exist
4. **Check Inspector** settings

## 📁 File Locations

### Unity Editor
- **Storage**: `%APPDATA%/../LocalLow/DefaultCompany/Digital-Logic-Sim/EditorLocalStorage/`
- **Logs**: Unity Console window

### Test Data
- **Scores**: `scores.json` in storage folder
- **Solutions**: `solutions.json` in storage folder

## 🎉 Success Indicators

### Upload Success
```
[Leaderboard] Score saved to local storage for level NOT Gate with score 42
[Leaderboard] Complete solution saved to local storage with ID: abc123
```

### View Success  
```
[SolutionSerializer] Loaded solution from local storage: abc123
[SolutionSerializer] Solution applied to project successfully
```

This system lets you test the complete solution sharing workflow without any Firebase setup! 🚀
