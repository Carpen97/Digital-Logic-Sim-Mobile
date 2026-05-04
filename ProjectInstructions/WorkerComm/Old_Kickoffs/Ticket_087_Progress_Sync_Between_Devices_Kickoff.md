# Ticket 087: Progress / project sync between devices (and sharing) – research

You are working on the Digital Logic Sim Mobile Unity project. This ticket is **first and foremost about finding the possibilities**. The aim is to enable (1) **syncing projects between your own devices** (e.g. computer and phone), and (2) **optionally sharing projects between different users**. The project already uses **Firebase**; we want to extend on that. No implementation yet – this phase is research and options.

**Source:** Community request (myithspa).

---

## Goals (for later implementation)

1. **Sync projects between your own devices**  
   User can have the same projects (and progress) on e.g. PC and phone, with changes syncing across devices.

2. **Optionally: sharing projects between different users**  
   Ability to share a project with another user (link, invite, or similar).

Both should build on the **existing Firebase** setup (auth, Realtime Database or Firestore, etc.) where possible.

---

## What to do in this ticket (research only)

1. **Map current Firebase usage**  
   Identify what the app already uses Firebase for (leaderboards, auth, etc.) and which Firebase products (Realtime Database, Firestore, Auth, Storage). Note any existing auth or user identity (e.g. username/device binding).

2. **Options for syncing projects across a user’s own devices**  
   - How to identify “same user” across devices (existing auth? account? device linking?).  
   - Where to store project data (Firestore collections, Realtime Database, Cloud Storage for blobs).  
   - Sync model: last-write-wins, conflict handling, or simple “upload / download” for v1.  
   - Size limits, cost, and security (who can read/write what).

3. **Options for sharing projects between different users**  
   - Share by link, by username, or by invite.  
   - Read-only vs copy-to-their-account vs collaborative edit (keep v1 simple if needed).  
   - How this fits with the same Firebase structures as (2).

4. **Recommendation**  
   Summarise 2–3 feasible approaches (e.g. “Firestore project docs keyed by userId” + “share tokens in a separate collection”) with pros/cons and a suggested first step for implementation. Call out unknowns (e.g. auth upgrade, quotas).

---

## Deliverable

Write your report to **`ProjectInstructions/WorkerComm/Ticket_087_Progress_Sync_Between_Devices_Report.md`** with:

- **Status:** Done / Blocked  
- **Summary:** 1–2 sentences on what you found.  
- **Current Firebase usage:** What the codebase uses today.  
- **Options for own-device sync:** Possible designs, with pros/cons.  
- **Options for sharing between users:** Possible designs, with pros/cons.  
- **Recommendation:** Preferred approach(es) and suggested next steps.  
- **What’s left:** Any follow-up research or decisions needed.

The PM (or user) will use this to decide how to scope the next phase (implementation ticket or further design).
