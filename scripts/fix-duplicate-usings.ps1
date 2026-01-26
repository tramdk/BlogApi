# Script to remove duplicate using directives

$files = @(
    "Application\Common\Behaviors\AuthorizationBehavior.cs",
    "Application\Features\Auth\Commands\LoginCommand.cs",
    "Application\Features\Cart\Commands\AddToCartCommand.cs",
    "Application\Features\Cart\Commands\RemoveFromCartCommand.cs",
    "Application\Features\Cart\Commands\UpdateCartItemQuantityCommand.cs",
    "Application\Features\Cart\Queries\GetCartQuery.cs",
    "Application\Features\Chat\Queries\GetChatHistory\GetChatHistoryQuery.cs",
    "Application\Features\Favorites\Commands\ToggleFavoriteCommand.cs",
    "Application\Features\Notifications\Commands\MarkAsRead\MarkNotificationAsReadCommand.cs",
    "Application\Features\Notifications\Queries\GetNotifications\GetNotificationsQuery.cs",
    "Application\Features\Reviews\Commands\AddProductReviewCommand.cs"
)

$baseDir = "c:\Users\T\.gemini\antigravity\scratch\BlogApi"
$count = 0

foreach ($file in $files) {
    $fullPath = Join-Path $baseDir $file
    
    if (Test-Path $fullPath) {
        try {
            $lines = Get-Content $fullPath -Encoding UTF8
            $uniqueLines = @()
            $seenUsings = @{}
            $inUsingSection = $true
            
            foreach ($line in $lines) {
                if ($line -match '^\s*using\s+') {
                    if (-not $seenUsings.ContainsKey($line)) {
                        $seenUsings[$line] = $true
                        $uniqueLines += $line
                    }
                }
                else {
                    if ($line -match '^\s*namespace\s+') {
                        $inUsingSection = $false
                    }
                    $uniqueLines += $line
                }
            }
            
            Set-Content -Path $fullPath -Value $uniqueLines -Encoding UTF8
            Write-Host "Fixed: $file" -ForegroundColor Green
            $count++
        }
        catch {
            Write-Host "Error in $file : $_" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Fixed $count files" -ForegroundColor Cyan
