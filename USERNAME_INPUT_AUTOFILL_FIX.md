# Username Input Field Autofill Fix

## Problem
When opening the username input popup, the validation message shows "✓ Verified user: Tester01" but the input field is empty instead of being pre-filled with "Tester01".

## Root Cause
**Timing issue** between async profile loading and UI rendering:

1. `Open()` called → Starts `LoadUserProfileAsync()` **asynchronously**
2. `DrawMenu()` called (first frame) → Draws input field
3. `DrawInputField()` initializes input with `_userName` (still empty at this point)
4. Sets `_hasInitializedInput = true` (prevents re-initialization)
5. Profile loads → Sets `_userName = "Tester01"` ✅
6. `DrawMenu()` called again → Input field doesn't update (already initialized) ❌

### The Problem:
```csharp
// In DrawInputField() - runs before profile loads
if (!_hasInitializedInput)
{
    inputState.SetText(_userName, false); // _userName is empty!
    _hasInitializedInput = true; // Marks as initialized
}

// Later, profile loads and sets _userName = "Tester01"
// But input field is already marked as initialized, so it doesn't update
```

## The Fix

Updated `LoadUserProfileAsync()` to **explicitly update the input field** after the profile loads:

```csharp
if (profile != null && !string.IsNullOrEmpty(profile.username))
{
    _userName = profile.username;
    _hasClaimedUsername = true;
    _validationMessage = $"✓ Verified user: {profile.username}";
    
    // NEW: Update the input field with the verified username
    var inputState = Seb.Vis.UI.UI.GetInputFieldState(ID_UserNameInput);
    inputState.SetText(_userName, false);
}
else
{
    LoadSavedUserName();
    
    // NEW: Update the input field with the local username (if any)
    if (!string.IsNullOrEmpty(_userName))
    {
        var inputState = Seb.Vis.UI.UI.GetInputFieldState(ID_UserNameInput);
        inputState.SetText(_userName, false);
    }
}
```

## How It Works Now

### Timeline:
1. ✅ Popup opens → Shows "Loading user profile..."
2. ✅ Input field initializes (might be empty initially)
3. ✅ Profile loads → Sets `_userName = "Tester01"`
4. ✅ **Explicitly updates input field** with "Tester01"
5. ✅ Shows "✓ Verified user: Tester01"
6. ✅ Input field displays "Tester01"

### User Experience:
- Opens popup → brief "Loading..." message
- Input field updates with username (smooth transition)
- Validation message confirms verified user
- **Input field is pre-filled correctly** ✅

## Files Modified
- `Assets/Scripts/Graphics/UI/Menus/UserNameInputPopup.cs`

## Testing
Test both scenarios:

### Scenario 1: Verified User (Has Firebase Profile)
1. Open username input popup
2. Wait for profile to load
3. ✅ Input field shows verified username
4. ✅ Message shows "✓ Verified user: [username]"

### Scenario 2: Unverified User (Local Cache Only)
1. Open username input popup  
2. Wait for profile check to complete
3. ✅ Input field shows cached username (if any)
4. ✅ User can edit or keep it

### Scenario 3: New User (No Profile, No Cache)
1. Open username input popup
2. Wait for profile check to complete
3. ✅ Input field is empty (ready for input)
4. ✅ No error messages

## Why This Approach Works

Instead of relying on the initialization logic in `DrawInputField()` (which runs too early), we **proactively update** the input field when we know the username is ready:

- After Firebase profile loads ✅
- After local cache loads ✅
- Input field state is updated directly ✅
- Works regardless of UI timing ✅

This ensures the input field always reflects the correct username, regardless of when the async operations complete.

