# Complete Release Process Guide v2.0

**Digital Logic Sim Mobile - Multi-Platform Release Process**  
**Last Updated:** October 12, 2025  
**Version:** 2.0 (Post-v2.1.6.10 Release)

---

## 📋 **Overview**

This guide documents the complete, tested release process for Digital Logic Sim Mobile across all platforms (PC, Android, iOS). Updated based on lessons learned from v2.1.6.10 release.

---

## 🎯 **Release Process Summary**

### **Phase 1: Pre-Release Preparation**
1. **Update Version Numbers** (Unity Project Settings)
2. **Update Patch Notes** (Single source of truth system)
3. **Verify Git Sync** (Critical - check for untracked files)
4. **Test Core Functionality** (Basic smoke tests)

### **Phase 2: Multi-Platform Builds**
1. **PC Build** (Unity → Windows executable)
2. **Android Build** (Unity → AAB for Google Play)
3. **iOS Build** (Unity → MacInCloud → Xcode → TestFlight)

### **Phase 3: Distribution & Testing**
1. **PC Distribution** (Google Drive → Discord)
2. **Android Distribution** (Google Play Console)
3. **iOS Distribution** (TestFlight via App Store Connect)

### **Phase 4: Post-Release**
1. **Monitor Reviews** (All platforms)
2. **Collect Feedback** (Discord, TestFlight)
3. **Document Lessons Learned**
4. **Update Process Guides**

---

## 🔧 **Phase 1: Pre-Release Preparation**

### **1.1 Update Version Numbers**

**Unity Project Settings:**
```
Edit → Project Settings → Player
├── Version: 2.1.6.10 (or next version)
├── Android → Bundle Version Code: 19 (increment by 1)
└── iOS → Build: 19 (or appropriate number)
```

**Critical Check:**
- Verify version displays correctly in About menu
- Ensure version numbers are consistent across platforms

### **1.2 Update Patch Notes**

**Using the Patch Notes Editor:**
1. **Open**: `Scripts/PatchNotesEditor.html`
2. **Load**: Current `patchNotes.json`
3. **Edit**: Add new version with features/fixes
4. **Save**: Export updated JSON
5. **Copy**: To `Assets/Resources/patchNotes.json`

**Key Features:**
- ✅ Single source of truth
- ✅ Visual editing with metadata
- ✅ User-facing vs. internal notes
- ✅ Automatic JSON generation

### **1.3 Critical Git Sync Check**

**MANDATORY - Check for Missing Files:**
```bash
# Check for untracked C# files
git status --porcelain | findstr "\.cs$"

# Check for untracked asset files  
git status --porcelain | findstr "\.asset$"

# Check for untracked meta files
git status --porcelain | findstr "\.meta$"

# If any files found, add and commit:
git add <missing-files>
git commit -m "Add missing files for release"
git push origin main
```

**Why This Matters:**
- Mac builds fail if C# files are missing
- iOS builds use old themes if assets are missing
- Compilation errors occur across platforms

### **1.4 Pre-Build Testing**

**Basic Smoke Tests:**
- [ ] Unity Editor runs without errors
- [ ] About menu shows correct version
- [ ] Patch notes popup displays correctly
- [ ] Core gameplay functions work
- [ ] Firebase features accessible

---

## 🖥️ **Phase 2.1: PC Build Process**

### **Build Settings:**
```
File → Build Settings
├── Platform: PC, Mac & Linux Standalone
├── Target Platform: Windows
├── Architecture: x86_64
└── Build Location: DLS_PC/
```

### **Build Steps:**
1. **Switch Platform** (if needed)
2. **Build** (not "Build and Run")
3. **Wait for completion** (5-15 minutes)
4. **Test executable** (double-click to launch)
5. **Verify version** in About menu

### **Distribution Preparation:**
1. **Create ZIP** of build folder
2. **Upload to Google Drive**
3. **Set sharing** to "Anyone with the link"
4. **Enable download tracking** (optional)

### **Common Issues:**
- **Memory errors**: Increase virtual memory, close other apps
- **Large file size**: Check for unused assets
- **Antivirus warnings**: Expected for Unity executables

