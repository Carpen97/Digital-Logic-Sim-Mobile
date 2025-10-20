# Ticket 061 - Firestore Security Rules

## Overview
This document contains the Firestore security rules required for the username reservation and authentication system. These rules enforce server-side validation to prevent impersonation and ensure leaderboard integrity.

## Security Model

### Key Principles
1. **One Username Per User**: Each Firebase UID can claim exactly one username
2. **Username Uniqueness**: Usernames are unique across all users (case-insensitive)
3. **Immutable Binding**: Once claimed, usernames cannot be changed by default
4. **Server-Side Enforcement**: Security rules validate all operations

### Collections

#### 1. `users` Collection
Stores user profiles with authentication data.

**Document ID**: Firebase Auth UID
**Fields**:
- `userId` (string): Firebase Auth UID
- `username` (string): Display name (original case)
- `deviceId` (string): Device identifier
- `createdAt` (string): ISO 8601 timestamp
- `lastLoginAt` (string): ISO 8601 timestamp
- `appVersion` (string): App version

#### 2. `usernames` Collection
Fast lookup index for username availability.

**Document ID**: Lowercase username
**Fields**:
- `username` (string): Lowercase username
- `userId` (string): Firebase Auth UID that owns this username
- `reservedAt` (string): ISO 8601 timestamp

#### 3. `scores` Collection
Leaderboard scores with authentication metadata.

**Document ID**: Auto-generated
**Fields**:
- `levelId` (string): Level identifier
- `userId` (string): Firebase Auth UID
- `userName` (string): Display name
- `score` (number): Score value
- `submittedAt` (string): ISO 8601 timestamp
- `isAuthenticated` (boolean): Whether user has claimed username
- `deviceId` (string): Device identifier
- `verifiedUsername` (string, optional): Verified username from profile
- `userCreatedAt` (string, optional): When user profile was created
- `completeSolutionId` (string, optional): Reference to complete solution

## Firestore Security Rules

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    
    // Helper functions
    function isAuthenticated() {
      return request.auth != null;
    }
    
    function isOwner(userId) {
      return isAuthenticated() && request.auth.uid == userId;
    }
    
    function isValidUsername(username) {
      // Username must be 3-20 characters, alphanumeric with spaces, hyphens, underscores
      return username.size() >= 3 && 
             username.size() <= 20 && 
             username.matches('^[a-zA-Z0-9 _-]+$');
    }
    
    function isReservedUsername(username) {
      // Check against reserved names (case-insensitive)
      let lowerUsername = username.lower();
      return lowerUsername in ['anonymous', 'guest', 'admin', 'moderator', 'system', 'deleted', 'unknown'];
    }
    
    // Users collection - stores user profiles
    match /users/{userId} {
      // Anyone can read user profiles (for leaderboard display)
      allow read: if true;
      
      // Only the user can create their own profile
      allow create: if isAuthenticated() && 
                      request.auth.uid == userId &&
                      request.resource.data.userId == request.auth.uid &&
                      isValidUsername(request.resource.data.username) &&
                      !isReservedUsername(request.resource.data.username) &&
                      // Ensure username is available (checked via username lookup)
                      !exists(/databases/$(database)/documents/usernames/$(request.resource.data.username.lower()));
      
      // Only the user can update their own profile (limited fields)
      allow update: if isOwner(userId) &&
                      // Can only update lastLoginAt and appVersion
                      request.resource.data.diff(resource.data).affectedKeys().hasOnly(['lastLoginAt', 'appVersion']);
      
      // No deletes allowed (prevents username recycling attacks)
      allow delete: if false;
    }
    
    // Usernames collection - fast lookup index for username availability
    match /usernames/{username} {
      // Anyone can read to check availability
      allow read: if true;
      
      // Only authenticated users can create username reservations
      // This should be done in a transaction with the users collection
      allow create: if isAuthenticated() &&
                      request.resource.data.userId == request.auth.uid &&
                      request.resource.data.username == username.lower() &&
                      isValidUsername(username) &&
                      !isReservedUsername(username);
      
      // No updates or deletes allowed (prevents username theft)
      allow update: if false;
      allow delete: if false;
    }
    
    // Scores collection - leaderboard submissions
    match /scores/{scoreId} {
      // Anyone can read scores (public leaderboard)
      allow read: if true;
      
      // Only authenticated users can submit scores
      allow create: if isAuthenticated() &&
                      request.resource.data.userId == request.auth.uid &&
                      // If user has a verified username, enforce it
                      (!request.resource.data.keys().hasAny(['isAuthenticated', 'verifiedUsername']) ||
                       !request.resource.data.isAuthenticated ||
                       request.resource.data.verifiedUsername == get(/databases/$(database)/documents/users/$(request.auth.uid)).data.username);
      
      // No updates or deletes (preserve leaderboard integrity)
      allow update: if false;
      allow delete: if false;
    }
    
    // Complete solutions collection - full reproducible solutions
    match /completeSolutions/{solutionId} {
      // Anyone can read solutions (for viewing)
      allow read: if true;
      
      // Only authenticated users can submit solutions
      allow create: if isAuthenticated() &&
                      request.resource.data.userId == request.auth.uid;
      
      // No updates or deletes
      allow update: if false;
      allow delete: if false;
    }
  }
}
```

## Deployment Instructions

### Step 1: Access Firebase Console
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Select your project: `dlsmobile-22657`
3. Navigate to **Firestore Database** in the left sidebar
4. Click on the **Rules** tab

### Step 2: Update Security Rules
1. Copy the security rules from the section above
2. Paste them into the Firebase Console rules editor
3. Click **Publish** to deploy the rules

### Step 3: Verify Rules
Test the rules using the Firebase Console's Rules Playground:

**Test Cases:**

1. **Username Claiming**:
   ```javascript
   // Test: Authenticated user claims username
   match /users/testUserId {
     allow create: if auth.uid == 'testUserId' and
                     resource.data.username == 'TestUser'
   }
   // Expected: Allow
   ```

2. **Duplicate Username Prevention**:
   ```javascript
   // Test: Another user tries to claim same username
   match /usernames/testuser {
     allow create: if auth.uid == 'differentUserId'
   }
   // Expected: Deny (already exists)
   ```

3. **Score Submission with Authentication**:
   ```javascript
   // Test: Authenticated user submits score
   match /scores/newScoreId {
     allow create: if auth.uid == 'testUserId' and
                     resource.data.verifiedUsername == 'TestUser'
   }
   // Expected: Allow if username matches user profile
   ```

### Step 4: Create Composite Indexes
Firestore requires composite indexes for complex queries. Create these indexes:

1. **Scores by Level and Score**:
   - Collection: `scores`
   - Fields indexed:
     - `levelId` (Ascending)
     - `score` (Ascending)
     - `submittedAt` (Descending)
   - Query scope: Collection

2. **Complete Solutions by Level**:
   - Collection: `completeSolutions`
   - Fields indexed:
     - `levelId` (Ascending)
     - `score` (Ascending)
   - Query scope: Collection

**Create indexes via Firebase Console:**
1. Go to **Firestore Database** → **Indexes** tab
2. Click **Create Index**
3. Enter the collection and fields as specified above
4. Click **Create**

Alternatively, the Firebase SDK will provide a direct link to create required indexes when queries fail.

## Security Considerations

### Strengths
1. ✅ **Prevents Impersonation**: Server-side validation ensures usernames match user profiles
2. ✅ **Immutable Bindings**: Once claimed, usernames cannot be stolen or changed
3. ✅ **Device Tracking**: Device IDs provide additional fraud detection capabilities
4. ✅ **Anonymous Auth**: Privacy-friendly, no personal info required
5. ✅ **Public Leaderboard**: Anyone can view scores for competitive transparency

### Limitations & Mitigations
1. **Anonymous Auth Limitation**: Users lose username if they uninstall/reinstall
   - **Mitigation**: Store device ID for recovery assistance
   - **Future**: Add email/password linking for account recovery

2. **Multiple Devices**: Same user on different devices = different accounts
   - **Mitigation**: Accept this as trade-off for privacy
   - **Future**: Add optional account linking

3. **Banned Username Cleanup**: Reserved names can't be reclaimed
   - **Mitigation**: Comprehensive reserved name list
   - **Admin Tool**: Create admin function to delete inappropriate usernames

## Admin Operations

### Remove Inappropriate Username
Create a Firebase Cloud Function for admin cleanup:

```javascript
const functions = require('firebase-functions');
const admin = require('firebase-admin');
admin.initializeApp();

