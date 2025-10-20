# Ticket 060 - Testing Guide: Two-Finger Panning During Wire Placement

## 🎯 Quick Test (2 minutes)

### Test 1: Wire Placement - Single Finger Does NOT Pan
1. Open project on mobile device (Android or iOS)
2. Create a chip with at least two pins
3. Tap on a pin to start placing a wire
4. **Expected:** Yellow banner appears at top: "Placing Wire"
5. Use single finger to drag around the screen
6. **Expected:** Camera does NOT move, wire anchor point follows finger
7. Tap to place the wire anchor point
8. **Expected:** Wire point is placed, still in wire placement mode

### Test 2: Wire Placement - Two Fingers DO Pan
1. While still placing a wire (from Test 1)
2. Use two fingers to drag around the screen
3. **Expected:** Camera DOES pan/move
4. Release and use single finger again
5. **Expected:** Wire placement continues normally
6. **Success!** Wire placement and camera control are now separated

### Test 3: Banner Toggle (Bonus Feature!)
1. While placing a wire
2. Tap the yellow "Placing Wire" banner at the top
3. **Expected:** Text changes between:
   - "Straight Wires: Off"
   - "Straight Wires: If Grid Shown"
   - "Straight Wires: On"
4. Place a wire with "Straight Wires: On"
5. **Expected:** Wire follows straight grid-aligned paths
6. **Success!** Quick toggle works without opening preferences

### Test 4: Normal Mode Still Works
1. Cancel wire placement (tap cancel button or press back)
2. **Expected:** Banner disappears
3. Use single finger to drag around
4. **Expected:** Camera pans normally (old behavior restored)
5. Use two fingers to pinch
6. **Expected:** Zoom works normally
7. **Success!** Normal mode unchanged

---

## 📋 Detailed Test Scenarios

### Scenario 1: Basic Wire Placement
**Setup:**
- Open any chip with at least 2 pins
- Tap on the first pin to start wire placement

**Test Steps:**
1. Observe yellow banner appears: "Placing Wire"
2. Single-finger drag in different directions
3. Verify camera does NOT move
4. Verify wire anchor point follows finger
5. Tap to add wire points
6. Use two-finger drag
7. Verify camera DOES pan
8. Complete wire or cancel
9. Verify banner disappears

**Expected Results:**
- ✅ Banner appears on wire placement start
- ✅ Single finger: wire control only
- ✅ Two fingers: camera control
- ✅ Banner disappears on completion/cancel

---

### Scenario 2: Banner Toggle Functionality
**Setup:**
- Start placing a wire to show banner

**Test Steps:**
1. Read banner text (shows current straight wires mode)
2. Tap the banner
3. Observe mode changes: Off → If Grid Shown → On → Off
4. Set to "On" mode
5. Place wire and observe it snaps to grid
6. Set to "Off" mode
7. Place wire and observe free-form placement

**Expected Results:**
- ✅ Tap cycles through 3 modes
- ✅ Mode changes take effect immediately
- ✅ Wire behavior matches selected mode

---

### Scenario 3: Edge Case - Finger Add/Remove
**Setup:**
- Start placing a wire

**Test Steps:**
1. Place one finger on screen (wire control)
2. Add second finger while first is down
3. Verify camera starts panning
4. Remove second finger (keep first down)
5. Verify camera stops panning
6. Verify wire control resumes

**Expected Results:**
- ✅ Smooth transition when adding finger
- ✅ Smooth transition when removing finger
- ✅ No crashes or glitches

---

### Scenario 4: Banner Priority
**Setup:**
- Be in a level (to trigger level banner)
- Start placing a wire

**Test Steps:**
1. Verify wire placement banner shows (NOT level banner)
2. Cancel wire placement
3. Verify level banner now shows
4. Enable eraser mode
5. Verify eraser banner shows (NOT level banner)
6. Disable eraser mode
7. Verify level banner shows again

**Expected Results:**
- ✅ Wire placement banner has highest priority
- ✅ Banner switches correctly based on state
- ✅ No overlapping banners

---

### Scenario 5: Pinch-to-Zoom Still Works
**Setup:**
- Start placing a wire

**Test Steps:**
1. Use two fingers in pinch gesture (fingers moving apart/together)
2. Verify zoom in/out works
3. Complete wire placement
4. Use two fingers in pinch gesture again
5. Verify zoom still works in normal mode

**Expected Results:**
- ✅ Pinch-to-zoom works during wire placement
- ✅ Pinch-to-zoom works in normal mode
- ✅ No regression in zoom functionality

---

## 🐛 Issues to Watch For

### Potential Issues:
1. **Banner doesn't appear:** Check `Project.ActiveProject.controller.IsCreatingWire` is true
2. **Single finger still pans camera:** Check platform defines are correct (`UNITY_ANDROID || UNITY_IOS`)
3. **Banner doesn't toggle:** Check touch detection in banner hitbox
4. **Camera panning feels laggy with two fingers:** Expected behavior - may need touch input smoothing (future enhancement)

### Debugging:
- Check Unity console for debug logs: `[WirePlacementBanner]` and `[CameraController]`
- Verify build target is Android or iOS (not standalone)
- Ensure touch input is enabled in Unity Player Settings

---

## ✅ Success Criteria Checklist

### Must Have (Critical):
- [ ] Single-finger drag does NOT pan camera during wire placement
- [ ] Two-finger drag DOES pan camera during wire placement
- [ ] Banner appears when placing wire
- [ ] Banner disappears when not placing wire
- [ ] Normal mode (not placing wires) unchanged

### Should Have (Important):
- [ ] Banner toggle cycles through straight wires modes
- [ ] Mode changes take effect immediately
- [ ] Pinch-to-zoom works during wire placement
- [ ] No crashes or glitches during finger add/remove

### Nice to Have (Bonus):
- [ ] Smooth camera panning with two fingers
- [ ] Banner text is clear and readable
- [ ] Transitions between modes feel polished

---

## 📱 Platform-Specific Notes

### Android:
- Test on multiple screen sizes (phone, tablet)
- Test with different Android versions (API 21+)
- Verify touch input latency is acceptable

### iOS:
- Test on iPhone and iPad
- Test with different iOS versions (iOS 12+)
- Verify gesture recognizers don't conflict

### Editor Testing:
- Use "Unity Remote" app to test touch input in editor
- Mouse input simulates single touch only
- Full testing requires device build

---

## 🎬 Video Demo Checklist

Record a short video demonstrating:
1. **Before (problem):** Show frustration of accidental camera panning
2. **After (solution):**
   - Place wire with single finger (no camera pan)
   - Pan camera with two fingers (during wire placement)
   - Toggle straight wires mode via banner
   - Complete wire placement successfully
3. **Normal mode:** Show camera still pans with single finger when not placing wires

---

**Last Updated:** October 17, 2025  
**Status:** Ready for Testing  
**Build Required:** Yes (Android/iOS device build)

