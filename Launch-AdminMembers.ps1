#!/usr/bin/env pwsh
# AdminMembers Application Launcher (PowerShell)
# Double-click this file to start the application and open it in your browser

$Host.UI.RawUI.WindowTitle = "AdminMembers Launcher"
Clear-Host

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AdminMembers Application Launcher" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Change to script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Check if .NET is installed
Write-Host "Checking prerequisites..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? .NET SDK installed (version $dotnetVersion)" -ForegroundColor Green
    } else {
        throw "dotnet not found"
    }
} catch {
    Write-Host "? .NET SDK not found!" -ForegroundColor Red
    Write-Host "  Please install .NET 8 SDK from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

# Check if database needs to be created/updated
Write-Host "Checking database..." -ForegroundColor Yellow
try {
    $dbStatus = dotnet ef database update 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Database ready" -ForegroundColor Green
    } else {
        Write-Host "? Database update had warnings (this is usually ok)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "? Could not check database (will try at runtime)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Starting AdminMembers application..." -ForegroundColor Cyan
Write-Host ""

# Start the application in background
$appProcess = Start-Process -FilePath "dotnet" -ArgumentList "run" -PassThru -WindowStyle Normal

# Wait for application to start
Write-Host "Waiting for server to start..." -ForegroundColor Yellow
$maxAttempts = 15
$attempt = 0
$serverStarted = $false

while ($attempt -lt $maxAttempts -and -not $serverStarted) {
    Start-Sleep -Seconds 1
    $attempt++
    Write-Host "." -NoNewline -ForegroundColor Yellow

    # Check if process is still running
    if ($appProcess.HasExited) {
        Write-Host ""
        Write-Host ""
        Write-Host "? Application failed to start!" -ForegroundColor Red
        Write-Host "  Check the error messages above." -ForegroundColor Yellow
        Write-Host ""
        Read-Host "Press Enter to exit"
        exit 1
    }

    # Try to connect to the server
    try {
        $response = Invoke-WebRequest -Uri "https://localhost:7223" -Method Head -SkipCertificateCheck -TimeoutSec 1 -ErrorAction SilentlyContinue
        $serverStarted = $true
    } catch {
        # Try HTTP if HTTPS fails
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:5000" -Method Head -TimeoutSec 1 -ErrorAction SilentlyContinue
            $serverStarted = $true
        } catch {
            # Server not ready yet, continue waiting
        }
    }
}

Write-Host ""
Write-Host ""

if ($serverStarted) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Application Started Successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Opening in your default browser..." -ForegroundColor Cyan
    Write-Host ""

    # Try HTTPS first
    try {
        Start-Process "https://localhost:7223/index.html"
        Write-Host "? Browser opened to: https://localhost:7223/index.html" -ForegroundColor Green
    } catch {
        # Fallback to HTTP
        Start-Process "http://localhost:5000/index.html"
        Write-Host "? Browser opened to: http://localhost:5000/index.html" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "Available URLs:" -ForegroundColor Yellow
    Write-Host "  Landing Page: https://localhost:7223/index.html" -ForegroundColor White
    Write-Host "  Home Dashboard: https://localhost:7223/home.html" -ForegroundColor White
    Write-Host "  Members: https://localhost:7223/members.html" -ForegroundColor White
    Write-Host "  Settings: https://localhost:7223/settings.html" -ForegroundColor White
    Write-Host "  Export: https://localhost:7223/export.html" -ForegroundColor White
    Write-Host "  API Docs: https://localhost:7223/swagger" -ForegroundColor White
    Write-Host ""
    Write-Host "The application is running in the background." -ForegroundColor Cyan
    Write-Host "To stop the application, close this window or press Ctrl+C" -ForegroundColor Yellow
    Write-Host ""

    # Keep the window open and wait for user to close it
    try {
        Wait-Process -Id $appProcess.Id
    } catch {
        # Process ended or was killed
    }
} else {
    Write-Host "? Server took too long to start" -ForegroundColor Yellow
    Write-Host "  The application may still be starting." -ForegroundColor Yellow
    Write-Host "  Try opening: https://localhost:7223/index.html" -ForegroundColor Cyan
    Write-Host ""
    Read-Host "Press Enter to exit"
}

Write-Host ""
Write-Host "Application stopped." -ForegroundColor Yellow
