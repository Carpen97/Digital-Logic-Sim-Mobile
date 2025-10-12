# Release Process Guide

**Digital Logic Sim Mobile - Multi-Platform Release**

This guide documents the complete process for preparing and publishing a new release across all platforms (PC, Android, iOS).

---

## 📋 Pre-Release Checklist

Before starting the release process, ensure:
- [ ] All tickets for this release are completed and merged to `main`
- [ ] All changes are committed and pushed to GitHub
- [ ] Patch notes are up to date with all changes
- [ ] No critical bugs or issues remain
- [ ] Testing has been completed for all major features

---

## 🔄 Release Process

### **Phase 1: Documentation Updates**

#### 1.1 Update PatchNotes.md

**Location**: `ProjectInstructions/PatchNotes.md`

**Steps:**
1. Open `PatchNotes.md`
2. Locate the current version section (e.g., `### **Version 2.1.6.10**`)
3. Update the release date to today's date
4. Review all entries under:
   - **Bug Fixes**
   - **New Features**
   - **Improvements**
5. Ensure all user-facing changes from completed tickets are documented
6. Verify language is clear and engaging from a user perspective
7. Save the file

**Format:**
```markdown
### **Version X.X.X** - YYYY-MM-DD

**Bug Fixes:**
- Description of bug fix from user perspective

**New Features:**
- Description of new feature

**Improvements:**
- Description of improvement
```

#### 1.2 Update Unity Project Settings

**Location**: Unity Editor → `Edit → Project Settings → Player`

**Steps:**
1. Open Unity Editor
2. Go to `Edit → Project Settings → Player`
3. Update **Version** field: `X.X.X` (e.g., `2.1.6.10`)
4. **For Android**:
   - Expand **Android** tab
   - Update **Bundle Version Code**: Increment by 1 (e.g., 17 → 18)
   - This must be unique and higher than previous release
5. **For iOS**:
   - Expand **iOS** tab
   - Update **Build**: Increment appropriately (e.g., `1.0.17` → `1.0.18`)
6. Click **Apply** or ensure auto-save is enabled
7. Save the scene and project (`Ctrl+S`)

**Important Notes:**
- Bundle Version Code (Android) must be an integer that increases with each release
- Build number (iOS) can be any string but typically follows versioning scheme
- These numbers are used by stores to determine which version is newer

#### 1.3 Verify About Menu Version Display

**Location**: Check in-game About menu

**Steps:**
1. Play the game in Unity Editor (or use a test build)
2. Navigate to **About** menu
3. Verify the version number displays correctly (e.g., `2.1.6.10`)
4. If version is hardcoded and needs updating:
   - Find the About menu script (e.g., `MainMenu.cs` or `AboutMenuUIController.cs`)
   - Update the version string
   - Save and test again

---

### **Phase 2: Build Process**

#### 2.1 PC/Windows Build

**Steps:**
1. In Unity Editor, go to `File → Build Settings`
2. Ensure platform is set to **PC, Mac & Linux Standalone**
   - If not, select it and click **Switch Platform** (may take a few minutes)
3. **Build Settings:**
   - **Target Platform**: Windows
   - **Architecture**: x86_64
4. Click **Build** (not "Build and Run")
5. Choose build location:
   - Create or select folder: `DLS_PC/` or `Builds/PC/`
   - Name the executable: `Digital-Logic-Sim.exe` or similar
6. Wait for build to complete (progress bar will show)
7. Navigate to build folder and verify executable exists

**Testing PC Build:**
- Double-click the executable to launch
- Verify version in About menu
- Test basic functionality (play a level, save/load)
- Check for any errors in the output log

#### 2.2 Android Build

**Steps:**
1. In Unity Editor, go to `File → Build Settings`
2. Ensure platform is set to **Android**
   - If not, select it and click **Switch Platform** (may take several minutes)
