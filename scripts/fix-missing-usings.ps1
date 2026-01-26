# Script to add missing using directive

$files = @(
    "Application\Features\Posts\Commands\UpdatePostCommand.cs",
    "Application\Features\Posts\Commands\DeletePostCommand.cs",
    "Application\Features\Posts\Commands\RatePostCommand.cs",
    "Application\Features\Posts\Queries\GetPostDetailQuery.cs",
    "Application\Common\Behaviors\AuthorizationBehavior.cs"
)

$baseDir = "c:\Users\T\.gemini\antigravity\scratch\BlogApi"

foreach ($file in $files) {
    $fullPath = Join-Path $baseDir $file
    
    if (Test-Path $fullPath) {
        $content = Get-Content $fullPath -Raw -Encoding UTF8
        
        # Check if already has the using
        if ($content -notmatch 'using BlogApi\.Domain\.Entities;') {
            # Add after the first using statement
            $content = $content -replace '(using BlogApi\.Application\.Common\.Interfaces;)', "`$1`r`nusing BlogApi.Domain.Entities;"
            
            Set-Content -Path $fullPath -Value $content -NoNewline -Encoding UTF8
            Write-Host "✅ Fixed: $file" -ForegroundColor Green
        }
        else {
            Write-Host "⏭️  Already OK: $file" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "Done! Run: dotnet build" -ForegroundColor Cyan
