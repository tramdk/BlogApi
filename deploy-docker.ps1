# deploy-docker.ps1
# This script automates the deployment of the Blog API to Docker Desktop (local).

Write-Host "Starting Automated Docker Deployment..." -ForegroundColor Cyan

# 1. Check if Docker is running
$dockerActive = $false
try {
    $dockerInfo = docker info --format '{{.ServerVersion}}'
    if ($LASTEXITCODE -eq 0) { $dockerActive = $true }
} catch {
    $dockerActive = $false
}

if (-not $dockerActive) {
    Write-Error "Docker is not running. Please start Docker Desktop first."
    exit 1
}

# 2. Stop and remove existing containers to ensure a clean state
Write-Host "Stopping and removing existing containers..." -ForegroundColor Yellow
docker-compose down --remove-orphans

# 3. Build and Start containers
Write-Host "Building and starting services (API, DB, Redis)..." -ForegroundColor Yellow
docker-compose up -d --build

if ($LASTEXITCODE -ne 0) {
    Write-Error "Docker Compose failed to start."
    exit 1
}

# 4. Wait for API to be healthy
$url = "http://localhost:8080/scalar/v1"
Write-Host "Waiting for API to be ready at $url..." -ForegroundColor Yellow

$attempts = 0
$maxAttempts = 30
$ready = $false

while (-not $ready -and $attempts -lt $maxAttempts) {
    try {
        $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($null -ne $response -and $response.StatusCode -eq 200) {
            $ready = $true
        }
    }
    catch {
        # API not ready yet
    }
    
    if (-not $ready) {
        $attempts++
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 2
    }
}

Write-Host "" # New line

if ($ready) {
    Write-Host "Deployment Successful!" -ForegroundColor Green
    Write-Host "API is running at: http://localhost:8080" -ForegroundColor Cyan
    Write-Host "Documentation: http://localhost:8080/scalar/v1" -ForegroundColor Cyan
    
    # 5. Open browser automatically
    Write-Host "Opening documentation in your browser..." -ForegroundColor Yellow
    Start-Process "http://localhost:8080/scalar/v1"
}
else {
    Write-Warning "API is taking too long to start. Please check logs manually with: docker-compose logs api"
}

Write-Host "Done!" -ForegroundColor Green
