# Run this script from the repository root in PowerShell to remove deleted SignalR files from git
# Review the list and confirm before running (y/n)
$paths = @(
    'src/DotNetApp.Server/Hubs/GameHub.cs',
    'src/DotNetApp.Client/Services/GameHubClient.cs',
    'tests/DotNetApp.Server.Tests.Integration.GameHub/GameHubTests.cs'
)
Write-Host "The following files will be removed from git if present:" -ForegroundColor Yellow
$paths | ForEach-Object { Write-Host " - $_" }
$confirm = Read-Host "Proceed with git rm for these files? (y/n)"
if ($confirm -ne 'y') { Write-Host "Aborted."; exit 1 }
foreach ($p in $paths) {
    if (Test-Path $p) {
        git rm $p
    } else {
        Write-Host "File not found: $p" -ForegroundColor DarkYellow
    }
}
Write-Host "Done. Remember to commit the changes." -ForegroundColor Green
