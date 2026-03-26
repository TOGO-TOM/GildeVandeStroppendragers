#!/usr/bin/env pwsh
# AdminMembers Deployment Script
# This script will restore, build, migrate database, and run the application

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "AdminMembers Deployment Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET 8 is installed
Write-Host "Checking .NET installation..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($LASTEXITCODE -eq 0) {
    Write-Host "? .NET SDK Version: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "? .NET SDK not found. Please install .NET 8 SDK." -ForegroundColor Red
    exit 1
}

# Check if EF Core tools are installed
Write-Host "Checking Entity Framework Core tools..." -ForegroundColor Yellow
$efVersion = dotnet ef --version 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "? EF Core tools installed" -ForegroundColor Green
} else {
    Write-Host "Installing EF Core tools..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}

# Restore packages
Write-Host ""
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "? Packages restored successfully" -ForegroundColor Green
} else {
    Write-Host "? Failed to restore packages" -ForegroundColor Red
    exit 1
}

# Build the project
Write-Host ""
Write-Host "Building the project..." -ForegroundColor Yellow
dotnet build --configuration Release
if ($LASTEXITCODE -eq 0) {
    Write-Host "? Build successful" -ForegroundColor Green
} else {
    Write-Host "? Build failed" -ForegroundColor Red
    exit 1
}

# Apply database migrations
Write-Host ""
Write-Host "Applying database migrations..." -ForegroundColor Yellow
dotnet ef database update
if ($LASTEXITCODE -eq 0) {
    Write-Host "? Database migrations applied successfully" -ForegroundColor Green
} else {
    Write-Host "? Warning: Database migration failed or no migrations found" -ForegroundColor Yellow
    Write-Host "  You may need to create migrations first using:" -ForegroundColor Yellow
    Write-Host "  dotnet ef migrations add InitialCreate" -ForegroundColor Yellow
}

# Display application info
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Ready to launch AdminMembers!" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Application URLs:" -ForegroundColor Green
Write-Host "  - Main App: https://localhost:5001/index.html" -ForegroundColor White
Write-Host "  - API Docs: https://localhost:5001/swagger" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Yellow
Write-Host ""

# Run the application
Write-Host "Starting the application..." -ForegroundColor Yellow
Write-Host ""
dotnet run --configuration Release
