Status: Done

Summary: Enlarged the Project Sharing list popups in `MainMenu.cs` so upload/import/my-projects use a wider content region and taller list areas. Added dedicated constants for list popup sizing to avoid unintended changes to other, smaller Project Sharing dialogs.

What I did:
- Updated `Assets/Scripts/Graphics/UI/Menus/MainMenu.cs` with named Project Sharing list popup constants based on a 0.2-0.8 content span (`0.6f` width fraction).
- Increased the upload list popup scroll area and aligned its action button row to the new list width.
- Increased both import and my-projects scroll areas, kept filter rows, and aligned filter/button row widths with the widened list region.
- Left existing generic popup constants in place for non-list dialogs (login, edit entry, username dialogs) to reduce regression risk.
