param(
    [string]$Token
)

if (-not $Token) {
    $Token = Read-Host -AsSecureString "Enter GitHub registration token" | ConvertFrom-SecureString
    # ConvertFrom-SecureString stores as encrypted string; docker compose expects plain text, so prompt for plain input if needed
    Write-Host "Please paste the token in plain text (it will not be saved):"
    $Token = Read-Host "Token"
}

Write-Host "Starting docker compose with token passed via environment variable"
$env:GITHUB_TOKEN = $Token
Push-Location -Path "$PSScriptRoot/.."
docker compose up --build
Pop-Location
