# 📋 **Chip Library Navigation Specification**
**Ticket 050: Fix chip navigation in ChipLibraryMenu**

---

## **1. OVERVIEW**

This document specifies the behavior of the 6 navigation buttons in the ChipLibraryMenu that allow users to relocate chips, collections, and nested collections within the library structure.

**Terminology:**
- **Element**: A chip, collection, or nested collection
- **Visual Tree**: The hierarchical display order in the Collections Panel
- **Parent Collection**: The collection that contains an element
- **Nesting Level**: Currently limited to 1 level deep (matches BottomBarUI.cs limitation)

---

## **2. BUTTON DEFINITIONS**

### **2.1 MOVE UP**
**Purpose**: Move the selected element one position up within its current container.

**Behavior**:
- **Within same container**: Swap positions with the element directly above
- **At top of container**: Move element into the collection above (as last element)
- **At top of first collection**: Button is **DISABLED** (nowhere to move)

**Examples**:
```
BEFORE:              AFTER MOVE UP (Chip A2):
Collection A         Collection A
  ├─ Chip A1          ├─ Chip A2  ← Swapped
  ├─ Chip A2          ├─ Chip A1  ← Swapped
  └─ Chip A3          └─ Chip A3

BEFORE:              AFTER MOVE UP (Chip B1):
Collection A         Collection A
  └─ Chip A1          ├─ Chip A1
Collection B         └─ Chip B1  ← Moved in as last element
  └─ Chip B1       Collection B
                      (empty)
```

---

### **2.2 MOVE DOWN**
**Purpose**: Move the selected element one position down within its current container.

**Behavior**:
- **Within same container**: Swap positions with the element directly below
- **Before a collection**: Move element into that collection (as first element)
- **At bottom of container AND no collection below**: Button is **DISABLED**

**Examples**:
```
BEFORE:              AFTER MOVE DOWN (Chip A1):
Collection A         Collection A
  ├─ Chip A1          ├─ Chip A2  ← Swapped
  ├─ Chip A2          ├─ Chip A1  ← Swapped
  └─ Chip A3          └─ Chip A3

BEFORE:              AFTER MOVE DOWN (Chip A3):
Collection A         Collection A
  ├─ Chip A1          ├─ Chip A1
  ├─ Chip A2          ├─ Chip A2
  ├─ Chip A3          └─ Nested Collection X
  └─ Nested Coll. X       ├─ Chip A3  ← Moved in as first
      ├─ Chip X1          ├─ Chip X1
      └─ Chip X2          └─ Chip X2
```

---

### **2.3 JUMP UP**
**Purpose**: Move element to the collection directly above the current parent collection.

**Behavior**:
- Element moves to the previous collection in the visual tree
- Element becomes the **last element** of the target collection
- If no collection above: Button is **DISABLED**
- **Auto-expands target collection** to show the moved element

**Examples**:
```
BEFORE:              AFTER JUMP UP (Chip B1):
Collection A         Collection A
  ├─ Chip A1          ├─ Chip A1
  └─ Chip A2          ├─ Chip A2
Collection B         └─ Chip B1  ← Moved as last element (A auto-expands)
  ├─ Chip B1       Collection B
  └─ Chip B2          └─ Chip B2

BEFORE:              AFTER JUMP UP (Nested Coll. Y):
Collection A         Collection A
  └─ Nested Coll. X   ├─ Nested Coll. X
Collection B         └─ Nested Coll. Y  ← Moved as last nested collection
  ├─ Nested Coll. Y  Collection B
  └─ Chip B1          └─ Chip B1
```

---

### **2.4 JUMP DOWN**
**Purpose**: Move element to the collection directly below the current parent collection.

**Behavior**:
- Element moves to the next collection in the visual tree
- Element becomes the **last element** of the target collection
- If no collection below: Button is **DISABLED**
- **Auto-expands target collection** to show the moved element

**Examples**:
```
BEFORE:              AFTER JUMP DOWN (Chip A1):
Collection A         Collection A
  ├─ Chip A1          └─ Chip A2
  └─ Chip A2       Collection B
Collection B         ├─ Chip B1
  └─ Chip B1          └─ Chip A1  ← Moved as last element (B auto-expands)
```

---

### **2.5 NEST** (formerly "Jump In")
**Purpose**: Move element into a collection, making it nested/contained.

**Behavior**:
- **Target**: The first collection found **immediately above** the element in the visual tree
- Element becomes the **last element** inside the target collection
- If no collection above: Button is **DISABLED**
- **Nesting Depth Limit**: Currently 1 level (nested collections cannot nest further)

