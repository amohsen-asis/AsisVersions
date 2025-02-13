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
	# Force a refresh of the PATH so we are sure to find the new tool
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
}
# Run GitVersion and capture output.  Use /output json for parsing. Crucial!
$gitVersionOutput = dotnet gitversion /output json

# Check for errors.  GitVersion might write errors to stderr even if it returns a version.
if ($LASTEXITCODE -ne 0) {
    Write-Error "GitVersion failed with exit code $LASTEXITCODE"
    exit 1
}

# Parse the JSON output.
try {
  $versionInfo = $gitVersionOutput | ConvertFrom-Json
}
catch {
  Write-Error "Failed to parse GitVersion output: $_"
  Write-Error "GitVersion Output: $gitVersionOutput"  # output the gitversion
  exit 1
}

# Extract and set environment variables for MSBuild.
$MajorMinorPatch = $versionInfo.MajorMinorPatch
[System.Environment]::SetEnvironmentVariable("Version", $MajorMinorPatch, "Process")
[System.Environment]::SetEnvironmentVariable("AssemblyVersion", "$MajorMinorPatch.0", "Process")
[System.Environment]::SetEnvironmentVariable("FileVersion", $MajorMinorPatch, "Process")
[System.Environment]::SetEnvironmentVariable("InformationalVersion", $MajorMinorPatch, "Process")

Write-Host "GitVersion set Version to: $MajorMinorPatch"

# Clean
Write-Host "Cleaning solution..."
dotnet clean $solution --configuration $configuration

# Restore dependencies
Write-Host "Restoring dependencies..."
dotnet restore $solution

# Build – crucially, remove any /p:Version overrides
Write-Host "Building solution..."
dotnet build $solution --configuration $configuration --no-restore

# Show version  (This will now use the environment variables)
# Remove or comment out this next line.  It's redundant and might be confusing.
# dotnet-gitversion

Write-Host "Build completed successfully!"
