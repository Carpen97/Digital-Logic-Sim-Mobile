# Ticket 053: Version 2.1.6.10 Build Checklist

**Release Date:** October 12, 2025  
**Status:** Documentation Phase Complete ✅

---

## ✅ COMPLETED: Phase 1 - Documentation Updates

- ✅ PatchNotes.md updated to October 12, 2025
- ✅ patchNotes.json updated to October 12, 2025  
- ✅ Main.cs DLSVersion updated to 2.1.6.10
- ✅ Main.cs LastUpdatedString updated to "12 Oct 2025"
- ✅ ProjectSettings.asset bundleVersion updated to 2.1.6.10
- ✅ AndroidBundleVersionCode updated to 19
- ✅ iOS buildNumber updated to 19
- ✅ Changes committed and pushed to GitHub

---

## 🎯 PHASE 2: BUILD PROCESS

### 2.1 PC/Windows Build

**Instructions:**
1. Open Unity Editor (project should already be open)
2. Go to `File → Build Settings`
3. Switch platform to `PC, Mac & Linux Standalone`
4. Target Platform: **Windows**
5. Architecture: **x86_64**
6. Build location: `DLS_PC/` (folder exists)
7. Click **Build** and wait for completion

**Expected Output:**
- `DLS_PC/Digital-Logic-Sim-Mobile.exe`
- `DLS_PC/Digital-Logic-Sim-Mobile_Data/` folder
- Supporting DLL files

**Build Time:** ~10-15 minutes

---

### 2.2 Android Build (AAB for Google Play)

**Instructions:**
1. Go to `File → Build Settings`
2. Switch platform to **Android**
3. Build System: **Gradle (Unity 2021+)**
4. **IMPORTANT:** Check "Build App Bundle (Google Play)"
5. Build location: Choose `Digital-Logic-Sim-Mobile.aab` (overwrite existing)
6. Click **Build** and wait for completion

**Optional - APK for Direct Testing:**
1. Uncheck "Build App Bundle"
2. Build location: `Digital-Logic-Sim-Mobile.apk`
3. Click **Build**

**Expected Output:**
- `Digital-Logic-Sim-Mobile.aab` (for Google Play submission)
- `Digital-Logic-Sim-Mobile.apk` (optional, for direct testing)

**Build Time:** ~15-20 minutes (AAB) + ~10 minutes (APK if building)

**Notes:**
- AAB is required for Google Play Store
- APK is useful for quick testing on device
- Version code 19 will be embedded

---

### 2.3 iOS Build (Xcode Project)

**Instructions - Unity Side:**
1. Go to `File → Build Settings`
2. Switch platform to **iOS**
3. Build location: `DLS_IOS/` (folder exists, will be updated)
4. Click **Build** and wait for Unity to generate Xcode project

**Instructions - Xcode Side:**
1. Navigate to `DLS_IOS/` folder
2. Open `Unity-iPhone.xcodeproj` in Xcode
3. Verify **General → Identity**:
   - Display Name: Digital Logic Sim Mobile
   - Bundle Identifier: com.DavidCarpenfelt.Digital-Logic-Sim-Mobile
   - Version: 2.1.6.10
   - Build: 19
4. Select your development team
5. Connect iPad/iPhone for testing OR select "Any iOS Device (arm64)"
6. Go to `Product → Archive`
7. Wait for archive to complete
8. In Organizer, select the archive and click **Distribute App**
9. Choose distribution method (App Store Connect / Ad Hoc for testing)

**Expected Output:**
- Updated `DLS_IOS/` Xcode project
- `.xcarchive` in Xcode Organizer
- `.ipa` file for distribution

**Build Time:** 
- Unity to Xcode: ~10-15 minutes
- Xcode Archive: ~20-30 minutes

---

## 🧪 PHASE 3: TESTING

### 3.1 PC Testing Checklist

**Launch Test:**
- [ ] Application launches without errors
- [ ] Main menu loads correctly

**Version Verification:**
- [ ] Open About menu
- [ ] Verify version displays: **2.1.6.10 (12 Oct 2025)**

**Core Functionality:**
- [ ] Create new project
- [ ] Open Levels menu
- [ ] Play a level (e.g., NOT Gate)
- [ ] Test validation (build simple solution)
- [ ] Play D Latch level (new in this version)
- [ ] Verify level completes successfully

**Firebase Integration:**
- [ ] Complete a level
- [ ] Upload score to leaderboard
- [ ] View leaderboard entries
- [ ] Test solution sharing (view/download)
- [ ] Set/change user name

**Patch Notes Popup:**
- [ ] Open About menu → What's New
- [ ] Verify version 2.1.6.10 appears at top
- [ ] Check date shows **2025-10-12**
- [ ] Verify all sections display correctly (New Features, Improvements, Bug Fixes)
- [ ] Test version selector dropdown
- [ ] Test scrolling through content

**Additional Tests:**
- [ ] Test "Coming Soon" chapter appears in Levels menu
- [ ] Test scrollable info panel in level validation popup
- [ ] Test Chip Library navigation (folders, collections)
- [ ] Import/export project (zip file)
- [ ] Test preferences menu

