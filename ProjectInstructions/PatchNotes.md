# Patch Notes

This document tracks user-facing changes and improvements for Digital Logic Sim Mobile releases. Patch notes are written from a user perspective, focusing on features and changes that directly impact the user experience.

---

## 🎯 **Purpose**
- **User-focused**: Only document changes that affect user experience
- **Release-oriented**: Track what's new since the last release
- **Accessible**: Popup accessible from About menu in the app
- **Concise**: Clear, user-friendly descriptions of new features and improvements

---

## 📋 **Workflow Integration**
After each ticket completion, ask: **"Should this be noted in patch notes?"**

**Include in patch notes:**
- ✅ New user-facing features
- ✅ UI/UX improvements visible to users
- ✅ New functionality users can access
- ✅ Bug fixes that improve user experience
- ✅ Performance improvements users will notice

**Exclude from patch notes:**
- ❌ Internal code refactoring
- ❌ Developer-only changes
- ❌ Backend infrastructure updates
- ❌ Technical implementation details
- ❌ Failed investigations or cancelled features

---

## 📝 **Patch Notes Format**

### **Version X.X.X** - Release Date
**New Features:**
- Feature description from user perspective

**Improvements:**
- Enhancement description from user perspective

**Bug Fixes:**
- Fix description from user perspective

---

## 🚀 **Current Release Notes**

### **Version 2.1.6.10** - 2025-10-11

**Bug Fixes:**
- **CRITICAL: Fixed bit order bug in level validation** - Resolved game-breaking bug where inputs/outputs were being reversed during validation, affecting all 26 levels. Game now correctly validates solutions.
- **Fixed D Flip-Flop level** - Corrected output count mismatch that made the level unplayable
- **Fixed 4-bit Subtractor** - Corrected all 26 test vectors for accurate validation
- **Fixed Simple ALU** - Regenerated 27 test vectors with correct validation logic
- **Enhanced Complete 4-bit ALU** - Expanded test coverage from 21 to 1024 vectors with optimized random sampling (40 tests for 25x faster validation)

**New Features:**
- **Added D Latch level** - New educational level fills gap in sequential logic progression
- **Test Vector Generator** (Developer Tool) - Press 'G' key to automatically generate comprehensive test vectors from working circuits
- **Random Test Sampling** - Performance optimization caps validation at 40 tests for complex levels
- **Setup Phase Support** - Sequential circuits can now initialize state before validation begins

**Improvements:**
- **Level selection behavior** - Improved UI interaction in levels menu
- **Validation performance** - 25x faster validation for complex levels with comprehensive coverage

---

### **Version 2.1.6.9** - 2025-01-28

**New Features:**
- **Patch Notes Popup**: View "What's New" directly from the About menu! See recent features, improvements, and bug fixes with color-coded sections. Easy to navigate with version selection and scrollable content.

**Improvements:**
- **PC Firebase Integration**: Full Firebase functionality now works on PC builds (Windows, macOS, Linux). PC users can now upload scores, view leaderboards, share solutions, and set user names just like mobile users. All online features are now available across all platforms.
- **Auto-Open Edit Tool**: Single component selected + wrench press now automatically opens the edit menu, reducing taps by 50% for faster workflow
- **Selectable Chapters in Levels Menu**: Chapters now show educational descriptions when selected, with color-coded banners, auto-selection of next incomplete level, and improved mobile touch experience. Better learning journey with context before diving into levels.

---

### **Version 2.1.6.8** - 2025-01-27

**New Features:**
- **Hierarchical Collection Organization**: Create and manage sub folders within collections for better chip organization. Drag and drop chips into folders, navigate through folder hierarchy, and organize your chip library with visual folder indicators and breadcrumb navigation.
- **PC Version**: Full mobile features now available on PC with mouse and keyboard support
- **Solution Sharing**: Upload and view complete solutions from leaderboard entries
- **User Names**: Add custom names when uploading scores to leaderboards
- **Level Progress**: Save chip shortcuts available directly from level completion screens
- **I/O Pin Names**: Toggle display of input/output pin names in levels for better guidance
- **iOS Platform Support**: Full iOS support with project import/export and Firebase integration working seamlessly on iPad and iPhone

**Improvements:**
- **Enhanced Level System**: Expanded with more challenging levels and progressive difficulty
- **Improved UI Navigation**: Better folder browsing and collection management
- **Cross-Platform Compatibility**: Seamless experience across mobile and PC platforms
- **Clearer Score Explanation**: Updated scoring information to better explain how nested NAND gates are counted in level solutions
- **Auto-Open Edit Tool**: Single component selected + wrench press now automatically opens the edit menu, reducing taps by 50% for faster workflow
- **Selectable Chapters in Levels Menu**: Chapters now show educational descriptions when selected, with color-coded banners, auto-selection of next incomplete level, and improved mobile touch experience. Better learning journey with context before diving into levels.

**Bug Fixes:**
- Fixed iOS file picker for importing project zip files
- Various stability improvements and performance optimizations

---

## 📊 **Statistics**
- **Total Features Added:** 12
- **Latest Update:** 2025-10-11
- **User-Facing Changes:** Critical bug fix (level validation), new D Latch level, test vector generator, enhanced levels, patch notes popup, PC Firebase integration

---

*This document is maintained as tickets are completed and user-facing changes are identified.*
