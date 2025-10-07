# iOS Deployment Guide - Ticket 043
## Digital Logic Sim Mobile - iPad Testing

---

## 📋 Overview

This guide walks you through deploying the Digital Logic Sim Mobile application to your iPad for comprehensive testing. The project has an existing iOS build in the `DLS_IOS` folder, which should streamline the process.

---

## ⚙️ Prerequisites

### Required Software
- **macOS** with latest available OS version
- **Xcode** 14.0 or later (latest stable recommended)
- **Unity** (version matching the project - check Unity version in ProjectSettings)
- **CocoaPods** for Firebase dependency management
- **Apple Developer Account** (free or paid)

### Required Hardware
- iPad (compatible with iOS 13.0 or later based on project settings)
- USB cable for device connection
- Mac computer for Xcode

---

## 🔨 Part 1: Unity Build Configuration

### 1.1 Open Project in Unity
1. Open Unity Hub
2. Add the project: `Digital-Logic-Sim` directory
3. Select the correct Unity version
4. Open the project

### 1.2 Switch Platform to iOS
1. Go to **File → Build Settings**
2. Select **iOS** from the platform list
3. Click **Switch Platform** (if not already on iOS)
4. Wait for Unity to reimport assets for iOS

### 1.3 Verify iOS Build Settings
1. In Build Settings window, verify:
   - **Target SDK**: iOS 13.0 or later
   - **Architecture**: ARM64
   - **Run in Xcode**: Check this option

2. Click **Player Settings** to verify:
   - **Company Name**: `DavidCarpenfelt`
   - **Product Name**: `Digital-Logic-Sim-Mobile`
   - **Bundle Identifier**: `com.DavidCarpenfelt.Digital-Logic-Sim-Mobile`
   - **Version**: `1.0.2` (as configured)
   - **Build Number**: `2`

### 1.4 iOS-Specific Settings
In Player Settings → iOS tab:
- **Minimum iOS Version**: 13.0
- **Target Device**: iPad (or iPad + iPhone)
- **Requires ARKit**: Disabled
- **Status Bar**: Hidden
- **Allowed Orientations**: Landscape Right
- **Architecture**: ARM64
- **Scripting Backend**: IL2CPP
- **Target SDK**: Device SDK

### 1.5 Firebase Configuration Check
**IMPORTANT**: The project currently uses `google-services.json` for Android. For iOS to work with Firebase:

1. **Option A - iOS Firebase App exists:**
   - Download `GoogleService-Info.plist` from Firebase Console
   - Project Settings → Your Apps → iOS app
   - Place file in `Assets/` directory
   - Unity will copy it to the Xcode project automatically

2. **Option B - No iOS Firebase App:**
   - Firebase will fall back to anonymous mode
   - Features will work in limited capacity
   - Firebase logging and leaderboards may not sync to cloud
   - The app handles this gracefully (see `FirebaseBootstrap.cs`)

### 1.6 Build for iOS
1. In Build Settings, click **Build**
2. Choose output location (or use existing `DLS_IOS` folder to update)
3. Wait for build to complete (this may take 10-30 minutes)
4. Unity will generate Xcode project files

---

## 🍎 Part 2: Xcode Configuration

### 2.1 Install CocoaPods Dependencies
1. Open Terminal
2. Navigate to the Xcode project directory:
   ```bash
   cd "path/to/Digital-Logic-Sim/DLS_IOS"
   ```
3. Install CocoaPods (if not already installed):
   ```bash
   sudo gem install cocoapods
   ```
4. Install Firebase dependencies:
   ```bash
   pod install
   ```
5. Wait for Firebase pods to download and install

### 2.2 Open Xcode Project
1. In the `DLS_IOS` folder, open **Unity-iPhone.xcworkspace** (NOT .xcodeproj)
2. If prompted, let Xcode update project settings

### 2.3 Configure Signing & Capabilities