**Performance:**
- [ ] No crashes during 10-minute play session
- [ ] No memory leaks
- [ ] Smooth navigation

---

### 3.2 Android Testing Checklist

**Installation:**
- [ ] Install APK on Android device: `adb install Digital-Logic-Sim-Mobile.apk`
- [ ] OR install AAB via Google Play Internal Testing

**Version Verification:**
- [ ] Open About menu
- [ ] Verify version: **2.1.6.10 (12 Oct 2025)**

**Core Functionality:**
- [ ] Touch controls responsive
- [ ] Create new project
- [ ] Play levels (including D Latch)
- [ ] Test validation

**Mobile-Specific Features:**
- [ ] Touch gestures (pinch to zoom, pan)
- [ ] Chip library navigation with touch
- [ ] Drag and drop chips
- [ ] Level selection UI
- [ ] Scrollable info panel in validation popup

**Firebase Integration:**
- [ ] Upload scores
- [ ] View leaderboards
- [ ] Share solutions
- [ ] Set user name

**File Operations:**
- [ ] Import project (zip file from Downloads)
- [ ] Export project (verify zip created in Downloads)
- [ ] Verify zip file selector works correctly

**Patch Notes Popup:**
- [ ] Open About → What's New
- [ ] Verify content displays correctly on mobile screen
- [ ] Test scrolling
- [ ] Test version selector

**Performance:**
- [ ] No crashes during 10-minute session
- [ ] Smooth 60 FPS (check with Android Profiler)
- [ ] Battery consumption reasonable

---

### 3.3 iOS Testing Checklist

**Installation:**
- [ ] Install via TestFlight OR
- [ ] Install via Xcode (Development build)

**Version Verification:**
- [ ] Open About menu
- [ ] Verify version: **2.1.6.10 (12 Oct 2025)**

**Core Functionality:**
- [ ] Touch controls on iPad
- [ ] Touch controls on iPhone
- [ ] Create project
- [ ] Play levels
- [ ] Test validation

**iOS-Specific Features:**
- [ ] File picker for import (critical fix in this version)
- [ ] Zip file selection works
- [ ] Project import successful
- [ ] Project export to Files app

**Firebase Integration:**
- [ ] Upload scores
- [ ] View leaderboards
- [ ] Share solutions

**Patch Notes Popup:**
- [ ] Open About → What's New
- [ ] Verify displays correctly on iOS
- [ ] Test interactions

**Performance:**
- [ ] Smooth performance on iPad
- [ ] Smooth performance on iPhone
- [ ] No crashes

---

## 📦 PHASE 4: STORE LISTINGS & DISTRIBUTION

### 4.1 Google Play Store Listing

**What's New (Release Notes for Google Play):**

```
🔧 Critical Bug Fixes:
• Fixed game-breaking level validation bug affecting all 26 levels
• Fixed D Flip-Flop level output mismatch
• Corrected test vectors for 4-bit Subtractor, Simple ALU, and Complete ALU

🆕 New Features:
• New D Latch educational level
• "Coming Soon" chapter with community feedback
• Test Vector Generator for developers
• Enhanced validation with random test sampling

⚡ Improvements:
• 25x faster validation for complex levels
• Improved Chip Library navigation
• Scrollable sequential test details
• Better level selection behavior

Version 2.1.6.10 | Oct 12, 2025
```