**Examples**:
```
BEFORE:              AFTER NEST (Chip A3):
Collection A         Collection A
  ├─ Nested Coll. X   ├─ Nested Coll. X
  │   ├─ Chip X1      │   ├─ Chip X1
  │   └─ Chip X2      │   ├─ Chip X2
  ├─ Chip A1          │   └─ Chip A3  ← Moved into X as last element
  ├─ Chip A2          ├─ Chip A1
  └─ Chip A3          └─ Chip A2

BEFORE:              AFTER NEST (Collection B):
Collection A         Collection A
  └─ Chip A1          ├─ Chip A1
Collection B         └─ Nested Coll. B  ← Became nested collection
  └─ Chip B1              └─ Chip B1
```

**Special Case - Multiple Collections Above**:
```
BEFORE:              AFTER NEST (Chip A3):
Collection A         Collection A
  ├─ Nested Coll. X   ├─ Nested Coll. X
  ├─ Nested Coll. Y   ├─ Nested Coll. Y
  │   └─ Chip Y1      │   ├─ Chip Y1
  ├─ Chip A1          │   └─ Chip A3  ← Moved into Y (closest above)
  ├─ Chip A2          ├─ Chip A1
  └─ Chip A3          └─ Chip A2
```

---

### **2.6 UNNEST** (formerly "Jump Out")
**Purpose**: Move element out of its parent container to the same level as the parent.

**Behavior**:
- Element moves out of its parent collection
- Element is placed **directly below** the parent collection (as next sibling)
- **CRITICAL**: Chips must always be inside a collection

**Enable/Disable Rules**:
- **Chips in top-level collections**: Button is **DISABLED** ❌ (chips must remain in a collection)
- **Chips in nested collections**: Button is **ENABLED** ✅ (moves to parent collection)
- **Nested collections**: Button is **ENABLED** ✅ (becomes top-level collection)
- **Top-level collections**: Button is **DISABLED** ❌ (already at top level)

**Examples**:
```
BEFORE:              AFTER UNNEST (Chip X1):
Collection A         Collection A
  ├─ Nested Coll. X   ├─ Nested Coll. X
  │   ├─ Chip X1      │   └─ Chip X2
  │   └─ Chip X2      ├─ Chip X1  ← Moved out, directly below parent
  └─ Chip A1          └─ Chip A1

BEFORE:              AFTER UNNEST (Nested Coll. B):
Collection A         Collection A
  ├─ Nested Coll. B   └─ Chip A1
  │   └─ Chip B1     Collection B  ← Became top-level collection
  └─ Chip A1          └─ Chip B1
```

**Special Case - Chips in Top-Level Collections**:
```
Collection BASIC
  ├─ PULSE  ← UNNEST is DISABLED (chip must stay in collection)
  └─ KEY
```

---

## **3. ENABLE/DISABLE LOGIC**

### **3.1 Condition Matrix**

| Button | Enabled When | Disabled When |
|--------|-------------|---------------|
| **MOVE UP** | Not first element in container | First element in first collection |
| **MOVE DOWN** | Not last element OR has collection below | Last element with no collection below |
| **JUMP UP** | Parent collection has a collection above | Parent is first collection |
| **JUMP DOWN** | Parent collection has a collection below | Parent is last collection |
| **NEST** | Collection exists immediately above in tree | No collection above |
| **UNNEST** | Element is nested (not top-level) | Top-level collection OR chip in top-level collection |

### **3.2 Type-Specific Rules**

**For Chips (in top-level collection)**:
- MOVE UP: Enabled (conditions apply)
- MOVE DOWN: Enabled (conditions apply)
- JUMP UP: Enabled (conditions apply)
- JUMP DOWN: Enabled (conditions apply)
- NEST: Enabled (conditions apply)
- **UNNEST: DISABLED** ❌ (chips must stay in a collection)

**For Chips (in nested collection)**:
- All 6 buttons available (conditions apply)
- **UNNEST: ENABLED** ✅ (moves to parent collection)

**For Nested Collections**:
- MOVE UP: Enabled (conditions apply)
- MOVE DOWN: Enabled (conditions apply)
- JUMP UP: Enabled (conditions apply)
- JUMP DOWN: Enabled (conditions apply)
- **NEST: DISABLED** ❌ (nesting depth limit = 1)
- **UNNEST: ENABLED** ✅ (becomes top-level collection)

