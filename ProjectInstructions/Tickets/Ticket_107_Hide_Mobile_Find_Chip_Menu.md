# Ticket 107 – Hide “Find chip” from mobile bottom bar menu

**Type:** Improvement (mobile UX)  
**Status:** Completed (see report)

## Goal

Remove the **FIND CHIP** row from the **MENU** popup on **Android / iOS** only. Desktop keeps **FIND CHIP** and Ctrl+F behaviour unchanged.

## Context

- Bottom bar menu is defined in `Assets/Scripts/Graphics/UI/Menus/BottomBarUI.cs` (`menuButtonNames`, button indices, `ButtonPressed`).
- Chip search / library behaviour is separate (see Ticket 069 for library search fixes). This ticket only removes the **menu entry** on mobile.

## Success criteria

- [x] Mobile build: MENU popup has no “FIND CHIP” line; remaining actions work and indices match.
- [x] PC / Mac editor and standalone: FIND CHIP still present; `OpenSearchMenu` still reachable from that button.
- [x] Report filed under `WorkerComm/Reports/`.
