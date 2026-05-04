# Ticket 087: Progress Sync Between Devices – Research Report

**Status:** Done  
**Date:** 2025-02-24

---

## Summary

The app already uses Firebase (Auth, Firestore) for leaderboards, usernames, scores, and solution sharing. Project data and level progress are **fully local** (filesystem). To sync across devices, you need: (1) **account upgrade** from anonymous auth to a persistent identity (email/password or provider linking) so the same user is recognised on all devices, and (2) **project/progress upload** to Firestore or Cloud Storage. Firebase Storage is included in the project but currently unused; it is suitable for larger project blobs if they exceed Firestore’s 1MB document limit. Sharing can be built on top of the same structures using share tokens and optional read-only access.

---

## Current Firebase Usage

### Products in Use

| Product | Status | Purpose |
|--------|--------|---------|
| **Firebase Auth** | ✅ In use | Anonymous authentication (`SignInAnonymouslyAsync`) |
| **Firestore** | ✅ In use | Leaderboards, user profiles, usernames, complete solutions |
| **Firebase Storage** | ⚠️ In project, not used | No code references |
| **Realtime Database** | ❌ Not used | Not in project |

### Firestore Collections (from `FIREBASE_DATA_RESET_GUIDE.md` and code)

1. **`users/{userId}`** – User profiles (username, deviceId, timestamps)  
2. **`usernames/{lowercaseUsername}`** – Username reservation index  
3. **`scores/{scoreId}`** – Leaderboard entries (levelId, userId, userName, score, etc.)  
4. **`completeSolutions/{solutionId}`** – Shared solutions with full JSON (`solutionJson`), metadata, optional screenshot path  

### Authentication & User Identity

- **Current model:** Anonymous auth, per-device UID.  
- **Username system:** Optional; claimed via `UserAuthService` and tied to `userId`.  
- **Known limitation (Ticket 061):**  
  > "Multiple Devices: Same user on different devices = different accounts"  
  Same person on PC and phone = two different Firebase UIDs, so no cross-device sync with current auth.

### What Is Not in Firebase (Local Only)

- **Projects:** Stored in `{persistentDataPath}/Projects/{projectName}/`:
  - `ProjectDescription.json`
  - `Chips/*.json` (chip definitions)
  - `Meta/levels_progress.json` (per-level completion, stars, best NAND count, in-progress state)
- **Import/Export:** Manual zip export/import via `AndroidIO` (`ExportProjectToZip`, `ImportProjectFromZip`)  
- **App settings:** `AppSettings.json` in `AllData`  
- **Level progress:** `LevelProgressService` – JSON file per project  

---

## Options for Own-Device Sync

### A. Same User Across Devices

**Issue:** Anonymous auth creates a new UID per device.

**Approach:** Add a persistent sign-in method and **link** the anonymous account so the UID is preserved.

1. **Email/password linking**
   - Enable Firebase Email/Password provider.
   - User signs up or signs in with email/password.
   - Call `linkWithCredential()` on the current anonymous user to upgrade.
   - Same UID on all devices where they sign in with that email/password.

2. **Google / Apple sign-in**
   - Enable Google and/or Apple Auth.
   - Same linking pattern as above.
   - Often preferred on mobile.

3. **Optional link prompt**
   - After anonymous usage, prompt “Save progress across devices?” → create account / link.
   - Keeps current flow for users who never sign in.

### B. Where to Store Project Data

| Option | Pros | Cons |
|--------|-----|-----|
| **Firestore documents** | Same stack as leaderboards, easy queries, offline | 1 MB document limit; large projects may not fit |
| **Firebase Storage** | No doc size limit, good for blobs | Separate API and security model |
| **Hybrid** | Metadata in Firestore, blobs in Storage | More implementation work |

**Suggested structure (Firestore-first):**

```
users/{userId}
  (existing)

userProjects/{userId}/projects/{projectId}
  - projectName
  - lastModified (ISO)
  - projectDescriptionJson (small)
  - chipsJson (array or subcollection) – if total ≤ ~1MB
  - levelsProgressJson
  
  OR for larger projects:
  - projectZipPath (gs://bucket/users/{userId}/projects/{projectId}.zip)
```

For projects that exceed ~1 MB, store them in Cloud Storage and keep only metadata + index in Firestore.

### C. Sync Model for v1

| Model | Complexity | Fit |
|-------|------------|-----|
| **Upload / download** | Low | Simple “cloud backup” and “restore on new device” |
| **Last-write-wins** | Medium | Overwrites remote on each save; conflicts possible |
| **Conflict resolution** | High | Needs timestamps, merge logic, or UI to choose version |

