# install-local-hooks.ps1
# This script installs local Git hooks (pre-commit, post-commit) to provide a fully automated CI/CD loop.

Write-Host "🔧 Installing Local Git Hooks..." -ForegroundColor Cyan

$sourceDir = "$PSScriptRoot/git-hooks" # Assuming we keep our masters here
$hooksDir = ".git/hooks"

# 1. Create the hook directories if they don't exist
if (-not (Test-Path $hooksDir)) {
    Write-Error "❌ Could not find .git. Are you in the project root?"
    exit 1
}

# 2. Write the pre-commit hook (runs tests)
$preCommitContent = @"
#!/bin/sh
echo "🔍 Running pre-commit tests (10s)..."
powershell.exe -ExecutionPolicy Bypass -Command "dotnet test FloraCore.Tests/FloraCore.Tests.csproj"
if [ `$? -ne 0 ]; then
    echo "❌ Tests failed! Commit aborted. Please fix the tests before committing."
    exit 1
fi
echo "✅ Tests passed. Proceeding with commit..."
exit 0
"@
Set-Content -Path "$hooksDir/pre-commit" -Value $preCommitContent

# 3. Write the post-commit hook (runs deploy)
$postCommitContent = @"
#!/bin/sh
echo "🚀 Starting Automated Local Deploy (Docker Desktop)..."
powershell.exe -ExecutionPolicy Bypass -Command "Start-Process powershell -ArgumentList '-NoExit', '-ExecutionPolicy Bypass', '-File ./deploy-docker.ps1' -WindowStyle Normal"
echo "✅ Local deployment started in a new terminal..."
exit 0
"@
Set-Content -Path "$hooksDir/post-commit" -Value $postCommitContent

Write-Host "✅ Hooks installed successfully!" -ForegroundColor Green
Write-Host "   - pre-commit: Runs dotnet tests before every commit." -ForegroundColor Cyan
Write-Host "   - post-commit: Automatically triggers Docker deployment after commit." -ForegroundColor Cyan
