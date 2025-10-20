# Firebase Rules for Username Changes

## 🔥 Copy and Paste These Rules into Firebase Console

Go to Firebase Console → Firestore Database → Rules, and paste this:

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    
    // ============================================
    // Helper Functions
    // ============================================
    
    function isSignedIn() {
      return request.auth != null;
    }
    
    function isOwner(userId) {
      return isSignedIn() && request.auth.uid == userId;
    }
    
    function isValidUsername(username) {
      return username.size() >= 3 && 
             username.size() <= 20 && 
             username.matches('^[a-zA-Z0-9 _-]+$');
    }
    
    function isReservedUsername(username) {
      let lowerUsername = username.lower();
      return lowerUsername in ['anonymous', 'guest', 'admin', 'moderator', 'system', 'deleted', 'unknown'];
    }
    
    // ============================================
    // Users Collection - User Profiles
    // ============================================
    
    match /users/{userId} {
      allow read: if true;
      
      allow create: if isSignedIn() && 
                      request.auth.uid == userId &&
                      request.resource.data.userId == request.auth.uid &&
                      isValidUsername(request.resource.data.username) &&
                      !isReservedUsername(request.resource.data.username);
      
      // Allow username changes with history tracking
      allow update: if isOwner(userId) &&
                      (
                        // Normal updates (lastLoginAt, appVersion)
                        request.resource.data.diff(resource.data).affectedKeys().hasOnly(['lastLoginAt', 'appVersion']) ||
                        // Username change (requires previousUsername and usernameChangedAt)
                        (request.resource.data.diff(resource.data).affectedKeys().hasOnly(['username', 'previousUsername', 'usernameChangedAt', 'lastLoginAt']) &&
                         request.resource.data.previousUsername == resource.data.username &&
                         isValidUsername(request.resource.data.username) &&
                         !isReservedUsername(request.resource.data.username))
                      );
      
      allow delete: if false;
    }
    
    // ============================================
    // Usernames Collection - Username Reservation Index
    // ============================================
    
    match /usernames/{username} {
      allow read: if true;
      
      allow create: if isSignedIn() &&
                      request.resource.data.userId == request.auth.uid &&
                      request.resource.data.username == username.lower() &&
                      isValidUsername(username) &&
                      !isReservedUsername(username);
      
      // Allow delete only by owner (for username changes)
      allow delete: if isSignedIn() &&
                      resource.data.userId == request.auth.uid;
      
      allow update: if false;
    }
    
    // ============================================
    // Scores Collection - Leaderboard Submissions
    // ============================================
    
    match /scores/{scoreId} {
      allow read: if true;
      
      allow create: if isSignedIn()
        && request.resource.data.levelId is string
        && request.resource.data.userId == request.auth.uid
        && request.resource.data.score is number
        && request.resource.data.score >= 0
        && (!request.resource.data.keys().hasAny(['isAuthenticated', 'verifiedUsername']) ||
            !request.resource.data.isAuthenticated ||
            !exists(/databases/$(database)/documents/users/$(request.auth.uid)) ||
            request.resource.data.verifiedUsername == get(/databases/$(database)/documents/users/$(request.auth.uid)).data.username)
        && request.resource.data.keys().hasOnly([
            'levelId', 'userId', 'userName', 'score', 'submittedAt', 
            'solutionJsonPath', 'solutionImagePath', 'completeSolutionId',
            'isAuthenticated', 'verifiedUsername', 'deviceId', 'userCreatedAt'
        ]);
      
      // Allow updates ONLY to userName and verifiedUsername fields by the owner
      allow update: if isSignedIn()
        && resource.data.userId == request.auth.uid
        && request.resource.data.diff(resource.data).affectedKeys().hasOnly(['userName', 'verifiedUsername'])
        && request.resource.data.userName is string
        && (!request.resource.data.keys().hasAny(['verifiedUsername']) ||
            request.resource.data.verifiedUsername == get(/databases/$(database)/documents/users/$(request.auth.uid)).data.username);
      
      allow delete: if false;
    }
    
    // ============================================
    // Complete Solutions Collection
    // ============================================
    
    match /completeSolutions/{solutionId} {
      allow read: if true;
      
      allow create: if isSignedIn()
        && request.resource.data.levelId is string
        && request.resource.data.userId == request.auth.uid
        && request.resource.data.userName is string
        && request.resource.data.score is number
        && request.resource.data.score >= 0
        && request.resource.data.submittedAt is string
        && request.resource.data.solutionJson is string
        && request.resource.data.metadata is map
        && request.resource.data.keys().hasOnly([
            'levelId', 'userId', 'userName', 'score', 'submittedAt', 
            'solutionJson', 'metadata', 'solutionImagePath'
        ]);
      
      // Allow updates ONLY to userName field by the owner
      allow update: if isSignedIn()
        && resource.data.userId == request.auth.uid
        && request.resource.data.diff(resource.data).affectedKeys().hasOnly(['userName'])
        && request.resource.data.userName is string;
      
      allow delete: if false;
    }
  }
}
```

## ✅ What Changed

### 1. **Username Changes Allowed**
- Users can now change their username (with 30-day cooldown)
- Old username becomes available for others to claim

### 2. **Leaderboard Updates Allowed**
- Users can update their own leaderboard entries' `userName` and `verifiedUsername` fields
- This allows updating all past scores when username changes

### 3. **Complete Solutions Updates Allowed**
- Users can update their own complete solutions' `userName` field
- Keeps all shared solutions consistent with new username

### 4. **Username Reservation Changes**
- Users can now **delete** their old username reservation
- Required for username change flow (release old → claim new)

### 5. **Rate Limiting in Code**
- 30-day cooldown between username changes (enforced in C# code)
- Prevents username squatting and abuse

## 🚀 After Pasting Rules

1. ✅ Click **"Publish"** in Firebase Console
2. ✅ Test in Rules Playground if desired
3. ✅ Create composite indexes (if not already created):
   - **scores**: `userId` (Ascending) 
   - **completeSolutions**: `userId` (Ascending)

## 🎯 How It Works

When a user changes their username:

1. **Check Rate Limit**: 30 days since last change? ✅
2. **Check Availability**: New username available? ✅
3. **Reserve New Username**: Create new entry in `usernames` collection
4. **Update Profile**: Update `users` document with new username + history
5. **Update Leaderboard**: Update ALL scores for this user (batch operation)
6. **Update Solutions**: Update ALL complete solutions for this user
7. **Release Old Username**: Delete old `usernames` entry

**All Past Leaderboard Entries Updated = One Consistent Identity! 🎉**

---

**Date**: 2025-10-16  
**Status**: Ready to Deploy