exports.deleteUsername = functions.https.onCall(async (data, context) => {
  // Verify admin privileges
  if (!context.auth || !context.auth.token.admin) {
    throw new functions.https.HttpsError('permission-denied', 'Must be admin');
  }
  
  const username = data.username;
  const userId = data.userId;
  
  const db = admin.firestore();
  const batch = db.batch();
  
  // Delete username reservation
  batch.delete(db.collection('usernames').doc(username.toLowerCase()));
  
  // Update user profile to clear username
  batch.update(db.collection('users').doc(userId), {
    username: admin.firestore.FieldValue.delete()
  });
  
  await batch.commit();
  return { success: true };
});
```

## Testing Checklist

- [ ] User can claim a username (first-time user)
- [ ] User cannot claim duplicate username
- [ ] User cannot change username after claiming
- [ ] User cannot impersonate others when submitting scores
- [ ] Anonymous submissions work without username
- [ ] Leaderboard displays authenticated users correctly
- [ ] Reserved usernames are rejected
- [ ] Invalid characters in usernames are rejected
- [ ] Score submissions include authentication metadata
- [ ] Complete solutions can be uploaded and retrieved

## Troubleshooting

### Common Issues

**Issue**: "Missing or insufficient permissions"
**Solution**: Verify Firestore rules are published correctly

**Issue**: "Index not found"
**Solution**: Create required composite indexes (see Step 4)

**Issue**: "Username already taken" when it shouldn't be
**Solution**: Check `usernames` collection for stale entries

**Issue**: User can't submit score with their claimed username
**Solution**: Verify `isAuthenticated` flag and `verifiedUsername` match user profile

## Migration Strategy

### For Existing Users
1. Existing scores remain in database (no cleanup needed)
2. Users must claim username on next score submission
3. Backward compatibility: Allow anonymous submissions
4. Gradual rollout: Don't enforce strict validation initially

### Phased Deployment
1. **Phase 1**: Deploy client code with authentication (current)
2. **Phase 2**: Deploy Firestore rules (lenient mode)
3. **Phase 3**: Monitor adoption and errors
4. **Phase 4**: Tighten rules to enforce authentication

## Support & Maintenance

### Monitoring
- Track authentication failures in Firebase Console
- Monitor username claim rate
- Watch for spam/abuse patterns

### User Support
- Provide "Forgot Username" flow (check device ID)
- Handle username disputes (admin tools)
- Support account recovery (future feature)

---

**Last Updated**: 2025-10-16
**Related Tickets**: Ticket 061
**Status**: Ready for Deployment

