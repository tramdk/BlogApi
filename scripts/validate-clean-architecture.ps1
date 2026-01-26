# Clean Architecture Validation Script
# Checks for violations of the Dependency Rule

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Clean Architecture Validation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$violations = @()
$baseDir = "c:\Users\T\.gemini\antigravity\scratch\BlogApi"

# Check 1: Application layer should NOT import from Infrastructure
Write-Host "[1/4] Checking Application layer dependencies..." -ForegroundColor Yellow
$appFiles = Get-ChildItem -Path (Join-Path $baseDir "Application") -Recurse -Filter "*.cs"
foreach ($file in $appFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match "using BlogApi\.Infrastructure") {
        $violations += "❌ $($file.FullName.Replace($baseDir, '')) imports Infrastructure"
    }
}

if ($violations.Count -eq 0) {
    Write-Host "   ✅ No Application → Infrastructure dependencies found" -ForegroundColor Green
}
else {
    Write-Host "   ❌ Found $($violations.Count) violations" -ForegroundColor Red
}

# Check 2: Interfaces should be in Application layer
Write-Host "[2/4] Checking interface locations..." -ForegroundColor Yellow
$infraInterfaces = Get-ChildItem -Path (Join-Path $baseDir "Infrastructure") -Recurse -Filter "I*.cs" -ErrorAction SilentlyContinue
$interfaceViolations = 0
foreach ($file in $infraInterfaces) {
    $content = Get-Content $file.FullName -Raw
    # Check if it's actually an interface (not just a file starting with I)
    if ($content -match "public interface I[A-Z]") {
        $violations += "⚠️  $($file.FullName.Replace($baseDir, '')) - Interface should be in Application"
        $interfaceViolations++
    }
}

if ($interfaceViolations -eq 0) {
    Write-Host "   ✅ All interfaces are in Application layer" -ForegroundColor Green
}
else {
    Write-Host "   ❌ Found $interfaceViolations interface(s) in Infrastructure" -ForegroundColor Red
}

# Check 3: Domain layer should have no dependencies
Write-Host "[3/4] Checking Domain layer purity..." -ForegroundColor Yellow
$domainFiles = Get-ChildItem -Path (Join-Path $baseDir "Domain") -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
$domainViolations = 0
foreach ($file in $domainFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match "using BlogApi\.(Application|Infrastructure)") {
        $violations += "❌ $($file.FullName.Replace($baseDir, '')) has dependencies on other layers"
        $domainViolations++
    }
}

if ($domainViolations -eq 0) {
    Write-Host "   ✅ Domain layer is pure (no dependencies)" -ForegroundColor Green
}
else {
    Write-Host "   ❌ Found $domainViolations violation(s)" -ForegroundColor Red
}

# Check 4: Count interfaces in correct location
Write-Host "[4/4] Verifying interface organization..." -ForegroundColor Yellow
$appInterfaces = Get-ChildItem -Path (Join-Path $baseDir "Application\Common\Interfaces") -Filter "*.cs" -ErrorAction SilentlyContinue
Write-Host "   ✅ Found $($appInterfaces.Count) interface(s) in Application/Common/Interfaces" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($violations.Count -eq 0) {
    Write-Host "✅ No Clean Architecture violations found!" -ForegroundColor Green
    Write-Host "   Your project follows Clean Architecture principles." -ForegroundColor Green
    exit 0
}
else {
    Write-Host "❌ Found $($violations.Count) violation(s):" -ForegroundColor Red
    Write-Host ""
    $violations | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
    Write-Host ""
    Write-Host "Please review and fix these violations." -ForegroundColor Red
    exit 1
}
