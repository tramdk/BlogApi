# scripts/final-check.ps1
# Unified final check and validation script for FloraCore.
# Enforces coding policies, architecture rules, test runs, and deployments.

param (
    [Parameter(Mandatory=$true, Position=0)]
    [ValidateSet("install-hooks", "validate-arch", "validate-policy", "validate-all", "run-tests", "deploy")]
    [string]$Action
)

$baseDir = Split-Path $PSScriptRoot -Parent

# Ensure console supports UTF-8 characters for symbols
if ($PSVersionTable.PSVersion.Major -ge 6) {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
}

# 1. Install Git Hooks
function Install-Hooks {
    Write-Host "🔧 Installing Local Git Hooks..." -ForegroundColor Cyan
    $hooksDir = Join-Path $baseDir ".git/hooks"
    
    if (-not (Test-Path $hooksDir)) {
        Write-Error "❌ Could not find .git. Are you in the project root?"
        exit 1
    }

    # Write pre-commit hook (runs fast validations, then tests)
    $preCommitContent = @'
#!/bin/sh
echo "🔍 Running pre-commit validations..."
powershell.exe -ExecutionPolicy Bypass -Command "./scripts/final-check.ps1 validate-all"
if [ $? -ne 0 ]; then
    echo "❌ Pre-commit validation failed! Commit aborted."
    exit 1
fi

echo "🔍 Running pre-commit tests..."
powershell.exe -ExecutionPolicy Bypass -Command "./scripts/final-check.ps1 run-tests"
if [ $? -ne 0 ]; then
    echo "❌ Tests failed! Commit aborted. Please fix the tests before committing."
    exit 1
fi

echo "✅ All checks passed. Proceeding with commit..."
exit 0
'@

    # Write post-commit hook (triggers deployment)
    $postCommitContent = @'
#!/bin/sh
echo "🚀 Starting Automated Local Deploy (Docker Desktop)..."
powershell.exe -ExecutionPolicy Bypass -Command "./scripts/final-check.ps1 deploy"
echo "✅ Local deployment started in a new terminal..."
exit 0
'@

    $preCommitPath = Join-Path $hooksDir "pre-commit"
    $postCommitPath = Join-Path $hooksDir "post-commit"

    # Git Bash requires LF (\n) line endings to parse hooks correctly
    [System.IO.File]::WriteAllText($preCommitPath, $preCommitContent.Replace("`r`n", "`n"))
    [System.IO.File]::WriteAllText($postCommitPath, $postCommitContent.Replace("`r`n", "`n"))

    Write-Host "✅ Hooks installed successfully!" -ForegroundColor Green
    Write-Host "   - pre-commit: Runs static validations & tests before commit." -ForegroundColor Cyan
    Write-Host "   - post-commit: Triggers local Docker deployment after commit." -ForegroundColor Cyan
}

# 2. Clean Architecture validation
function Validate-Architecture {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Clean Architecture Validation" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    $violations = @()
    
    # Check 1: Application layer should NOT import from Infrastructure
    Write-Host "[1/4] Checking Application layer dependencies..." -ForegroundColor Yellow
    $appFiles = Get-ChildItem -Path (Join-Path $baseDir "Application") -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
    foreach ($file in $appFiles) {
        $content = Get-Content $file.FullName -Raw
        if ($content -match "using FloraCore\.Infrastructure") {
            $violations += "❌ $($file.FullName.Replace($baseDir, '')) imports Infrastructure"
        }
    }

    if ($violations.Count -eq 0) {
        Write-Host "   ✅ No Application -> Infrastructure dependencies found" -ForegroundColor Green
    }

    # Check 2: Interfaces should be in Application layer
    Write-Host "[2/4] Checking interface locations..." -ForegroundColor Yellow
    $infraInterfaces = Get-ChildItem -Path (Join-Path $baseDir "Infrastructure") -Recurse -Filter "I*.cs" -ErrorAction SilentlyContinue
    $interfaceViolations = 0
    foreach ($file in $infraInterfaces) {
        $content = Get-Content $file.FullName -Raw
        if ($content -match "public interface I[A-Z]") {
            $violations += "⚠️  $($file.FullName.Replace($baseDir, '')) - Interface should be in Application"
            $interfaceViolations++
        }
    }

    if ($interfaceViolations -eq 0) {
        Write-Host "   ✅ All interfaces are in Application layer" -ForegroundColor Green
    }

    # Check 3: Domain layer should have no dependencies
    Write-Host "[3/4] Checking Domain layer purity..." -ForegroundColor Yellow
    $domainFiles = Get-ChildItem -Path (Join-Path $baseDir "Domain") -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
    $domainViolations = 0
    foreach ($file in $domainFiles) {
        $content = Get-Content $file.FullName -Raw
        if ($content -match "using FloraCore\.(Application|Infrastructure)") {
            $violations += "❌ $($file.FullName.Replace($baseDir, '')) has dependencies on other layers"
            $domainViolations++
        }
    }

    if ($domainViolations -eq 0) {
        Write-Host "   ✅ Domain layer is pure (no dependencies)" -ForegroundColor Green
    }

    # Check 4: Count interfaces in correct location
    Write-Host "[4/4] Verifying interface organization..." -ForegroundColor Yellow
    $appInterfaces = Get-ChildItem -Path (Join-Path $baseDir "Application\Common\Interfaces") -Filter "*.cs" -ErrorAction SilentlyContinue
    Write-Host "   ✅ Found $($appInterfaces.Count) interface(s) in Application/Common/Interfaces" -ForegroundColor Green

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Architecture Summary" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan

    if ($violations.Count -eq 0) {
        Write-Host "✅ No Clean Architecture violations found!" -ForegroundColor Green
        return $true
    }
    else {
        Write-Host "❌ Found $($violations.Count) architecture violation(s):" -ForegroundColor Red
        Write-Host ""
        $violations | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
        Write-Host ""
        return $false
    }
}