3. **Build Settings:**
   - **Build System**: Gradle (recommended)
   - Check **Build App Bundle (Google Play)** for AAB
   - Optionally also build APK for direct distribution
4. Click **Build** for AAB
5. Choose build location:
   - Save as: `Digital-Logic-Sim-Mobile.aab` (or with version number)
6. Wait for build to complete (Gradle build may take 5-10 minutes)
7. If building APK, repeat with APK option selected

**Optional: Build APK for direct distribution**
1. Uncheck **Build App Bundle**
2. Click **Build**
3. Save as: `Digital-Logic-Sim-Mobile.apk`

**Testing Android Build:**
- **Using ADB (USB debugging)**:
  ```bash
  adb install -r Digital-Logic-Sim-Mobile.apk
  ```
- Or copy APK to device and install manually
- Launch app and verify version
- Test on actual Android device if possible

#### 2.3 iOS Build

**Steps:**
1. In Unity Editor, go to `File → Build Settings`
2. Ensure platform is set to **iOS**
   - If not, select it and click **Switch Platform** (may take several minutes)
3. **iOS Build Settings:**
   - Verify settings match your Apple Developer setup
4. Click **Build** (not "Build and Run")
5. Choose build location:
   - Create or select folder: `DLS_IOS/` or `Builds/iOS/`
6. Wait for Unity to generate Xcode project (a few minutes)
7. **Open in Xcode:**
   - Navigate to build folder
   - Open `Unity-iPhone.xcodeproj` or `Unity-iPhone.xcworkspace`
8. **In Xcode:**
   - Select the project in left sidebar
   - Verify **Version** and **Build** numbers match
   - Select target device or **Any iOS Device (arm64)**
   - Go to `Product → Archive`
   - Wait for archive to complete
9. **Organizer Window:**
   - Xcode will open the Organizer automatically
   - Select your archive
   - Click **Distribute App** for App Store submission
   - Or **Export** for local testing

**Testing iOS Build:**
- Connect iPad/iPhone via USB
- In Xcode, select your device as target
- Click **Run** (play button) to build and install
- Test on actual device
- Verify version in About menu

---

### **Phase 3: Testing**

#### 3.1 PC Testing Checklist

**Basic Functionality:**
- [ ] Application launches without errors
- [ ] Version displays correctly in About menu (`X.X.X`)
- [ ] Main menu loads properly
- [ ] UI is responsive

**Level System:**
- [ ] Levels menu displays correctly
- [ ] Can select and play levels
- [ ] Level validation works (test pass/fail scenarios)
- [ ] New levels appear correctly (e.g., D Latch)
- [ ] "Coming Soon" chapter displays if added

**Firebase Integration:**
- [ ] Can upload scores
- [ ] Leaderboards display correctly
- [ ] Solution sharing works
- [ ] User name input works

**Recent Features:**
- [ ] Test any features from recent tickets
- [ ] Patch notes popup displays correctly
- [ ] Any UI improvements work as expected

**Performance:**
- [ ] No crashes or freezes
- [ ] Acceptable frame rate
- [ ] No memory leaks during extended play

#### 3.2 Android Testing Checklist

**Basic Functionality:**
- [ ] App installs without errors
- [ ] App launches without crashes
- [ ] Version displays correctly in About menu
- [ ] Main menu loads properly
- [ ] Touch controls are responsive

**Level System:**
- [ ] Levels menu displays correctly on mobile
- [ ] Can select and play levels with touch
- [ ] Level validation works
- [ ] New levels appear correctly
- [ ] Scrollable info panel works (if implemented)

**Firebase Integration:**
- [ ] Can upload scores
- [ ] Leaderboards load
- [ ] Solution sharing works
- [ ] User name input works on mobile keyboard

**File Operations:**
- [ ] Can import project (zip file selection)
- [ ] Can export project
- [ ] File picker works correctly

**Mobile-Specific:**
- [ ] UI scales correctly on different screen sizes
- [ ] No UI elements are cut off
- [ ] Touch gestures work (pinch-to-zoom if applicable)
- [ ] Orientation handling (if applicable)

