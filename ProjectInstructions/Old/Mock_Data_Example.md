# Mock Leaderboard Data - Visual Example

## What You'll See in the Leaderboard

When you open the leaderboard in Unity Editor with Android build target, you'll see entries like this:

```
┌─────────────────────────── LEADERBOARD - NOT Gate ───────────────────────────┐
│                                                                                │
│  Top 10 scores                                                                │
│                                                                                │
│  ┌──────────────────────────────────────────────────────────────────────┐    │
│  │ Rank    Score    User                                   Date          │    │
│  ├──────────────────────────────────────────────────────────────────────┤    │
│  │ #1      5        Alice                                 01/08 14:23    │    │
│  │ #2      7        Bob                                   01/07 09:45    │    │
│  │ #3      8        VeryLongUserNameThatShouldBeTru...   01/06 18:12    │    │
│  │ #4      11       Diana                                 01/05 22:34    │    │
│  │ #5      13       🎮Gamer42                             01/04 11:56    │    │
│  │ #6      14       Frank                                 01/03 16:21    │    │
│  │ #7      17       Katherine_with_a_really_long...      01/02 08:45    │    │
│  │ #8      19       Henry                                 01/01 19:33    │    │
│  │ #9      20       Player_12345                          12/31 14:27    │    │
│  │ #10     23       Jack                                  12/30 10:18    │    │
│  └──────────────────────────────────────────────────────────────────────┘    │
│                                                                                │
│                       [VIEW]              [CANCEL]                             │
│                                                                                │
└────────────────────────────────────────────────────────────────────────────────┘
```

## Mock Data Features

### Username Variety
The mock data includes different types of usernames to test UI edge cases:

**Normal Names:**
- `Alice`, `Bob`, `Charlie`, `Diana`, `Eve`, `Frank`, `Grace`, `Henry`, `Ivy`, `Jack`

**Long Names (should be truncated):**
- `Katherine_with_a_really_long_name_to_test_truncation` → displays as `Katherine_with_a_really_long...`
- `VeryLongUserNameThatShouldBeTruncatedInTheUI` → displays as `VeryLongUserNameThatShouldBeTru...`

**Special Characters:**
- `短名` - Chinese characters
- `🎮Gamer42` - Emoji + alphanumeric
- `Player_12345` - Underscore + numbers
- `Anonymous` - Generic fallback name

### Score Distribution
Scores are generated with realistic progression:
- Rank #1: ~5-7 (best score)
- Rank #2-3: ~7-12
- Rank #4-7: ~11-19
- Rank #8-10: ~19-25
- Rank #11+: 25+ (not shown in top 10 by default)

### Date/Time Stamps
Dates span the past 7 days with varied times:
- Recent: Within last 24 hours
- Medium: 2-4 days ago
- Older: 5-7 days ago
- Format: `MM/dd HH:mm` (e.g., "01/08 14:23")

### Solution Availability
- Every **3rd entry** has a complete solution (mock)
- These entries enable the "VIEW" button when selected
- Entries without solutions keep "VIEW" button disabled

## Testing Different Levels

Each level has its own set of mock scores:

### NOT Gate
- 10-15 entries
- Scores range: 5-30

### AND Gate
- 10-15 entries
- Scores range: 5-35

### OR Gate
- 10-15 entries
- Scores range: 5-30

### XOR Gate
- 10-15 entries
- Scores range: 7-32

### NAND Gate
- 10-15 entries
- Scores range: 5-28

### NOR Gate
- 10-15 entries
- Scores range: 6-33

## How to Verify Graphics

### Column Alignment
Check that all columns are properly aligned:
```
Rank    Score    User           Date
#1      5        Alice          01/08 14:23
#2      7        Bob            01/07 09:45
#10     23       Jack           12/30 10:18
```
- Rank column: Right-aligned or centered
- Score column: Numeric, yellow color
- User column: Left-aligned, cyan color
- Date column: Left-aligned, white color

