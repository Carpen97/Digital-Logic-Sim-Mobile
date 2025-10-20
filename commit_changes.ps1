# Reset staging area completely
git reset HEAD

# Only add the specific files we actually modified
git add Assets/Scripts/LevelsIntegration/LevelManager.cs
git add Assets/Scripts/SaveSystem/Saver.cs
git add Assets/Scripts/Game/Main/Main.cs
git add Assets/Scripts/Graphics/UI/Menus/LevelsMenu.cs
git add Assets/Scripts/Graphics/UI/Menus/LevelValidationPopup.cs

Write-Host "=== Files staged for commit ===" -ForegroundColor Green
git status --short

Write-Host "`nPress Enter to commit and push, or Ctrl+C to cancel..."
Read-Host

git commit -m "Fix level progress features and iOS project creation bug

- Restore level progress tracking (dots on levels with saved progress)
- Restore Continue/Restart buttons for levels with progress
- Add Restart and Next buttons to LevelValidationPopup
- Fix Next button to properly navigate to next level in chapter
- Fix level progress loading using reflection to avoid temp chips
- Fix unsaved changes detection for levels
- Make completed levels show Continue/Restart buttons
- Fix critical NullReferenceException in HasUnsavedChanges causing iOS project creation to fail
- Add comprehensive error handling and logging for project creation
- Fix Debug ambiguous reference compilation errors"

git push

Write-Host "`nDone! Press Enter to exit..."
Read-Host