**Performance:**
- [ ] No crashes during gameplay
- [ ] Acceptable frame rate on target devices
- [ ] Battery usage is reasonable

#### 3.3 iOS Testing Checklist

**Basic Functionality:**
- [ ] App installs via TestFlight or Xcode
- [ ] App launches without crashes
- [ ] Version displays correctly in About menu
- [ ] Main menu loads properly
- [ ] Touch controls are responsive

**Level System:**
- [ ] Levels menu displays correctly
- [ ] Can select and play levels
- [ ] Level validation works
- [ ] New content appears correctly

**Firebase Integration:**
- [ ] Can upload scores
- [ ] Leaderboards load
- [ ] Solution sharing works
- [ ] User name input works

**File Operations:**
- [ ] Can import project
- [ ] **ZIP file selection works** (this was a bug, ensure fix is working)
- [ ] Can export project

**iOS-Specific:**
- [ ] UI scales correctly on iPad and iPhone
- [ ] Safe area handling is correct (notch devices)
- [ ] No UI overlap issues
- [ ] Touch gestures work smoothly

**Performance:**
- [ ] No crashes on iOS
- [ ] Acceptable performance on older devices
- [ ] No memory warnings

---

### **Phase 4: Prepare Store Listings**

#### 4.1 Google Play Store (Android)

**What's New Section** (500 character limit):

```
What's New in Version X.X.X:

🐛 Critical Bug Fixes:
- [List major bug fixes]

✨ New Features:
- [List new features]

🎨 Improvements:
- [List improvements]

🔒 Security:
- [Any security updates]

Your feedback helps us improve! Share suggestions at @Carpen# on Discord.
```

**Tips:**
- Keep it concise (500 character limit enforced by Google Play)
- Use emojis for visual appeal
- Focus on user benefits
- Mention community channel

#### 4.2 App Store (iOS)

**What's New** (4000 character limit):

```
What's New:

Critical Fixes:
• [Bug fix description]
• [Bug fix description]

New Features:
• [Feature description]
• [Feature description]

Improvements:
• [Improvement description]
• [Improvement description]

Security:
• [Security update description]

Share your ideas on Discord: @Carpen#
```

**Tips:**
- More space available than Google Play
- Use bullet points (•) for better readability
- Can be more detailed about changes
- Mention community engagement

#### 4.3 PC Distribution (itch.io / Steam / etc.)

**Release Notes:**

```
Version X.X.X - YYYY-MM-DD

Critical Fixes:
- [Detailed bug fix description]
- [Detailed bug fix description]

New Content:
- [Feature description]
- [Feature description]

Improvements:
- [Improvement description]
- [Improvement description]

Technical:
- [Unity version update if applicable]
- [Performance improvements]
```

**Tips:**
- PC users often appreciate more technical details
- Can be more verbose than mobile
- Include performance notes if relevant
- Mention system requirements if changed

---

### **Phase 5: Distribution**

#### 5.1 Google Play Console

