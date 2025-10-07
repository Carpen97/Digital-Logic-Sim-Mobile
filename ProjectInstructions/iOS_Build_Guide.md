# iOS Build Guide & Log

**Last Updated:** October 7, 2025

## Overview
This document tracks the iOS build process for Digital Logic Sim Mobile. Since development happens on Windows and iOS builds require macOS, this guide outlines the complete workflow.

---

## Build Workflow (Windows → Mac → App Store)

### Phase 1: Unity Build on Windows ✓
**Status:** Ready to start

1. **Switch Platform to iOS**
   - ✓ Platform switched (Firebase warning is expected and safe to ignore on Windows)
   - File > Build Settings > iOS > Switch Platform
   
2. **Configure Build Settings**
   - [ ] Verify Bundle Identifier matches App Store (com.yourcompany.digitallogicsim or similar)
   - [ ] Check Version number and Build number
   - [ ] Verify signing team is set (or will be set in Xcode)
   - [ ] Confirm architecture settings (ARM64 for modern devices)
   
3. **Build Xcode Project**
   - [ ] Build Settings > Build (creates Xcode project folder)
   - [ ] This creates a folder (likely named "DLS_IOS" or similar)
   - [ ] Build output location: Choose a clean folder

**Notes:**
- Firebase iOS warning on Windows is expected - Firebase will work correctly when built on Mac
- The Unity build creates an Xcode project, NOT a final app

---

### Phase 2: Transfer to Mac (MacInCloud)
**Status:** Pending

- [ ] Zip the Xcode project folder from Unity build
- [ ] Upload to MacInCloud (or transfer via cloud storage)
- [ ] Extract on Mac

**Transfer options:**
- Direct upload to MacInCloud
- Google Drive/Dropbox
- GitHub (if project size allows)

---

### Phase 3: Xcode Build & Archive (on Mac)
**Status:** Pending

1. **Open in Xcode**
   - [ ] Open .xcworkspace or .xcodeproj file
   - [ ] Wait for Xcode to index project
   
2. **Configure Signing & Capabilities**
   - [ ] Select your Apple Developer Team
   - [ ] Verify Bundle Identifier
   - [ ] Check provisioning profiles
   - [ ] Verify capabilities (Firebase, Push Notifications, etc.)
   
3. **Build & Test**
   - [ ] Select "Any iOS Device" or "Generic iOS Device" as target
   - [ ] Product > Build (⌘B) to verify no errors
   - [ ] Fix any Mac-specific build errors if they occur
   
4. **Archive for Distribution**
   - [ ] Product > Archive
   - [ ] Wait for archive to complete
   - [ ] Archive should appear in Organizer window

---

### Phase 4: Upload to App Store Connect
**Status:** Pending

1. **Distribute Archive**
   - [ ] Window > Organizer
   - [ ] Select the archive
   - [ ] Click "Distribute App"
   - [ ] Choose "App Store Connect"
   - [ ] Select distribution options (include bitcode, upload symbols)
   
2. **Wait for Processing**
   - [ ] Upload completes
   - [ ] Wait for Apple to process (10-60 minutes usually)
   - [ ] Check App Store Connect for build availability

---

### Phase 5: TestFlight & Testing
**Status:** Pending

1. **TestFlight Setup**
   - [ ] Build appears in App Store Connect > TestFlight
   - [ ] Add testers or test groups
   - [ ] Provide test information if required
   
2. **Install on iPad**
   - [ ] Download TestFlight app on iPad
   - [ ] Accept invite
   - [ ] Install build
   - [ ] Test functionality

---

### Phase 6: App Store Submission (Optional)
**Status:** Pending

- [ ] Create new version in App Store Connect
- [ ] Select the build
- [ ] Fill in "What's New" 
- [ ] Update screenshots if needed
- [ ] Submit for review

---

## Common Issues & Solutions

### Firebase Warning on Windows
**Issue:** "Firebase iOS builds are not supported on Windows"
- **Solution:** This is just a warning. The build will work fine. Firebase functionality is configured correctly when building on Mac.