---

## 📱 **Phase 2.2: Android Build Process**

### **Build Settings:**
```
File → Build Settings
├── Platform: Android
├── Build System: Gradle
├── Build App Bundle (Google Play): ✅
└── Build Location: Choose folder
```

### **Build Steps:**
1. **Switch Platform** (if needed, may take 10-30 minutes)
2. **Build AAB** (recommended for Google Play)
3. **Optionally Build APK** (for direct distribution)
4. **Wait for Gradle build** (5-10 minutes)

### **Google Play Console Upload:**
1. **Go to**: [Google Play Console](https://play.google.com/console/)
2. **Select app**: Digital Logic Sim Mobile
3. **Production/Testing**: Choose appropriate track
4. **Upload AAB**: Drag and drop or browse
5. **Add release notes**: Copy from patch notes
6. **Submit for review**: Wait 1-3 days

### **Common Issues:**
- **Gradle errors**: Update Android SDK, check JDK version
- **Bundle too large**: Enable compression, remove unused assets
- **Signing issues**: Verify keystore and certificates

---

## 🍎 **Phase 2.3: iOS Build Process**

### **Setup Requirements:**
- **MacInCloud** rental (or physical Mac)
- **Apple Developer Account**
- **Xcode** (latest stable version)

### **MacInCloud Workflow:**
```bash
# 1. Connect to MacInCloud
# 2. Navigate to project directory
cd /path/to/project/Digital-Logic-Sim

# 3. Sync with Git
git pull origin main

# 4. Verify files exist
ls -la Assets/Scripts/Graphics/PatchNotesData.cs
ls -la Assets/Resources/SquigglesTheme.asset
```

### **Unity iOS Build:**
```
File → Build Settings
├── Platform: iOS
├── Switch Platform (if needed)
└── Build Location: DLS_IOS/
```

### **Xcode Configuration:**
1. **Open**: `Unity-iPhone.xcworkspace` (not .xcodeproj)
2. **Project Settings**:
   - Version: `1.6.10` (max 3 components for Apple)
   - Build: `19`
   - Bundle ID: `com.DavidCarpenfelt.Digital-Logic-Sim-Mobile`
3. **Signing**: Enable automatic signing, select team
4. **Archive**: Product → Archive
5. **Upload**: Distribute App → App Store Connect

### **TestFlight Setup:**
1. **Go to**: [App Store Connect](https://appstoreconnect.apple.com/)
2. **TestFlight tab**: Select your app
3. **External Testing**: Add testers or use existing group
4. **What to Test**: Add release notes for testers
5. **Submit for Review**: Wait for Apple review

### **Common Issues:**
- **Missing files**: Git sync problems (see Phase 1.3)
- **Theme errors**: SquigglesTheme.asset not synced
- **Version format**: Apple requires max 3 components
- **Signing errors**: Check Apple Developer account status

---

## 📢 **Phase 3: Distribution & Community**

### **3.1 Discord Announcement**

**Template:**
```markdown
# 🚀 Digital Logic Sim Mobile v2.1.6.10 - Major Update!

## ✨ New Features
• [List new features from patch notes]

## 🔧 Improvements & Fixes  
• [List improvements and fixes]

## 📦 Available Now:
• **PC Build:** [Google Drive Link] (45MB)
• **Android Testing:** [Join Open Testing]
• **iOS Testing:** [Join TestFlight]

**Feedback welcome!** Report issues on Discord.
```

### **3.2 Platform-Specific Distribution**

**PC:**
- Google Drive link in Discord
- Enable download tracking
- Monitor for antivirus false positives

**Android:**
- Google Play Console upload
- Open testing program
- Monitor review status

**iOS:**
- TestFlight external testing
- Monitor Apple review
- Respond to any rejection notices

---

## 📊 **Phase 4: Post-Release Monitoring**

### **4.1 Review Status Tracking**

**Daily Checks:**
- [ ] Google Play Console review status
- [ ] TestFlight review status
- [ ] User feedback from Discord
- [ ] Crash reports (if any)

### **4.2 Feedback Collection**

**Sources:**
- **Discord**: Community feedback and bug reports
- **TestFlight**: Built-in feedback system
- **Google Play**: Review comments
- **Analytics**: Usage data (if available)

### **4.3 Issue Response**

**Priority Levels:**
1. **Critical**: App crashes, major functionality broken
2. **High**: Important features not working
3. **Medium**: UI issues, minor bugs
4. **Low**: Polish, edge cases

**Response Process:**
1. **Acknowledge** issue on Discord
2. **Investigate** and reproduce
3. **Estimate** fix timeline
4. **Update** community on progress

---

## 🚨 **Critical Lessons Learned**

### **1. Git Sync is Critical**
- **Always check** for untracked files before building
- **Missing C# files** cause Mac compilation errors
- **Missing assets** cause iOS theme issues
- **Solution**: Mandatory `git status` checks in Phase 1.3

### **2. Platform-Specific Requirements**
- **iOS version format**: Max 3 components (1.6.10, not 2.1.6.10)
- **Android bundle code**: Must be integer, increment by 1
- **PC memory**: May need increased virtual memory for IL2CPP

### **3. Patch Notes Management**
- **Single source of truth**: `Assets/Resources/patchNotes.json`
- **Visual editor**: `Scripts/PatchNotesEditor.html`
- **Metadata support**: User-facing vs. internal notes
- **No duplication**: Eliminates version conflicts

### **4. Build Process Optimization**
- **PC**: 5-15 minutes, test executable
- **Android**: 10-20 minutes with Gradle
- **iOS**: 30-60 minutes (Unity + Xcode + upload)

---

## 🔄 **Process Improvements for Next Release**

### **Automation Opportunities:**
1. **Git status checker**: Automated script to find untracked files
2. **Version validator**: Check iOS format compliance
3. **Build automation**: Automated build scripts for all platforms
4. **Release notes generator**: Auto-generate from patch notes

### **Documentation Updates:**
1. **Platform-specific guides**: Separate guides for each platform
2. **Troubleshooting database**: Common issues and solutions
3. **Video tutorials**: Screen recordings of build processes
4. **Checklist automation**: Interactive checklists

### **Quality Assurance:**
1. **Automated testing**: Basic functionality tests
2. **Cross-platform validation**: Ensure feature parity
3. **Performance monitoring**: Track build times and file sizes
4. **User acceptance testing**: Systematic tester feedback

---

## 📞 **Emergency Procedures**

### **Critical Issues:**
1. **Hotfix process**: Emergency patch for critical bugs
2. **Rollback procedures**: Revert to previous stable version
3. **Communication plan**: Notify community of issues
4. **Recovery timeline**: Target resolution times

### **Build Failures:**
1. **PC build fails**: Check memory, clean cache, restart
2. **Android build fails**: Update SDK, check Gradle
3. **iOS build fails**: Verify MacInCloud, check Xcode
4. **Upload fails**: Check platform requirements, try again

---

## 📚 **Resources & References**

### **Platform Documentation:**
- [Unity iOS Build Guide](https://docs.unity3d.com/Manual/ios-BuildProcess.html)
- [Google Play Console Help](https://support.google.com/googleplay/android-developer)
- [Apple TestFlight Guide](https://developer.apple.com/testflight/)

### **Project-Specific:**
- [Patch Notes System Guide](ProjectInstructions/Guides/Patch_Notes_System_Guide.md)
- [iOS Build Process](iOS_Build_Process_v2.1.6.10.md)
- [Release Completion Report](ProjectInstructions/Tickets/Ticket_053_Release_Completion_Report.md)

### **Community Channels:**
- **Discord**: [Digital Logic Sim Mobile Releases](https://discord.com/channels/1361307968276136007/1426249925544382595)
- **Support Email**: carpen97+DigitalLogicSimMobile@gmail.com

---

**This guide represents the complete, tested release process for Digital Logic Sim Mobile. Update it with lessons learned from each release.**

*Last Updated: October 12, 2025 - Post v2.1.6.10 Release*
