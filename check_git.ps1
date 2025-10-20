# Reset everything first
git reset HEAD

# Show what changed files exist
Write-Host "=== Modified files ===" -ForegroundColor Green
git status --short

Write-Host "`n=== Untracked large files ===" -ForegroundColor Yellow
git status --short | Where-Object { $_ -match '^\?\?' }

Write-Host "`n=== Checking for large files ===" -ForegroundColor Red
git ls-files --others --exclude-standard | ForEach-Object {
    $size = (Get-Item $_).Length / 1MB
    if ($size -gt 10) {
        Write-Host "$_ : $([math]::Round($size, 2)) MB"
    }
}

