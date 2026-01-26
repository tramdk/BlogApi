# Script to fix Clean Architecture violations
# Replace Infrastructure.Repositories imports with Application.Common.Interfaces

$files = @(
    "Application\Features\Products\Commands\CreateProductCommand.cs",
    "Application\Features\Products\Commands\UpdateProductCommand.cs",
    "Application\Features\Products\Commands\DeleteProductCommand.cs",
    "Application\Features\Products\Queries\GetProductsQuery.cs",
    "Application\Features\Products\Queries\GetProductByIdQuery.cs",
    "Application\Features\ProductCategories\Commands\CreateProductCategoryCommand.cs",
    "Application\Features\ProductCategories\Commands\UpdateProductCategoryCommand.cs",
    "Application\Features\ProductCategories\Commands\DeleteProductCategoryCommand.cs",
    "Application\Features\ProductCategories\Queries\GetProductCategoriesQuery.cs",
    "Application\Features\ProductCategories\Queries\GetProductCategoryByIdQuery.cs",
    "Application\Features\PostCategories\Commands\CreatePostCategoryCommand.cs",
    "Application\Features\PostCategories\Commands\UpdatePostCategoryCommand.cs",
    "Application\Features\PostCategories\Commands\DeletePostCategoryCommand.cs",
    "Application\Features\PostCategories\Queries\GetPostCategoriesQuery.cs",
    "Application\Features\PostCategories\Queries\GetPostCategoryByIdQuery.cs",
    "Application\Features\Cart\Commands\AddToCartCommand.cs",
    "Application\Features\Cart\Commands\RemoveFromCartCommand.cs",
    "Application\Features\Cart\Commands\UpdateCartItemQuantityCommand.cs",
    "Application\Features\Cart\Queries\GetCartQuery.cs",
    "Application\Features\Favorites\Commands\ToggleFavoriteCommand.cs",
    "Application\Features\Reviews\Commands\AddProductReviewCommand.cs",
    "Application\Features\Notifications\Commands\MarkAsRead\MarkNotificationAsReadCommand.cs",
    "Application\Features\Notifications\Queries\GetNotifications\GetNotificationsQuery.cs",
    "Application\Features\Chat\Queries\GetChatHistory\GetChatHistoryQuery.cs",
    "Application\Features\Auth\Commands\LoginCommand.cs",
    "Application\Features\Auth\Commands\LogoutCommand.cs",
    "Application\Features\Auth\Commands\RefreshTokenCommand.cs",
    "Application\Common\Behaviors\AuthorizationBehavior.cs"
)

$baseDir = "c:\Users\T\.gemini\antigravity\scratch\BlogApi"
$count = 0
$errors = @()

foreach ($file in $files) {
    $fullPath = Join-Path $baseDir $file
    
    if (Test-Path $fullPath) {
        try {
            $content = Get-Content $fullPath -Raw -Encoding UTF8
            
            # Replace the import
            $newContent = $content -replace 'using BlogApi\.Infrastructure\.Repositories;', 'using BlogApi.Application.Common.Interfaces;'
            
            # Only write if changed
            if ($content -ne $newContent) {
                Set-Content -Path $fullPath -Value $newContent -NoNewline -Encoding UTF8
                Write-Host "Fixed: $file" -ForegroundColor Green
                $count++
            }
            else {
                Write-Host "Skipped (no change needed): $file" -ForegroundColor Yellow
            }
        }
        catch {
            $errors += "Error in $file : $_"
            Write-Host "Error in $file : $_" -ForegroundColor Red
        }
    }
    else {
        Write-Host "File not found: $file" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "   Fixed: $count files" -ForegroundColor Green
Write-Host "   Errors: $($errors.Count)" -ForegroundColor $(if ($errors.Count -gt 0) { "Red" } else { "Green" })

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "Errors:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "   $_" }
}
