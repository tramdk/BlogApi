# Script to replace IPostRepository with IGenericRepository<Post, Guid>

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Replacing IPostRepository" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseDir = "c:\Users\T\.gemini\antigravity\scratch\BlogApi"
$files = @(
    "Application\Features\Posts\Commands\CreatePostCommand.cs",
    "Application\Features\Posts\Commands\UpdatePostCommand.cs",
    "Application\Features\Posts\Commands\DeletePostCommand.cs",
    "Application\Features\Posts\Commands\RatePostCommand.cs",
    "Application\Features\Posts\Queries\GetPostDetailQuery.cs",
    "Application\Common\Behaviors\AuthorizationBehavior.cs"
)

$count = 0

foreach ($file in $files) {
    $fullPath = Join-Path $baseDir $file
    
    if (Test-Path $fullPath) {
        try {
            $content = Get-Content $fullPath -Raw -Encoding UTF8
            
            # Replace IPostRepository with IGenericRepository<Post, Guid>
            $newContent = $content -replace 'IPostRepository', 'IGenericRepository<Post, Guid>'
            
            # Only write if changed
            if ($content -ne $newContent) {
                Set-Content -Path $fullPath -Value $newContent -NoNewline -Encoding UTF8
                Write-Host "✅ Updated: $file" -ForegroundColor Green
                $count++
            }
        }
        catch {
            Write-Host "❌ Error in $file : $_" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Updated $count files" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Update Program.cs DI registration" -ForegroundColor Yellow
Write-Host "2. Delete IPostRepository.cs" -ForegroundColor Yellow
Write-Host "3. Delete PostRepository.cs" -ForegroundColor Yellow
Write-Host "4. Run: dotnet build" -ForegroundColor Yellow
