# build.ps1

# Stop on first error
$ErrorActionPreference = "Stop"

# Variables
$solution = "ASIS.API"
$configuration = "Release"

Write-Host "Building $solution..."

# Ensure tools are installed
Write-Host "Checking GitVersion installation..."
if (-not (Get-Command dotnet-gitversion -ErrorAction SilentlyContinue)) {
    Write-Host "Installing GitVersion..."
    dotnet tool install --global GitVersion.Tool
}

# Clean
Write-Host "Cleaning solution..."
dotnet clean $solution --configuration $configuration

# Restore dependencies
Write-Host "Restoring dependencies..."
dotnet restore $solution

# Build
Write-Host "Building solution..."
dotnet build $solution --configuration $configuration --no-restore

# Show version
Write-Host "Current version:"
dotnet-gitversion

Write-Host "Build completed successfully!"