#!/usr/bin/env pwsh
# Script to run dotnet format with the same settings as the GitHub workflow

param (
    [switch]$VerifyOnly,
    [string]$Verbosity = "diagnostic",
    [string]$Folder = "."
)

$formatArgs = @()

if ($VerifyOnly.IsPresent) {
    $formatArgs += "--verify-no-changes"
    Write-Host "Running in verify-only mode (will not make changes)" -ForegroundColor Yellow
} else {
    Write-Host "Running in fix mode (will modify files)" -ForegroundColor Green
}

Write-Host "Running dotnet format with verbosity: $Verbosity" -ForegroundColor Cyan
Write-Host "Target folder: $Folder" -ForegroundColor Cyan
Write-Host ""

# Configure the format command with the same parameters as the GitHub workflow
$formatArgs += "--verbosity", $Verbosity

# Only add include if a specific folder is specified
if ($Folder -ne ".") {
    $formatArgs += "--include", $Folder
}

# Execute the command
Write-Host "Executing: dotnet format $([string]::Join(" ", $formatArgs))" -ForegroundColor Magenta
Write-Host "---------------------------------------------------------------" -ForegroundColor Magenta

try {
    & dotnet format @formatArgs
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Host "---------------------------------------------------------------" -ForegroundColor Magenta
        Write-Host "Format check passed successfully!" -ForegroundColor Green
    } else {
        Write-Host "---------------------------------------------------------------" -ForegroundColor Magenta
        Write-Host "Format check failed with exit code $exitCode" -ForegroundColor Red
        
        if ($VerifyOnly.IsPresent) {
            Write-Host "Run the script without -VerifyOnly to automatically fix formatting issues" -ForegroundColor Yellow
        }
    }

    exit $exitCode
} catch {
    Write-Host "Error executing dotnet format: $_" -ForegroundColor Red
    exit 1
}
