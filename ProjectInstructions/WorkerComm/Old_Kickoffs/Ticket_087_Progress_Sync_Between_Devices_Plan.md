# Ticket 087: Progress Sync Between Devices – Implementation Plan

**Ticket:** 087  
**Status:** Planning  
**Last updated:** 2025-02-24  
**Related:** [Kickoff](Ticket_087_Progress_Sync_Between_Devices_Kickoff.md) | [Research Report](Ticket_087_Progress_Sync_Between_Devices_Report.md)

---

## Overview

Add a **Project Sharing** feature that lets users upload their projects to a shared Firebase-based Library and import projects from other users. Users can work with or without an account; anonymous users can use the Library but cannot sync across their own devices.

---

## Navigation Structure

```
Main Menu
  └── Project Sharing (button)
        └── Project Sharing screen
```

**Double-nested:** One tap from Main Menu to Project Sharing.

---

## Entry: First Time in Project Sharing

When the user taps **Project Sharing** and is not yet signed in, they see three options:

1. **Create Account** – Opens Create Account view/menu  
2. **Login** – Opens Login view/menu  
3. **Sign in as Guest** (or "Continue as Anonymous") – Signs in anonymously; goes straight to Project Sharing content  

The user can later **log out** of anonymous and switch to Create Account or Login.

---

## Authentication States

| State | Options |
|-------|---------|
| **Not signed in** | Create Account, Login, Sign in as Guest |
| **Signed in (anonymous)** | Access Library (upload/download); can Log Out to return to the three options |
| **Signed in (account)** | Access Library; can sync across devices (future); can Log Out |

---

## Create Account & Login Views

- **Create Account** – Separate menu/view for account creation (email, password, etc.)  
- **Login** – Separate menu/view for signing into existing account  

Exact placement and layout to be decided during implementation.

---

## Project Sharing Screen Content (when signed in)

- **Export (Upload)** – List of user's local projects; option to upload each to the Library  
- **Import (Download)** – Browse/search the Library; select a project to import locally  
- **Log Out** – Sign out and return to the auth choice screen  

---

## The Library

- Firebase-backed storage for shared projects  
- Same source for both uploads and imports  
- Anonymous uploads appear as "Anonymous" in the Library  
- Database structure and Firestore schema to be defined during implementation  

---

## Scope Notes

- **Reuse existing UI** – Leverage patterns from `UserNameInputPopup` and other popups where possible  
- **Keep it simple** – One flow for anonymous and account users; don't overcomplicate for the anonymous case  
- **Future (out of scope for v1):** Friends, sorting by friends' projects, cross-device sync  

---

## Implementation Phases (to be refined)

1. **Auth & UI shell** – Main Menu button, Project Sharing screen, Create Account / Login / Sign in as Guest views  
2. **Library backend** – Firebase collection(s) and security rules  
3. **Export** – Upload local projects to the Library  
4. **Import** – Browse and import projects from the Library (with search/filter when needed)  