**Steps:**
1. Go to [Google Play Console](https://play.google.com/console)
2. Select **Digital Logic Sim Mobile** app
3. Navigate to **Production → Create new release**
4. Upload `Digital-Logic-Sim-Mobile.aab`
5. Release name: `2.1.6.10 (19)` 
6. Paste release notes above
7. Save and review
8. **Submit for review**

**Timeline:** 1-3 days for review

---

### 4.2 Apple App Store Listing

**What's New (Release Notes for App Store):**

```
🔧 Critical Bug Fixes
• Fixed game-breaking level validation bug affecting all 26 levels
• Fixed D Flip-Flop level that was previously unplayable
• Corrected test vectors for multiple ALU levels

🆕 New Features  
• New D Latch educational level fills gap in sequential logic
• "Coming Soon" chapter teases future content
• Enhanced test coverage for comprehensive validation

⚡ Improvements
• 25x faster validation for complex levels
• Improved Chip Library navigation with better focus management
• Scrollable test details panel for long sequences
• Enhanced level selection behavior

Version 2.1.6.10 includes critical fixes and performance improvements for the best logic design experience!
```

**Steps:**
1. Go to [App Store Connect](https://appstoreconnect.apple.com)
2. Select **Digital Logic Sim Mobile**
3. Click **+ Version** or **Update Submission**
4. Version number: **2.1.6.10**
5. Upload build from Xcode Organizer (Build 19)
6. What's New: Paste release notes above
7. Save
8. **Submit for review**

**Timeline:** 1-3 days for review

---

### 4.3 PC Distribution (itch.io or Direct)

**Option A: itch.io**

**Description for itch.io:**
```
Digital Logic Sim Mobile - Version 2.1.6.10

Critical bug fixes and new features!

This release fixes a game-breaking validation bug that affected all levels, adds a new D Latch educational level, and includes major performance improvements (25x faster validation).

See full patch notes in-app (About → What's New)

System Requirements:
- Windows 10/11 64-bit
- 2GB RAM
- 500MB storage
```

**Steps:**
1. Zip the `DLS_PC/` folder as `DLS-PC-2.1.6.10.zip`
2. Go to your itch.io game page
3. Click **Upload files**
4. Upload zip file
5. Mark as **Windows**
6. Version: **2.1.6.10**
7. Update game description with version info
8. Click **Save & view page**

**Option B: Direct Distribution**
1. Zip `DLS_PC/` folder
2. Upload to your hosting/distribution platform
3. Update download links
4. Notify users via Discord/social media

---

## ✅ PHASE 5: POST-RELEASE TASKS

### 5.1 Documentation

- [ ] Update README.md with latest version (if needed)
- [ ] Verify all documentation is current

### 5.2 Git Release Tag

**Create annotated release tag:**

```bash
git tag -a v2.1.6.10 -m "Release Version 2.1.6.10 - Critical bug fixes and new D Latch level"
git push origin v2.1.6.10
```

### 5.3 GitHub Release Page

1. Go to GitHub repository: https://github.com/Carpen97/Digital-Logic-Sim-Mobile
2. Click **Releases → Draft a new release**
3. Tag: **v2.1.6.10**
4. Release title: **Version 2.1.6.10 - Critical Bug Fixes & New Features**
5. Description:

```markdown
## Version 2.1.6.10 - October 12, 2025

### 🔧 Critical Bug Fixes
- **Fixed bit order bug in level validation** - Resolved game-breaking bug where inputs/outputs were being reversed during validation, affecting all 26 levels
- Fixed D Flip-Flop level output count mismatch
- Fixed 4-bit Subtractor test vectors
- Fixed Simple ALU test vectors
- Enhanced Complete 4-bit ALU test coverage

### 🆕 New Features
- **New D Latch level** - Fills gap in sequential logic education
- **"Coming Soon" chapter** - Placeholder for future content with community feedback
- Test Vector Generator (Developer Tool) - Press 'G' to generate test vectors
- Random Test Sampling for performance optimization
- Setup Phase Support for sequential circuits

### ⚡ Improvements
- 25x faster validation for complex levels
- Improved Chip Library navigation
- Scrollable sequential test details panel
- Better level selection behavior

### 📦 Downloads
- **PC (Windows):** [Download PC Build](link-to-build)
- **Android:** [Google Play Store](your-play-store-link)
- **iOS:** [Apple App Store](your-app-store-link)

### 🔒 Security
- Addresses Unity CVE-2025-59489 security vulnerability

See full patch notes in-app: **About → What's New**
```

6. Attach builds (optional):
   - `DLS-PC-2.1.6.10.zip`
   - `Digital-Logic-Sim-Mobile.apk` (optional)
7. Click **Publish release**

### 5.4 Close Ticket

- [ ] Mark Ticket 053 as **COMPLETE**
- [ ] Update project status
- [ ] Notify team/users of release

---

## 📊 SUMMARY

### Version Information
- **Version:** 2.1.6.10
- **Release Date:** October 12, 2025
- **Android Version Code:** 19
- **iOS Build Number:** 19

### Build Artifacts
- PC Build: `DLS_PC/`
- Android AAB: `Digital-Logic-Sim-Mobile.aab`
- Android APK: `Digital-Logic-Sim-Mobile.apk`
- iOS Archive: `DLS_IOS/` + Xcode archive

### Key Features in This Release
- ✅ Critical bit order validation bug fixed
- ✅ New D Latch educational level
- ✅ 25x faster validation performance
- ✅ Improved navigation and UI
- ✅ Unity security patch (CVE-2025-59489)

### Timeline Estimate
- **Phase 2 (Builds):** 1-2 hours
- **Phase 3 (Testing):** 2-3 hours
- **Phase 4 (Distribution):** 30 minutes
- **Phase 5 (Post-release):** 30 minutes
- **Store Review:** 1-3 days

**Total Active Time:** ~4-6 hours
**Total Calendar Time:** 1-3 days (including store reviews)

---

## 📝 NOTES

### Version Numbering
- Format: `MAJOR.MINOR.PATCH.BUILD`
- Current: `2.1.6.10`
- Next release will be: `2.1.6.11` or higher depending on changes

### Build Numbers
- Android: Increment by 1 for each release (current: 19)
- iOS: Match Android for consistency (current: 19)

### Testing Priority
1. **Critical:** Level validation (bit order fix)
2. **Critical:** D Latch level playability
3. **High:** Firebase integration
4. **High:** Patch notes popup display
5. **Medium:** Performance (validation speed)
6. **Medium:** Navigation improvements

---

**Good luck with the release! 🚀**