**Recommendation for v1:** Simple **upload / download**, with “auto-upload on save” and “pull latest” on launch. Avoid real-time sync and merge logic initially.

### D. Size, Cost, Security

- **Firestore:** 1 MB per document; free tier ~1 GiB storage.  
- **Storage:** Pay-as-you-go; suitable for large project zips.  
- **Security:** Extend existing Firestore rules to restrict `userProjects/{userId}/...` to `request.auth.uid == userId`.

---

## Options for Sharing Between Users

### A. Share Mechanisms

| Mechanism | UX | Complexity |
|-----------|----|------------|
| **Share link** | `dls://share/{token}` or `https://...` | Medium; need token generation and lookup |
| **Username lookup** | “Share with @username” | Simpler; reuses `users` collection |
| **Invite code** | Short code to join/copy | Medium |

**Recommendation:** Start with share links (or short codes) pointing to a share token document.

### B. Share Behaviour

| Option | Description | Complexity |
|--------|-------------|------------|
| **Read-only** | Recipient can view/load, not edit | Low |
| **Copy to account** | Recipient gets their own copy; edit independently | Low |
| **Collaborative edit** | Real-time or async co-editing | High |

**Recommendation for v1:** **Copy to account** – recipient imports a copy into their own projects, with optional read-only preview first.

### C. Firestore Shape for Sharing

```
shares/{shareToken}
  - projectId (or projectSnapshot)
  - ownerUserId
  - createdAt
  - expiresAt (optional)
  - accessCount (optional)

  OR: sharedProjects/{shareToken}
  - Same fields
  - projectData or projectStoragePath
```

Security rule example:

```
match /shares/{shareToken} {
  allow read: if true;  // Anyone with link can read
  allow create: if request.auth != null && 
                   request.resource.data.ownerUserId == request.auth.uid;
  allow update, delete: if request.auth != null && 
                           resource.data.ownerUserId == request.auth.uid;
}
```

---

## Recommendation

### Approach 1: Firestore-Only (Small Projects)

- **Auth:** Add optional email/password (or Google/Apple) and link anonymous account for cross-device identity.
- **Storage:** New collection `userProjects/{userId}/projects/{projectId}`.
- **Schema:** One document per project with `projectDescriptionJson`, `chipsJson`, `levelsProgressJson`.
- **Sync:** Upload on save; download on app start / “Restore” action.

**Pros:** Single backend, simple rules.  
**Cons:** Limited by 1 MB per project; complex projects may need to split into subcollections or move to Storage.

### Approach 2: Firestore + Cloud Storage (All Projects)

- Same auth as above.
- **Metadata in Firestore:** `userProjects/{userId}/projects/{projectId}` with `projectName`, `lastModified`, `sizeBytes`, etc.
- **Blob in Storage:** `users/{userId}/projects/{projectId}.zip` – full project zip (same format as current export).

**Pros:** No size ceiling, reuses existing zip export/import logic.  
**Cons:** Two services (Firestore + Storage), slightly more integration work.

### Approach 3: Sharing Layer

- Add `shares/{shareToken}` with `projectStoragePath`, `ownerUserId`, `createdAt`.
- Generate token (e.g. nanoid or UUID) when user taps “Share project”.
- Recipient: “Open shared project” → fetch from Storage using token → import as new local project (copy-to-account).

---

## Suggested First Steps

1. **Auth upgrade**
   - Enable Email/Password (and optionally Google) in Firebase Console.
   - Implement `linkWithCredential()` for upgrading anonymous users.
   - Add simple “Create account” / “Sign in” UI for users who want cross-device sync.

2. **Project sync (v1)**
   - Implement “Backup to cloud” / “Restore from cloud” using `userProjects` in Firestore (or Storage if you expect large projects).
   - Call upload on project save (and/or explicit “Backup” button).
   - On app start, offer “Restore from cloud” if no local projects or user explicitly requests it.

3. **Sharing (v2)**
   - Add `shares` collection and Cloud Storage path for shared project blobs.
   - Implement “Share” flow that creates a token and optionally short link.
   - Implement “Open shared link” that fetches and imports as a new project.

---

## What’s Left

| Item | Notes |
|------|-------|
| **Auth provider choice** | Email/password vs Google/Apple vs both – product decision |
| **Project size distribution** | Measure typical project zips to decide Firestore-only vs Storage |
| **Conflict handling** | For future: last-modified timestamps, “cloud vs local” comparison UI |
| **Quotas & billing** | Monitor Firestore + Storage usage; consider alerts for production |
| **Offline behaviour** | Define behaviour when offline (queue uploads, show cached, etc.) |

---

*Report generated for Digital Logic Sim Mobile – Ticket 087 research phase.*