### Text Truncation
Long usernames should show ellipsis (...):
```
✅ Good: Katherine_with_a_really_long...
❌ Bad:  Katherine_with_a_really_long_name_to_test_truncation_overflow
```

### Color Scheme
- **Rank**: White text
- **Score**: Yellow text (highlight for importance)
- **Username**: Cyan text (distinguishes from other columns)
- **Date**: White text
- **Header row**: Bold font

### Touch Targets (Mobile)
Each leaderboard row should:
- Have adequate height for touch input (minimum 44pt/2.0 units)
- Show visual feedback when tapped
- Support selection highlighting

### Selection State
When a row is selected:
- Background changes to highlight color
- Other rows return to normal state
- "VIEW" button enables if solution exists

## Example Console Output

When mock data initializes, you'll see:
```
[EditorLocalStorage] Created storage directory: ...
[EditorLocalStorage] Populating mock leaderboard data for UI testing...
[EditorLocalStorage] Created 72 mock scores across 6 levels for UI testing
[EditorLocalStorage] Local storage initialized successfully
[Leaderboard] Editor mode with local storage enabled - using mock storage
[Leaderboard] Retrieved 10 scores from local storage
```

## Regenerating Mock Data

If you want fresh mock data with different randomization:

**From Unity Menu:**
```
DLS → Mock Data → Regenerate Leaderboard Data
```

**Expected Console Output:**
```
[EditorLocalStorage] Regenerating mock data...
[EditorLocalStorage] Populating mock leaderboard data for UI testing...
[EditorLocalStorage] Created 72 mock scores across 6 levels for UI testing
[EditorLocalStorage] Mock data regenerated successfully
```

**Result:** New random usernames, scores, and timestamps

## Graphics Testing Checklist

When reviewing the leaderboard graphics, verify:

### Layout
- [ ] Table has clear column headers
- [ ] Rows are evenly spaced
- [ ] Background panel has proper margins
- [ ] Scrollbar appears if needed (>10 entries)

### Typography
- [ ] All text is readable on mobile screen
- [ ] Font sizes are appropriate for mobile
- [ ] Bold headers stand out from data rows
- [ ] Line height prevents text overlap

### Colors & Contrast
- [ ] All text has sufficient contrast against background
- [ ] Selection highlight is visible but not distracting
- [ ] Color scheme is consistent with app theme
- [ ] Yellow scores are easily distinguishable

### Truncation & Overflow
- [ ] Long usernames show ellipsis
- [ ] No text overflows column boundaries
- [ ] Truncated text still readable
- [ ] Emojis don't break layout

### Interaction
- [ ] Rows respond to tap/click
- [ ] Only one row selected at a time
- [ ] VIEW button enables/disables correctly
- [ ] CANCEL button always works
- [ ] Scrolling is smooth (if needed)

### Edge Cases
- [ ] Empty leaderboard shows appropriate message
- [ ] Single entry displays correctly
- [ ] Full 10 entries fit without scrolling
- [ ] Very short usernames align properly
- [ ] Very recent dates display correctly

## Mock Data Limitations

The mock data system has these limitations:

1. **Not Persistent Between Editor Sessions**
   - Mock data resets when Unity Editor closes
   - Saved to disk but regenerated if file is deleted

2. **Fixed Level Set**
   - Only generates mock data for 6 common levels
   - Can be extended by editing `EditorLocalStorage.cs`

3. **No Real Firebase Sync**
   - Mock data is local only
   - Won't reflect actual production leaderboard data

4. **Simplified Solution Data**
   - Solutions are mock IDs, not actual playable solutions
   - "VIEW" button won't load actual chip configurations

## Next Steps

After verifying graphics with mock data:
1. Test on actual Android device for true mobile experience
2. Switch to PC build target to test real Firebase integration
3. Submit test scores to Firebase
4. Verify real leaderboard data displays correctly