#### Code Signing
1. Select the **Unity-iPhone** project in Project Navigator
2. Select the **Unity-iPhone** target
3. Go to **Signing & Capabilities** tab
4. Check **Automatically manage signing**
5. Select your **Team** from dropdown (your Apple Developer account)
6. Xcode will automatically create provisioning profiles

#### Bundle Identifier
- Verify Bundle Identifier: `com.DavidCarpenfelt.Digital-Logic-Sim-Mobile`
- If there's a conflict, you can temporarily change it for testing

#### Capabilities
The project requires these capabilities (should already be configured):
- **File Access** (implicit via NativeFilePicker)
- **iCloud** (only if using cloud storage - can be disabled for testing)

### 2.4 Build Settings Review
1. Select **Unity-iPhone** target
2. Go to **Build Settings** tab
3. Verify:
   - **iOS Deployment Target**: 13.0
   - **Architectures**: arm64
   - **Build Active Architecture Only**: Yes (for Debug)
   - **Enable Bitcode**: No
   - **Valid Architectures**: arm64

### 2.5 Fix Common Build Issues

#### Issue: "GoogleService-Info.plist not found"
- **Solution**: If you don't have iOS Firebase app, this is expected
- App will run but Firebase features use local fallback

#### Issue: CocoaPods not found
- **Solution**: Run `pod install` in terminal from DLS_IOS directory

#### Issue: Signing Certificate errors
- **Solution**: In Xcode Preferences → Accounts → Download Manual Profiles
- Or create new certificates in Apple Developer portal

---

## 📱 Part 3: Deploy to iPad

### 3.1 Connect iPad
1. Connect iPad to Mac via USB cable
2. **On iPad**: If prompted, tap **Trust This Computer**
3. Enter iPad passcode
4. In Xcode, select your iPad from device dropdown (top toolbar)

### 3.2 First-Time Device Setup
If this is your first time deploying to this iPad:
1. Go to Xcode → **Preferences → Accounts**
2. Verify your Apple ID is added
3. Select your account → View Details
4. Download certificates if needed

### 3.3 Build and Run
1. Select your iPad as destination device
2. Click **Build and Run** button (Play icon) or press **⌘R**
3. Wait for build (first build may take 15-30 minutes)
4. Xcode will install app on iPad automatically

### 3.4 Trust Developer on iPad
**IMPORTANT**: First time running apps from a developer on iPad:
1. On iPad, go to **Settings → General → VPN & Device Management**
2. Find your developer profile
3. Tap **Trust "[Developer Name]"**
4. Confirm trust
5. Return to home screen and launch the app

### 3.5 Monitor Build
- Watch Xcode's build output for errors
- If build fails, check error messages in Issue Navigator
- Common first-time issues are usually signing-related

---

## 🧪 Part 4: Testing Checklist

See companion document: **iOS_Testing_Checklist.md**

---

## 🔍 Troubleshooting

### Build Fails in Unity
**Error**: "iOS build support not installed"
- **Solution**: Install iOS Build Support module in Unity Hub

**Error**: "Unable to find Xcode"
- **Solution**: Install Xcode from Mac App Store, run once to accept license

### Xcode Build Fails
**Error**: "No Provisioning Profile found"
- **Solution**: Enable "Automatically manage signing" in Signing & Capabilities

**Error**: "Command CodeSign failed"
- **Solution**: 
  1. Clean Build Folder (Product → Clean Build Folder)
  2. Delete derived data
  3. Rebuild

**Error**: Pod installation failed
- **Solution**: 
  ```bash
  pod repo update
  pod install --repo-update
  ```

### App Crashes on Launch
**Check Console**: In Xcode, Window → Devices and Simulators → Select iPad → View Console
- Look for crash logs
- Check for missing resources or configuration

**Firebase Errors**:
- Expected if no `GoogleService-Info.plist`
- App should handle gracefully and continue
- Check debug logs: Firebase will log fallback to anonymous mode