### Missing GoogleService-Info.plist for iOS
**Issue:** iOS app needs Firebase configuration file (different from Android's google-services.json)
- **Solution:** 
  1. Go to [Firebase Console](https://console.firebase.google.com)
  2. Select your project
  3. Add iOS app or go to existing iOS app settings
  4. Download GoogleService-Info.plist
  5. Place in Assets/ folder in Unity project
  6. Unity will automatically copy it to the Xcode project during build

### NativeFilePicker Can't Select Files on iOS
**Issue:** File picker shows files but doesn't allow selection - files appear grayed out or can only be deleted/moved
- **Root Cause:** Android MIME types (like "application/zip") don't work on iOS - iOS requires UTI format
- **Solution:** Use platform-specific file type identifiers with #if UNITY_IOS:
  - For ZIP files on iOS: `"public.zip-archive"`, `"com.pkware.zip-archive"`
  - For JSON files on iOS: `"public.json"`, `"public.plain-text"`
  - Keep Android MIME types for Android builds
- **Reference:** See fixes in `Main.cs` (ImportProject) and `AndroidChipIO.cs` (ImportChip)

### Code Signing Errors
**Issue:** "X 'Unity-iPhone' requires a provisioning profile" (even with profile selected)
- **Current Status:** You have `DLS_DAVID` provisioning profile selected but Xcode still complains
- **Solution Steps:**
  1. **Try Automatic Signing First:**
     - Check "Automatically manage signing" 
     - Let Xcode create/update profiles automatically
     - This often fixes profile mismatches
     
  2. **If Automatic Fails - Manual Profile Refresh:**
     - Uncheck "Automatically manage signing"
     - Click the dropdown next to "Provisioning Profile"
     - Try selecting "Xcode Managed Profile" if available
     - Or try "None" then select your team again
     
  3. **Profile Download:**
     - Xcode > Preferences > Accounts
     - Select your Apple ID
     - Click "Download Manual Profiles"
     - Wait for it to complete
     
  4. **Bundle ID Mismatch (Most Common):**
     - Your Bundle ID: `com.DavidCarpenfelt.Digital-Logic-Sim-Mobile`
     - Try changing to: `com.DavidCarpenfelt.Digital-Logic-Sim-Mobile.v2`
     - Or: `com.DavidCarpenfelt.DLS-Mobile`
     - The `DLS_DAVID` profile might be for a different Bundle ID
     
  5. **Clean & Reset:**
     - Product > Clean Build Folder (⌘⇧K)
     - Close Xcode
     - Reopen project
     
  6. **Nuclear Option:**
     - Delete derived data: `rm -rf ~/Library/Developer/Xcode/DerivedData`
     - Restart Xcode

### Firebase Linking Errors
**Issue:** 100+ "Undefined symbol" errors for Firebase classes (FIRApp, FIRAuth, Firestore, etc.)
- **Root Cause:** Firebase SDK libraries not properly linked to Xcode project
- **Solution Steps:**
  1. **Add Firebase Frameworks to Xcode:**
     - In Xcode, right-click on project root
     - "Add Files to 'Unity-iPhone'"
     - Navigate to: `Assets/Firebase/Plugins/iOS/`
     - Select all `.framework` files (FirebaseAuth.framework, FirebaseFirestore.framework, etc.)
     - Make sure "Add to target: Unity-iPhone" is checked
     
  2. **Configure Framework Search Paths:**
     - Build Settings > Framework Search Paths
     - Add: `$(SRCROOT)/Firebase/Plugins/iOS`
     
  3. **Add Required Libraries:**
     - Build Settings > Other Linker Flags
     - Add: `-ObjC` and `-lc++`
     
  4. **Verify GoogleService-Info.plist:**
     - Ensure GoogleService-Info.plist is in project root
     - Check it's added to "Unity-iPhone" target
     
  5. **Clean and Rebuild:**
     - Product > Clean Build Folder
     - Build again

### Build Number Must Increment
**Issue:** "This build number has already been uploaded"
- **Solution:** Increment the build number in Unity (Player Settings) before building

### Missing Capabilities
**Issue:** Firebase or other services not working
- **Solution:** Verify Capabilities tab in Xcode has all required permissions

---

## Build Checklist

### Pre-Build (Unity/Windows)
- [ ] All code changes committed/backed up
- [ ] Version number updated
- [ ] Build number incremented
- [ ] Target iOS version set appropriately
- [ ] Architecture set to ARM64

### Mac Build
- [ ] MacInCloud rental activated
- [ ] Xcode is latest stable version
- [ ] Apple Developer certificates are valid
- [ ] Provisioning profiles are up to date

### Post-Build
- [ ] Build tested on actual device (iPad)
- [ ] All Firebase features working
- [ ] No crashes or major issues
- [ ] Performance is acceptable

---

## Current Build Session Log

### Session: October 7, 2025

**Time:** Starting  
**Goal:** Complete iOS build and deploy to TestFlight

#### Actions Taken:
1. ✓ Switched Unity platform to iOS (Firebase warning noted - safe to ignore)
2. ✓ Created this guide document
3. ✓ **FIXED iOS Import Bug:** Updated NativeFilePicker calls to use iOS UTI instead of Android MIME types
   - Modified `Main.cs::ImportProject()` - now uses "public.zip-archive" on iOS
   - Modified `AndroidChipIO.cs::ImportChip()` - now uses "public.json" on iOS
   - Used conditional compilation (#if UNITY_IOS) to maintain Android compatibility

#### Next Steps:
1. ⚠️ **CRITICAL:** Add GoogleService-Info.plist for iOS Firebase
2. ✓ **FIXED:** NativeFilePicker file type issue for importing on iOS
3. ⚠️ **CURRENT ISSUE:** Xcode signing error - "requires a development team"
4. Configure Unity iOS build settings (version/build numbers)
5. Build Xcode project from Unity
6. Transfer to MacInCloud
7. Build in Xcode
8. Upload to App Store Connect
9. Test on iPad via TestFlight

#### Issues Encountered:
- **Missing iOS Firebase Config:** No GoogleService-Info.plist found in project
  - Need to download from Firebase Console for iOS app
  - Should be placed in Assets/ folder (Unity will copy to Xcode project)

- **NativeFilePicker Import Not Working on iOS:** ✓ FIXED
  - Problem: Code was using Android MIME types (e.g., "application/zip") instead of iOS UTI
  - iOS requires UTI format like "public.zip-archive" for zip files
  - Fixed in Main.cs ImportProject() and AndroidChipIO.cs ImportChip()
  - Used platform-specific file type identifiers

- **Firebase Linking Errors:** ⚠️ **CURRENT ISSUE**
  - Problem: 100+ "Undefined symbol" errors for Firebase classes and functions
  - Root Cause: Firebase libraries not properly linked to Xcode project
  - Affects: FIRApp, FIRAuth, Firestore, and all Firebase components
  - Solution: Add Firebase frameworks to Xcode project and configure linking

#### Notes:
- DLS_IOS folder exists from previous build (version 1.0.1, build 1)
- Current version in that folder: 1.0.1 (build 1) - will need to increment before new submission
- Bundle ID from old build: ${PRODUCT_BUNDLE_IDENTIFIER} (set in Xcode)
- Orientation: Landscape Right only

---

## Quick Reference

### Important Files/Folders
- **Unity Build Output:** DLS_IOS/ (or custom location)
- **Bundle ID:** (Check in Unity Player Settings)
- **Firebase Config:** Assets/google-services.json (for Android), GoogleService-Info.plist should be in project for iOS

### Key Unity Settings Location
- Edit > Project Settings > Player > iOS tab
- Bundle Identifier
- Version/Build numbers  
- Architecture
- Target SDK

### MacInCloud Info
- Service: (Add your MacInCloud details)
- Connection method: (Remote desktop, SSH, etc.)

---

## Resources
- [Unity iOS Build Documentation](https://docs.unity3d.com/Manual/ios-BuildProcess.html)
- [Xcode Archive & Upload Guide](https://developer.apple.com/documentation/xcode/distributing-your-app-for-beta-testing-and-releases)
- App Store Connect: https://appstoreconnect.apple.com


