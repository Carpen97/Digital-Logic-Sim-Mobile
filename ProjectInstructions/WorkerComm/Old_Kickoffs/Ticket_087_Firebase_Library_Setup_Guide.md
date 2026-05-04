# Ticket 087: Firebase Library Setup Guide

**Purpose:** Add Firestore rules and structure for the Project Sharing Library.  
**Do this in Firebase Console** before or while the app code is being developed.

---

## Step 1: Add Library Rules to Firestore

1. Go to [Firebase Console](https://console.firebase.google.com/) → select project **dlsmobile-22657**
2. Open **Firestore Database** → **Rules** tab
3. Find the closing `}` of the last `match` block (before the final `}` of the `documents` block)
4. **Add** the following block (do not remove your existing rules for users, usernames, scores, completeSolutions):

```javascript
    // ============================================
    // Library Collection - Shared Projects (Ticket 087)
    // ============================================
    
    match /library/{projectId} {
      // Anyone can read public projects; owners can read their own (public or private)
      allow read: if resource.data.isPublic == true
        || (request.auth != null && resource.data.ownerUserId == request.auth.uid);
      
      // Only authenticated users can create; must set self as owner
      allow create: if request.auth != null
        && request.resource.data.ownerUserId == request.auth.uid
        && request.resource.data.projectName is string
        && request.resource.data.displayName is string
        && request.resource.data.isPublic is bool
        && request.resource.data.createdAt is string
        && request.resource.data.projectData is string;
      
      // Only owner can update or delete
      allow update, delete: if request.auth != null
        && resource.data.ownerUserId == request.auth.uid;
    }
```

5. Click **Publish**

---

## Step 2: Create Composite Index (when prompted)

The app queries `library` with `where isPublic == true` and `orderBy createdAt descending`. When you first run a browse/import, Firestore may show an error with a **link to create the index**. Click it and create the index.

**Or create manually:**
- Collection: `library`
- Fields: `isPublic` (Ascending), `createdAt` (Descending)
- Query scope: Collection

---

## Document Schema (Reference)

Each `library/{projectId}` document will have:

| Field | Type | Description |
|-------|------|-------------|
| `projectName` | string | Name of the project |
| `displayName` | string | "Anonymous" or username for display |
| `ownerUserId` | string | Firebase Auth UID |
| `isPublic` | boolean | true = shown in Library browse; false = owner only |
| `createdAt` | string | ISO 8601 timestamp |
| `updatedAt` | string | ISO 8601 timestamp |
| `projectData` | string | Full project JSON (chips, description, level progress) |

---

## Step 3: Enable Email/Password Auth (optional, for Create Account / Login)

To use Create Account and Login in Project Sharing:

1. Go to **Firebase Console** → **Authentication** → **Sign-in method**
2. Enable **Email/Password** provider
3. Save

---

---

## Addendum: Updates for Download Tracking, My Projects, Compression

If you've added download counting, "My projects" filter, sort-by-downloads, or project compression, apply these changes in Firebase Console.

### 1. Update Firestore Security Rules (Required)

The app now **updates** library documents when a user imports a project (to record unique downloads). The current rule only allows the **owner** to update. You must allow **any authenticated user** to update *only* the `downloadCount` and `downloadedBy` fields when they import.

**Replace** the library `match` block with:

```javascript
    match /library/{projectId} {
      // Anyone can read public projects; owners can read their own (public or private)
      allow read: if resource.data.isPublic == true
        || (request.auth != null && resource.data.ownerUserId == request.auth.uid);
      
      // Only authenticated users can create; must set self as owner
      allow create: if request.auth != null
        && request.resource.data.ownerUserId == request.auth.uid
        && request.resource.data.projectName is string
        && request.resource.data.displayName is string
        && request.resource.data.isPublic is bool
        && request.resource.data.createdAt is string
        && request.resource.data.projectData is string;
      
      // Owner can fully update/delete; any authenticated user can update only downloadCount + downloadedBy (for import tracking)
      allow update: if request.auth != null && (
        resource.data.ownerUserId == request.auth.uid
        || request.resource.data.diff(resource.data).affectedKeys().hasOnly(['downloadCount', 'downloadedBy'])
      );
      allow delete: if request.auth != null
        && resource.data.ownerUserId == request.auth.uid;
    }
```

### 2. Create Composite Indexes (When Prompted)

When you first use **"My projects"** or **"Most downloads"** in the Import list, Firestore may show an error with a link to create the index. Click it to auto-create.

**Or create manually** in Firestore → Indexes:

| Collection | Fields | Query scope |
|------------|--------|-------------|
| `library` | `ownerUserId` Ascending, `createdAt` Descending | Collection |
| `library` | `ownerUserId` Ascending, `downloadCount` Descending | Collection |
| `library` | `ownerUserId` Ascending, `projectName` Ascending | Collection |
| `library` | `isPublic` Ascending, `downloadCount` Descending | Collection |

*(The original `isPublic` + `createdAt` index may already exist.)*

**Note:** The `ownerUserId` + `projectName` index is needed for **GetExistingEntryByProjectNameAsync** (used when re-uploading/syncing to find and update an existing entry).

### 3. No Other Changes

- **Compression**: Stored as part of `projectData`; no Firebase config needed.
- **New document fields** (`downloadCount`, `downloadedBy`, `projectDataCompressed`): Firestore accepts them automatically.

---

### Addendum: My Projects, Edit/Delete, Sync/Update

The app now supports **My Projects** (list/edit/delete/sync). Existing rules already allow owner update and delete; no rule changes are needed.

**Composite index** for sync/update: `ownerUserId` + `projectName` (see table above). Create it when Firestore prompts, or manually.

---

---

### Editor Testing (No Firebase)

When running in the Unity Editor, Firebase is disabled to prevent crashes. **My Projects** and **Import** show **dummy data** so you can test the UI layout and flows (select rows, Edit/Delete/Sync buttons, filter wheel). Edit/Delete/Sync on dummy entries have no real effect. Test real Firebase features on a built executable or device.

---

### Troubleshooting: Windows PC Crash (uWS::HttpSocket::upgrade)

If the PC build crashes on launch or restart with a stack trace in `FirebaseCppApp` / `uWS::HttpSocket::upgrade`:

1. **The app now auto-clears** `%LOCALAPPDATA%\firestore` and `%LOCALAPPDATA%\firebase-heartbeat` on Windows startup to avoid corrupted-cache crashes.
2. **Update Visual C++ Redistributable**: Install the [latest VC++ 2015-2022 Redistributable (x64)](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170).
3. If it still crashes, test on Android/iOS; the issue is known on some Windows configs ([firebase-unity-sdk#1291](https://github.com/firebase/firebase-unity-sdk/issues/1291)).

---

**Related:** [Ticket 087 Plan](Ticket_087_Progress_Sync_Between_Devices_Plan.md)