### File Picker Not Working
**Permissions**: 
- iOS automatically handles document picker permissions
- No Info.plist entries needed for basic file access

**Ticket 042 Validation**:
- Ensure MIME types are used: `application/zip`, `application/octet-stream`
- Not iOS UTI types like `public.zip-archive`

### Performance Issues
- First launch is slower (caching)
- Monitor CPU/Memory in Xcode's Debug Navigator
- Enable Metal Frame Capture if graphics issues

---

## 📊 Key Configuration Files

### Unity Project Settings
- `ProjectSettings/ProjectSettings.asset` - Build settings, bundle ID, version
- `Assets/google-services.json` - Android Firebase config (exists)
- `Assets/GoogleService-Info.plist` - iOS Firebase config (MISSING - optional)

### Xcode Project
- `DLS_IOS/Info.plist` - App configuration
- `DLS_IOS/Podfile` - Firebase dependency versions
- `DLS_IOS/Unity-iPhone.xcodeproj/project.pbxproj` - Project settings

### Current Configuration
- **Bundle ID**: `com.DavidCarpenfelt.Digital-Logic-Sim-Mobile`
- **Version**: 1.0.2
- **Build**: 2
- **Min iOS**: 13.0
- **Platform**: iOS 15.0+ (from Podfile)
- **Orientation**: Landscape Right
- **Architecture**: ARM64
- **Scripting Backend**: IL2CPP

---

## 🎯 Critical Testing Points for Ticket 043

### Must Test
1. ✅ **Ticket 042 Fix**: Zip file selection for project import
2. ✅ **NativeFilePicker**: JSON chip import
3. ✅ **Firebase**: Score upload, leaderboards, user names
4. ✅ **Level System**: All levels, validation, progress saving
5. ✅ **Collections**: Subfolder navigation (Ticket 036)
6. ✅ **UI**: iPad screen scaling, touch interactions

### Known Considerations
- **Firebase**: May use fallback mode without iOS plist (graceful degradation)
- **First Launch**: Slower due to caching and initialization
- **File Access**: iOS sandbox - files only accessible via document picker
- **Performance**: Monitor first few minutes for stability

---

## 📝 Success Indicators

### Build Success
- ✅ Unity builds without errors
- ✅ Xcode compiles successfully
- ✅ App installs on iPad
- ✅ App launches without crash

### Runtime Success
- ✅ UI displays correctly on iPad screen
- ✅ Touch interactions responsive
- ✅ All menus navigable
- ✅ Levels load and play
- ✅ File import/export works

### Feature Validation
- ✅ Zip files selectable (Ticket 042)
- ✅ Firebase connects (or graceful fallback)
- ✅ Leaderboards accessible
- ✅ Level progression saves
- ✅ No critical bugs or crashes

---

## 🚀 Next Steps After Successful Deploy

1. **Systematic Testing**: Follow iOS_Testing_Checklist.md
2. **Document Findings**: Note any iOS-specific behaviors
3. **Firebase Setup**: If needed, create iOS app in Firebase Console
4. **Performance Profiling**: Use Xcode Instruments for optimization
5. **Prepare for TestFlight**: If deploying to testers

---

## 📞 Resources

### Apple Developer
- [Xcode Documentation](https://developer.apple.com/documentation/xcode)
- [Code Signing Guide](https://developer.apple.com/support/code-signing/)
- [TestFlight](https://developer.apple.com/testflight/)

### Firebase
- [Firebase iOS Setup](https://firebase.google.com/docs/ios/setup)
- [Firebase Console](https://console.firebase.google.com/)

### Unity
- [iOS Build Documentation](https://docs.unity3d.com/Manual/ios-BuildProcess.html)
- [iOS Player Settings](https://docs.unity3d.com/Manual/class-PlayerSettingsiOS.html)

---

**Good luck with your iOS deployment! 🎉**

*Remember: First builds always take longer. Be patient with Xcode.*