**Steps:**
1. Go to [Google Play Console](https://play.google.com/console/)
2. Select your app: **Digital Logic Sim Mobile**
3. Navigate to **Production** (or **Testing** for beta)
4. Click **Create new release**
5. **Upload AAB:**
   - Drag and drop the `.aab` file
   - Or click **Browse files** and select it
6. Wait for upload and processing
7. **Release name**: `X.X.X` (auto-filled from bundle version)
8. **Release notes**:
   - Copy your prepared "What's New" text
   - Add translations if available
9. **Review and rollout:**
   - Click **Review release**
   - Check for any warnings or errors
   - Click **Start rollout to Production**
10. Confirm rollout percentage (usually 100% for stable releases)
11. **Submit for review**
12. Wait for Google's review (usually 1-3 days)

**Post-Submission:**
- Monitor the **Publishing overview** page
- Check for any rejection notices
- Respond to any review questions promptly

#### 5.2 App Store Connect (iOS)

**Steps:**
1. **Upload build via Xcode:**
   - In Xcode Organizer, select your archive
   - Click **Distribute App**
   - Choose **App Store Connect**
   - Follow the distribution wizard
   - Wait for upload to complete (may take 10-30 minutes)
2. Go to [App Store Connect](https://appstoreconnect.apple.com/)
3. Select your app: **Digital Logic Sim**
4. Click **+ Version or Platform** or select existing version
5. **Version Information:**
   - Version number: `X.X.X`
   - Build: Select the build you just uploaded (may take time to process)
6. **What's New in This Version:**
   - Paste your prepared release notes
7. **Screenshots:** Update if needed
8. **Keywords:** Update if relevant
9. **Review Information:** Ensure contact info is current
10. Click **Save**
11. Click **Submit for Review**
12. Wait for Apple's review (usually 1-3 days)

**Post-Submission:**
- Monitor status in App Store Connect
- Respond to any review questions via Resolution Center
- Check for any rejection notices

#### 5.3 PC Distribution (itch.io example)

**Steps:**
1. Go to [itch.io Dashboard](https://itch.io/dashboard)
2. Select your game: **Digital Logic Sim**
3. **Upload Files:**
   - Click **Upload files**
   - Select the build folder or create a ZIP:
     - Include: Executable, data folder, dependencies
     - Name: `Digital-Logic-Sim-vX.X.X-Windows.zip`
   - Wait for upload
4. **Version/Changelog:**
   - In **Devlog** or **Edit game**
   - Post your release notes
   - Version: `X.X.X - YYYY-MM-DD`
5. **Update game description** if major features added
6. **Publish changes**

**Other PC Platforms:**
- **Steam**: Use Steamworks SDK and depot builder
- **GOG**: Contact GOG for build submission process
- **Epic Games Store**: Use Epic Games Store publishing tools

---

### **Phase 6: Post-Release**

#### 6.1 Update Documentation

**Files to Update:**
1. **README.md** (if exists):
   - Update version number references
   - Update download links if changed
   - Add release date

2. **Any version-specific documentation**:
   - Update tutorials if features changed
   - Update screenshots if UI changed

3. **Commit changes:**
   ```bash
   git add ProjectInstructions/PatchNotes.md
   git add [any other changed files]
   git commit -m "Update patch notes release date for v X.X.X"
   git push origin main
   ```

#### 6.2 Create Git Release Tag

**Steps:**
1. Open terminal in project directory
2. Create annotated tag:
   ```bash
   git tag -a vX.X.X -m "Release Version X.X.X - [Brief description]"
   ```
   Example:
   ```bash
   git tag -a v2.1.6.10 -m "Release Version 2.1.6.10 - Critical bug fixes, new features, and security updates"
   ```
3. Push tag to GitHub:
   ```bash
   git push origin vX.X.X
   ```
   Example:
   ```bash
   git push origin v2.1.6.10
   ```
4. **Create GitHub Release:**
   - Go to your GitHub repository
   - Click **Releases** tab
   - Click **Draft a new release**
   - Select the tag you just pushed
   - Release title: `Version X.X.X`
   - Description: Copy release notes from PatchNotes.md
   - Click **Publish release**

#### 6.3 Close Release Ticket

**In ProjectPlan.md:**
1. Move the release ticket from "In Progress" to "Completed"
2. Update CompletedTickets.md with release summary
3. Update ticket counts
4. Commit changes:
   ```bash
   git commit -a -m "Close Ticket XXX: Release Version X.X.X completed"
   git push origin main
   ```

---

## 📊 Release Checklist Summary

Use this quick checklist during release:

### Pre-Release
- [ ] All tickets completed and merged
- [ ] All changes pushed to GitHub
- [ ] Patch notes reviewed and complete

### Documentation
- [ ] Patch notes date updated
- [ ] Unity project settings updated (version, bundle code, build number)
- [ ] About menu version verified

### Builds
- [ ] PC build created and tested
- [ ] Android AAB/APK created and tested
- [ ] iOS build created and tested in Xcode

### Testing
- [ ] PC tested (functionality, Firebase, performance)
- [ ] Android tested (UI, touch, file operations, performance)
- [ ] iOS tested (UI, safe areas, file picker, performance)

### Distribution
- [ ] Google Play release notes prepared
- [ ] App Store release notes prepared
- [ ] PC distribution release notes prepared
- [ ] Android AAB uploaded to Google Play
- [ ] iOS build uploaded via Xcode to App Store Connect
- [ ] PC build uploaded to distribution platform

### Post-Release
- [ ] Documentation updated
- [ ] Git release tag created and pushed
- [ ] GitHub Release created
- [ ] Release ticket closed in ProjectPlan.md
- [ ] All changes committed and pushed

---

## 🚨 Common Issues and Solutions

### Unity Build Issues

**Issue**: Build fails with errors
- Check Unity console for specific errors
- Verify all scripts compile without errors
- Check that all assets are properly referenced
- Try cleaning build cache: `Build → Clean All`

**Issue**: Platform switch takes very long
- This is normal for first-time platform switches
- Can take 10-30 minutes depending on project size
- Subsequent switches are faster

### Android Build Issues

**Issue**: Gradle build fails
- Check Android SDK is up to date
- Verify JDK version compatibility
- Try `Build → Clean All` then rebuild
- Check Unity console for specific Gradle errors

**Issue**: APK too large
- Check for unused assets
- Enable compression in Build Settings
- Consider using AAB format (smaller)

### iOS Build Issues

**Issue**: Code signing errors
- Verify Apple Developer account is active
- Check certificates are valid in Xcode
- Ensure provisioning profile is correct
- Try automatic signing in Xcode

**Issue**: Xcode project doesn't open
- Verify Xcode is installed and up to date
- Open `.xcworkspace` if it exists (instead of `.xcodeproj`)
- Try regenerating from Unity

### Store Submission Issues

**Issue**: Google Play rejection
- Check for policy violations
- Verify APK/AAB is signed correctly
- Review crash reports if any
- Check target SDK version meets requirements

**Issue**: App Store rejection
- Review Apple's rejection reason carefully
- Common issues: privacy policy, permissions, content
- Respond via Resolution Center
- Fix issues and resubmit

---

## 📝 Notes

### Version Numbering Scheme
- Format: `MAJOR.MINOR.PATCH` (e.g., `2.1.6`)
- **MAJOR**: Significant changes, major new features
- **MINOR**: New features, significant improvements
- **PATCH**: Bug fixes, small improvements

### Build Number Guidelines
- **Android Bundle Version Code**: Integer that must increase (17, 18, 19, ...)
- **iOS Build Number**: Can be any string, typically matches version or increments (1.0.17, 1.0.18, ...)
- Both must be unique and higher than previous release

### Testing Priorities
1. **Critical**: App launches, version displays, core gameplay
2. **High**: Firebase, file operations, new features
3. **Medium**: UI polish, performance optimizations
4. **Low**: Minor visual issues, edge cases

### Timeline Estimates
- **Documentation**: 30-60 minutes
- **PC Build**: 5-15 minutes
- **Android Build**: 10-20 minutes (Gradle)
- **iOS Build**: 15-30 minutes (Unity + Xcode)
- **Testing**: 1-2 hours per platform
- **Store Submission**: 30-60 minutes per platform
- **Total**: 4-8 hours for complete release

---

## 🔄 Continuous Improvement

After each release, consider:
- What went smoothly?
- What caused delays or issues?
- How can the process be improved?
- Update this guide with lessons learned

---

**This guide should be updated as the release process evolves.**

*Last Updated: October 12, 2025*
*Version: 1.0*