**For Top-Level Collections**:
- MOVE UP: Enabled if not first
- MOVE DOWN: Enabled if not last
- JUMP UP: Enabled if not first
- JUMP DOWN: Enabled if not last
- NEST: Enabled if collection above exists
- **UNNEST: DISABLED** ❌ (already at top level)

---

## **4. POST-MOVE REQUIREMENTS**

### **4.1 Selection Persistence**
✅ **The moved element MUST remain selected after the move**
- `selectedCollectionIndex` must be updated
- `selectedChipInCollectionIndex` must be updated
- `selectedNestedCollectionIndex` must be updated
- `selectedChipInNestedCollectionIndex` must be updated

### **4.2 Visual Persistence**
✅ **The moved element MUST remain visible in the Collections Panel**
- If moved into a closed collection: **Auto-expand** that collection
- If moved into a closed nested collection: **Auto-expand** both parent and nested collection
- Scroll position should adjust to keep the element visible (if possible)

### **4.3 Preview Panel**
✅ **The moved element MUST remain displayed in the Preview Panel**
- Header shows the element name
- Chip preview shows the chip design
- Movement buttons remain functional

---

## **5. VISUAL TREE EXAMPLES**

### **5.1 Complete Example Structure**
```
Collection A
  ├─ Nested Collection X
  │   ├─ Chip X1
  │   └─ Chip X2
  ├─ Nested Collection Y
  │   └─ Chip Y1
  ├─ Chip A1
  ├─ Chip A2
  └─ Chip A3
Collection B
  ├─ Chip B1
  └─ Chip B2
Collection C (empty)
```

### **5.2 Button Availability for Each Element**

| Element | Move Up | Move Down | Jump Up | Jump Down | Nest | Unnest |
|---------|---------|-----------|---------|-----------|------|--------|
| **Collection A** | ❌ (first) | ✅ | ❌ (first) | ✅ | ❌ (first) | ❌ (top-level) |
| **Nested Coll. X** | ❌ (first in A) | ✅ | ✅ | ✅ | ❌ (depth limit) | ✅ |
| **Chip X1** | ❌ (first in X) | ✅ | ✅ | ✅ | ❌ (no coll. above) | ✅ |
| **Chip X2** | ✅ | ✅ | ✅ | ✅ | ❌ (no coll. above) | ✅ |
| **Nested Coll. Y** | ✅ | ✅ | ✅ | ✅ | ❌ (depth limit) | ✅ |
| **Chip Y1** | ❌ (first & only) | ❌ (last & only) | ✅ | ✅ | ❌ (no coll. above) | ✅ |
| **Chip A1** | ✅ | ✅ | ✅ | ✅ | ✅ (Nest Y above) | ❌ (top-level chip) |
| **Chip A2** | ✅ | ✅ | ✅ | ✅ | ✅ (Nest Y above) | ❌ (top-level chip) |
| **Chip A3** | ✅ | ✅ | ✅ | ✅ | ✅ (Nest Y above) | ❌ (top-level chip) |
| **Collection B** | ✅ | ✅ | ✅ | ✅ | ✅ (into A) | ❌ (top-level) |
| **Chip B1** | ❌ (first) | ✅ | ✅ | ✅ | ❌ (no coll. above) | ❌ (top-level chip) |
| **Chip B2** | ✅ | ✅ | ✅ | ✅ | ❌ (no coll. above) | ❌ (top-level chip) |
| **Collection C** | ✅ | ❌ (last) | ✅ | ❌ (last) | ✅ (into B) | ❌ (top-level) |

---

## **6. TEST PLAN**

### **6.1 Setup Test Environment**
Create a test project with the following structure:
```
Collection ALPHA
  ├─ Nested Collection DELTA
  │   ├─ Chip D1
  │   └─ Chip D2
  ├─ Chip A1
  ├─ Chip A2
  └─ Chip A3
Collection BETA
  ├─ Chip B1
  └─ Chip B2
Collection GAMMA (empty)
```

---

### **6.2 Test Cases**

#### **TEST 1: Move Up Within Container**
1. Open Chip Library
2. Expand Collection ALPHA
3. Select **Chip A2**
4. Click **MOVE UP**
5. ✅ **Expected**: Chip A2 is now above Chip A1
6. ✅ **Expected**: Chip A2 remains selected (highlighted in blue)
7. ✅ **Expected**: Chip A2 is visible in Collections Panel

---