# 3. Coding Policy validation
function Validate-CodingPolicy {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Coding Policy Validation (C# 12+)" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    $violations = @()

    # Get changed files from git
    $changedFiles = git status --porcelain | ForEach-Object {
        if ($_ -match '^[MADRC\s]+\s+(.*)$') {
            $matches[1].Trim().Trim('"')
        }
    } | Where-Object { $_ -like "*.cs" -and (Test-Path $_) }

    if ($changedFiles.Count -eq 0) {
        Write-Host "✅ No C# files changed. Skipping policy checks." -ForegroundColor Green
        return $true
    }

    Write-Host "Scanning $($changedFiles.Count) changed C# file(s)..." -ForegroundColor Yellow

    foreach ($file in $changedFiles) {
        $content = Get-Content $file -Raw
        $filename = Split-Path $file -Leaf
        $isTest = $file -like "*Tests*" -or $filename -like "*Test.cs"
        $isMiddleware = $filename -like "*Middleware.cs"
        $isException = $filename -like "*Exception.cs"
        
        if ($isTest -or $isMiddleware -or $isException) {
            continue
        }

        # Extract class or record name
        if ($content -match '(?:public|internal|private|protected)\s+(?:class|record|struct)\s+([A-Za-z0-9_]+)') {
            $className = $matches[1]
            
            # Check 1: Enforce Primary Constructor (traditional constructor should not match class name)
            $constructorPattern = "public\s+$className\s*\("
            if ($content -match $constructorPattern) {
                $violations += "❌ $($file) - Uses traditional constructor instead of C# 12+ Primary Constructor."
            }

            # Check 2: Verify primary constructor has null checks if parameters exist
            if ($content -match "(?:class|record|struct)\s+$className\s*\(\s*[^)]+\s*\)") {
                if ($content -notmatch "ThrowIfNull" -and $content -notmatch "\?\?\s+throw") {
                    $violations += "⚠️  $($file) - Primary constructor has parameters but no null-checks (ThrowIfNull) found."
                }
            }
        }
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Policy Summary" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan

    if ($violations.Count -eq 0) {
        Write-Host "✅ All changed files comply with coding policies!" -ForegroundColor Green
        return $true
    }
    else {
        Write-Host "❌ Found $($violations.Count) coding policy violation(s):" -ForegroundColor Red
        Write-Host ""
        $violations | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
        Write-Host ""
        return $false
    }
}

# 4. Run Test Suite
function Run-Tests {
    Write-Host "🧪 Running test suite..." -ForegroundColor Cyan
    dotnet test (Join-Path $baseDir "FloraCore.Tests/FloraCore.Tests.csproj")
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Tests failed!"
        exit 1
    }
}

# 5. Local Docker Deployment
function Deploy {
    Write-Host "🚀 Starting Automated Local Deploy (Docker Desktop)..." -ForegroundColor Cyan
    Start-Process powershell -ArgumentList '-NoExit', '-ExecutionPolicy Bypass', '-File ./deploy-docker.ps1' -WindowStyle Normal
}

# Route the action parameter
switch ($Action) {
    "install-hooks" {
        Install-Hooks
    }
    "validate-arch" {
        $result = Validate-Architecture
        if (-not $result) { exit 1 }
    }
    "validate-policy" {
        $result = Validate-CodingPolicy
        if (-not $result) { exit 1 }
    }
    "validate-all" {
        # Sắp xếp thứ tự logic: chạy các static checks nhanh nhất trước để tối ưu hóa thời gian phản hồi
        Write-Host "⏳ Running static validations..." -ForegroundColor Cyan
        
        $archResult = Validate-Architecture
        if (-not $archResult) {
            Write-Error "❌ Clean Architecture check failed!"
            exit 1
        }
        
        $policyResult = Validate-CodingPolicy
        if (-not $policyResult) {
            Write-Error "❌ Coding Policy check failed!"
            exit 1
        }
        
        Write-Host "✅ All static validations passed successfully!" -ForegroundColor Green
    }
    "run-tests" {
        Run-Tests
    }
    "deploy" {
        Deploy
    }
}