#### **TEST 2: Move Up Into Collection Above**
1. Open Chip Library
2. Expand Collection BETA
3. Select **Chip B1** (first in BETA)
4. Click **MOVE UP**
5. ✅ **Expected**: Chip B1 moves into Collection ALPHA as last element (after Chip A3)
6. ✅ **Expected**: Chip B1 remains selected
7. ✅ **Expected**: Collection ALPHA is auto-expanded (if it wasn't already)
8. ✅ **Expected**: Chip B1 is visible in Collections Panel

---

#### **TEST 3: Move Down Within Container**
1. Open Chip Library
2. Expand Collection ALPHA
3. Select **Chip A1**
4. Click **MOVE DOWN**
5. ✅ **Expected**: Chip A1 is now below Chip A2
6. ✅ **Expected**: Chip A1 remains selected
7. ✅ **Expected**: Chip A1 is visible

---

#### **TEST 4: Move Down Into Nested Collection**
1. Open Chip Library
2. Expand Collection ALPHA
3. Expand Nested Collection DELTA
4. Select **Chip A1**
5. Move **Chip A1** so it's directly above Nested Collection DELTA
6. Click **MOVE DOWN**
7. ✅ **Expected**: Chip A1 moves into DELTA as first element (before Chip D1)
8. ✅ **Expected**: Chip A1 remains selected
9. ✅ **Expected**: DELTA remains expanded, Chip A1 is visible

---

#### **TEST 5: Jump Up (Chip in Nested Collection)**
1. Open Chip Library
2. Expand Collection ALPHA → Nested Collection DELTA
3. Close Nested Collection DELTA (collapse it)
4. Expand another nested collection in ALPHA (if available)
5. Select **Chip D1** in the other nested collection
6. Click **JUMP UP**
7. ✅ **Expected**: Chip moves to DELTA as last element
8. ✅ **Expected**: Chip remains selected
9. ✅ **Expected**: DELTA **auto-expands** to show the moved chip
10. ✅ **Expected**: Chip is visible

---

#### **TEST 6: Jump Down (Chip in Nested Collection)**
1. Open Chip Library
2. Expand Collection ALPHA → Nested Collection DELTA
3. Create/use another nested collection below DELTA
4. Close the lower nested collection
5. Select **Chip D1** in DELTA
6. Click **JUMP DOWN**
7. ✅ **Expected**: Chip moves to the nested collection below as last element
8. ✅ **Expected**: Chip remains selected
9. ✅ **Expected**: Target nested collection **auto-expands** to show the moved chip
10. ✅ **Expected**: Chip is visible

---

#### **TEST 7: Nest (Chip into Nested Collection)**
1. Open Chip Library
2. Expand Collection ALPHA
3. Expand Nested Collection DELTA
4. Select **Chip A1**
5. Click **NEST**
6. ✅ **Expected**: Chip A1 moves into DELTA as last element (after Chip D2)
7. ✅ **Expected**: Chip A1 remains selected
8. ✅ **Expected**: DELTA remains expanded, Chip A1 is visible

---

#### **TEST 8: Nest (Collection into Collection)**
1. Open Chip Library
2. Select **Collection BETA**
3. Click **NEST**
4. ✅ **Expected**: BETA becomes a nested collection inside ALPHA
5. ✅ **Expected**: BETA remains selected
6. ✅ **Expected**: Collection ALPHA is auto-expanded, BETA is visible as nested collection

---

#### **TEST 9: Unnest (Chip from Nested Collection)**
1. Open Chip Library
2. Expand Collection ALPHA → Nested Collection DELTA
3. Select **Chip D1**
4. Click **UNNEST**
5. ✅ **Expected**: Chip D1 moves out of DELTA, appears directly below DELTA in ALPHA
6. ✅ **Expected**: Chip D1 remains selected
7. ✅ **Expected**: Chip D1 is visible in Collections Panel

---

#### **TEST 10: Unnest (Nested Collection to Top Level)**
1. Open Chip Library
2. Expand Collection ALPHA
3. Select **Nested Collection DELTA**
4. Click **UNNEST**
5. ✅ **Expected**: DELTA becomes a top-level collection, appears directly below Collection ALPHA
6. ✅ **Expected**: DELTA remains selected
7. ✅ **Expected**: DELTA is visible as a top-level collection

---

#### **TEST 11: Unnest DISABLED for Chips in Top-Level Collections** ⭐ **BUG FIX TEST**
1. Open Chip Library
2. Expand Collection ALPHA
3. Select **Chip A1** (a chip directly in ALPHA, not in a nested collection)
4. ✅ **Expected**: UNNEST button is **DISABLED** (grayed out)
5. Try clicking UNNEST
6. ✅ **Expected**: Nothing happens (button is not clickable)
7. Select **Chip B1** in Collection BETA
8. ✅ **Expected**: UNNEST button is **DISABLED**

---

#### **TEST 12: Button Disable States**
1. Open Chip Library
2. Select **Collection ALPHA** (first collection)
3. ✅ **Expected**: MOVE UP is **DISABLED**
4. ✅ **Expected**: JUMP UP is **DISABLED**
5. ✅ **Expected**: UNNEST is **DISABLED**
6. Select **Collection GAMMA** (last collection)
7. ✅ **Expected**: MOVE DOWN is **DISABLED**
8. ✅ **Expected**: JUMP DOWN is **DISABLED**

---

#### **TEST 13: Nested Collection Depth Limit**
1. Open Chip Library
2. Expand Collection ALPHA
3. Select **Nested Collection DELTA**
4. ✅ **Expected**: NEST button is **DISABLED** (depth limit = 1)

---

#### **TEST 14: Auto-Expand Closed Collections (Top-Level)** ⭐ **BUG FIX TEST**
1. Open Chip Library
2. **Collapse** Collection ALPHA
3. Select a chip in Collection BETA
4. Click **JUMP UP** (move to ALPHA)
5. ✅ **Expected**: Collection ALPHA **auto-expands** to show the moved chip
6. ✅ **Expected**: Moved chip is visible and selected

---

#### **TEST 15: Move Sequence (Complex)**
1. Open Chip Library
2. Select **Chip A1** in Collection ALPHA
3. Click **JUMP DOWN** (move to BETA)
4. Click **NEST** (move into nested collection if available, or stay)
5. Click **UNNEST** (move back out)
6. Click **JUMP UP** (move back to ALPHA)
7. ✅ **Expected**: After each move, chip remains selected and visible
8. ✅ **Expected**: No errors or index out of bounds

---

#### **TEST 16: Edge Case - Moving Last Chip in Collection**
1. Open Chip Library
2. Create a collection with only 1 chip
3. Move that chip to another collection
4. ✅ **Expected**: Source collection becomes empty but still exists
5. ✅ **Expected**: No crashes or errors

---

### **6.3 Acceptance Criteria Summary**

✅ All 6 navigation buttons work as specified  
✅ Button enable/disable states are correct  
✅ UNNEST is disabled for chips in top-level collections ⭐ **FIXED**  
✅ JUMP UP/DOWN auto-expand target nested collections ⭐ **FIXED**  
✅ Moved elements remain selected after move  
✅ Moved elements remain visible (auto-expand if needed)  
✅ Preview panel updates correctly  
✅ No index out of bounds errors  
✅ Works on both PC and Mobile  
✅ Nested collection depth limit enforced  
✅ Visual feedback matches actual state  

---

## **7. IMPLEMENTATION NOTES**

### **7.1 Bugs Fixed**

#### **Bug #1: JUMP UP/DOWN didn't auto-expand target nested collection**
**Location**: Lines 840-852 (JUMP UP), Lines 872-884 (JUMP DOWN)
**Fix**: Added `targetNestedCollection.IsToggledOpen = true;` after moving chip

#### **Bug #2: UNNEST enabled for chips in top-level collections**
**Location**: Line 604
**Fix**: Changed `bool canJumpOut = true;` to `bool canJumpOut = false;`
**Reasoning**: Chips must always remain inside a collection (architectural constraint)

### **7.2 Key Methods Modified**
- Updated JUMP OUT enable/disable logic in `DrawSelectedItemPanel()` (line 604)
- Added auto-expand logic for nested JUMP UP (line 849)
- Added auto-expand logic for nested JUMP DOWN (line 881)

### **7.3 Related Code References**
- Movement logic: Lines 624-997 in `ChipLibraryMenu.cs`
- Selection state variables: Lines 56-60
- BottomBarUI nesting reference: `Assets/Scripts/Graphics/UI/Menus/BottomBarUI.cs`

---

## **8. CHANGE LOG**

**Version 1.1** - 2025-10-11
- Fixed Bug #1: JUMP UP/DOWN now auto-expand target nested collections
- Fixed Bug #2: UNNEST disabled for chips in top-level collections
- Updated enable/disable logic in Section 3
- Added bug fix test cases (TEST 11, TEST 14)
- Clarified UNNEST behavior with architectural constraint

**Version 1.0** - 2025-10-11
- Initial specification document
- Defined all 6 button behaviors
- Created comprehensive test plan

---

**Document Version**: 1.1  
**Date**: 2025-10-11  
**Status**: ✅ **Implemented & Fixed**

