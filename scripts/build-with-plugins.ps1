# Neo build script with plugin integration v1.1.0
# This script builds Neo.CLI and all plugins, organizing them in framework-specific directories

param(
    [string]$BuildConfig = "Release",
    [switch]$Clean = $true,
    [switch]$Verbose = $false
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Set the directory variables
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Join-Path $ScriptDir ".."
$OutputDir = Join-Path $RootDir "bin"
$CliProject = Join-Path $RootDir "src\Neo.CLI\Neo.CLI.csproj"
$NeoSolution = Join-Path $RootDir "neo.sln"

# Log file setup
$LogFile = Join-Path $RootDir "build.log"
"Build started at $(Get-Date)" | Out-File -FilePath $LogFile -Encoding UTF8

function Write-Log {
    param([string]$Message)
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogMessage = "[$Timestamp] $Message"
    Write-Host $LogMessage
}

function Write-Error-Log {
    param([string]$Message)
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogMessage = "[$Timestamp] ERROR: $Message"
    Write-Host $LogMessage -ForegroundColor Red
    $LogMessage | Out-File -FilePath $LogFile -Append -Encoding UTF8
}

function Write-Warning-Log {
    param([string]$Message)
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogMessage = "[$Timestamp] WARNING: $Message"
    Write-Host $LogMessage -ForegroundColor Yellow
    $LogMessage | Out-File -FilePath $LogFile -Append -Encoding UTF8
}

# Function definitions
function Find-Dll {
    param(
        [string]$DllName,
        [string]$RootDir,
        [string]$OutputDir,
        [string]$BuildConfig,
        [string]$TargetFramework
    )
    
    # First check if the DLL is already in the main bin directory
    $MainDll = Join-Path $OutputDir $DllName
    if (Test-Path $MainDll) {
        return $MainDll
    }
    
    # Check in direct project folders under bin
    $ProjectDirs = Get-ChildItem -Path $OutputDir -Directory -ErrorAction SilentlyContinue
    foreach ($ProjectDir in $ProjectDirs) {
        $DllPath = Join-Path $ProjectDir.FullName $DllName
        if (Test-Path $DllPath) {
            return $DllPath
        }
        
        # Also check for nested target framework folders
        $FrameworkDirs = Get-ChildItem -Path $ProjectDir.FullName -Directory -ErrorAction SilentlyContinue
        foreach ($FrameworkDir in $FrameworkDirs) {
            $DllPath = Join-Path $FrameworkDir.FullName $DllName
            if (Test-Path $DllPath) {
                return $DllPath
            }
        }
    }
    
    # Common locations to check in the src directory
    $SrcDir = Join-Path $RootDir "src"
    $Patterns = @(
        "*\bin\$BuildConfig\$TargetFramework\$DllName",
        "*\bin\$BuildConfig\$DllName",
        "*\*\bin\$BuildConfig\$TargetFramework\$DllName",
        "*\*\bin\$BuildConfig\$DllName"
    )
    
    # Check each pattern
    foreach ($Pattern in $Patterns) {
        $FoundDlls = Get-ChildItem -Path $SrcDir -Filter $DllName -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.FullName -like "*$Pattern*" }
        if ($FoundDlls) {
            return $FoundDlls[0].FullName
        }
    }
    
    # As a last resort, search the entire src directory
    Write-Log "Searching for $DllName in src directory..."
    $FoundDlls = Get-ChildItem -Path $SrcDir -Filter $DllName -Recurse -ErrorAction SilentlyContinue
    if ($FoundDlls) {
        return $FoundDlls[0].FullName
    }
    
    return $null
}

function Copy-PluginDependencies {
    param(
        [string]$PluginDll,
        [string]$CliOutputDir,
        [string]$ProjectName
    )

    $PluginDir = Split-Path -Parent $PluginDll
    Write-Log "Copying dependencies for $ProjectName from $PluginDir to $CliOutputDir"

    # Copy all DLL files from the plugin's output directory (except the main plugin DLL)
    $DllFiles = Get-ChildItem -Path $PluginDir -Filter "*.dll" | Where-Object { $_.Name -ne "$ProjectName.dll" }
    foreach ($DllFile in $DllFiles) {
        $DestPath = Join-Path $CliOutputDir $DllFile.Name
        if (-not (Test-Path $DestPath)) {
            Write-Log "Copying dependency: $($DllFile.Name)"
            Copy-Item -Path $DllFile.FullName -Destination $DestPath -Force
        }
    }
}

function Process-PluginDirectory {
    param(
        [string]$PluginDir,
        [string]$CliPluginsDir,
        [string]$BuildConfig,
        [string]$RootDir,
        [string]$OutputDir,
        [string]$TargetFramework,
        [string]$LogFile
    )
    
    $script:PluginsProcessed = 0
    
    # Find all .csproj files in the directory
    $ProjectFiles = Get-ChildItem -Path $PluginDir -Filter "*.csproj"
    
    if ($ProjectFiles.Count -eq 0) {
        Write-Log "No project files found in $PluginDir, skipping"
        return
    }
    
    foreach ($ProjectFile in $ProjectFiles) {
        $ProjectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectFile.Name)
        
        # Build the plugin
        Write-Log "Building project: $ProjectName"
        try {
            & dotnet build $ProjectFile.FullName -c $BuildConfig
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to build $ProjectName"
            }
            Write-Log "Successfully built $ProjectName"
        } catch {
            Write-Error-Log "Failed to build $ProjectName"
            exit 1
        }
        
        # Find the DLL file
        $PluginDll = Find-Dll -DllName "$ProjectName.dll" -RootDir $RootDir -OutputDir $OutputDir -BuildConfig $BuildConfig -TargetFramework $TargetFramework
        
        # Create a plugin-specific folder in the Neo.CLI's plugins directory
        $PluginSpecificDir = Join-Path $CliPluginsDir $ProjectName
        New-Item -ItemType Directory -Path $PluginSpecificDir -Force | Out-Null
        
        # Copy the plugin DLL to its specific directory
        if ($PluginDll -and (Test-Path $PluginDll)) {
            Write-Log "Copying $ProjectName.dll to $PluginSpecificDir\ from $PluginDll"
            Copy-Item -Path $PluginDll -Destination $PluginSpecificDir -Force
            
            # Verify the copy was successful
            $CopiedDll = Join-Path $PluginSpecificDir "$ProjectName.dll"
            if (-not (Test-Path $CopiedDll)) {
                Write-Error-Log "Failed to copy $ProjectName.dll to $PluginSpecificDir"
                exit 1
            }

            # Copy plugin dependencies
            $CliOutputDir = Join-Path $CliPluginsDir ".."
            Copy-PluginDependencies -PluginDll $PluginDll -CliOutputDir $CliOutputDir -ProjectName $ProjectName
        } else {
            Write-Warning-Log "Could not find DLL for $ProjectName"
        }
        
        # Look for the config file in multiple locations
        $PluginConfig = Join-Path $PluginDir "$ProjectName.json"
        if (-not (Test-Path $PluginConfig)) {
            # Try to find the config elsewhere
            $ConfigFiles = Get-ChildItem -Path $PluginDir -Filter "$ProjectName.json" -Recurse -ErrorAction SilentlyContinue
            if ($ConfigFiles) {
                $PluginConfig = $ConfigFiles[0].FullName
            }
        }
        
        # Copy the plugin config if it exists
        if (Test-Path $PluginConfig) {
            Write-Log "Copying $ProjectName.json to $PluginSpecificDir\ from $PluginConfig"
            Copy-Item -Path $PluginConfig -Destination $PluginSpecificDir -Force
            
            # Verify the copy was successful
            $CopiedConfig = Join-Path $PluginSpecificDir "$ProjectName.json"
            if (-not (Test-Path $CopiedConfig)) {
                Write-Error-Log "Failed to copy $ProjectName.json to $PluginSpecificDir"
                exit 1
            }
        } else {
            Write-Log "No config file found for $ProjectName, skipping config copy"
        }
        
        Write-Log "Plugin $ProjectName installed"
        Write-Host ""
        
        $script:PluginsProcessed++
    }
}

# Start time measurement
$StartTime = Get-Date

Write-Host "Neo Build Script with Plugin Integration" -ForegroundColor Green
Write-Host ""

# Initialize counters
$PluginsFound = 0
$PluginsProcessed = 0

# Extract target framework from Neo.CLI.csproj
Write-Log "Detecting .NET version from Neo.CLI project..."

if (-not (Test-Path $CliProject)) {
    Write-Error-Log "Could not find Neo.CLI project at $CliProject"
    exit 1
}

try {
    $ProjectContent = Get-Content $CliProject -Raw
    $TargetFrameworkMatch = [regex]::Match($ProjectContent, '<TargetFramework>(.*?)</TargetFramework>')
    
    if ($TargetFrameworkMatch.Success) {
        $TargetFramework = $TargetFrameworkMatch.Groups[1].Value
        Write-Log "Detected target framework: $TargetFramework"
    } else {
        Write-Warning-Log "Could not detect target framework, falling back to net7.0"
        $TargetFramework = "net7.0"
    }
} catch {
    Write-Warning-Log "Error reading project file, falling back to net7.0"
    $TargetFramework = "net7.0"
}

Write-Log "Root directory: $RootDir"
Write-Log "Output directory: $OutputDir"
Write-Log "Target framework: $TargetFramework"
Write-Host ""

# Verify .NET SDK is installed
Write-Log "Verifying .NET SDK installation..."
try {
    $DotnetVersion = & dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed"
    }
    Write-Log "Found .NET SDK version: $DotnetVersion"
} catch {
    Write-Error-Log ".NET SDK is not installed or not in PATH"
    exit 1
}

Write-Host ""

# Clean up old build artifacts
if ($Clean) {
    Write-Log "Cleaning previous build artifacts..."
    if (Test-Path $OutputDir) {
        Remove-Item $OutputDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Log "Clean complete"
    Write-Host ""
}

# First build the entire solution to ensure dependencies are built
if (Test-Path $NeoSolution) {
    Write-Log "Building entire Neo solution first to resolve dependencies..."
    try {
        & dotnet build $NeoSolution -c $BuildConfig
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to build Neo solution"
        }
        Write-Log "Successfully built Neo solution"
    } catch {
        Write-Error-Log "Failed to build Neo solution"
        exit 1
    }
    Write-Host ""
} else {
    Write-Log "Neo solution file not found. Will attempt to build Neo.CLI directly."
}

# Build Neo.CLI
Write-Log "Building project: Neo.CLI"
try {
    & dotnet build $CliProject -c $BuildConfig
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build Neo.CLI"
    }
    Write-Log "Successfully built Neo.CLI"
} catch {
    Write-Error-Log "Failed to build Neo.CLI"
    exit 1
}
Write-Host ""

# Copy Neo.CLI output to bin directory
Write-Log "Copying Neo.CLI output to bin directory..."

$CliDll = Find-Dll -DllName "neo-cli.dll" -RootDir $RootDir -OutputDir $OutputDir -BuildConfig $BuildConfig -TargetFramework $TargetFramework

if ($CliDll -and (Test-Path $CliDll)) {
    $CliDir = Split-Path -Parent $CliDll
    Write-Log "Found neo-cli.dll at: $CliDir"
    
    Copy-Item -Path "$CliDir\*" -Destination $OutputDir -Recurse -Force
    Write-Log "Neo.CLI copied to output directory"
} else {
    Write-Error-Log "Could not find neo-cli.dll"
    Write-Log "Trying to find any DLLs in the output directories..."
    
    # Try to find any DLL in the build output directories
    $AnyDll = Get-ChildItem -Path $RootDir -Filter "*.dll" -Recurse | Select-Object -First 1
    if ($AnyDll) {
        $AnyDir = Split-Path -Parent $AnyDll.FullName
        Write-Log "Found DLLs at: $AnyDir"
        Copy-Item -Path "$AnyDir\*" -Destination $OutputDir -Recurse -Force
        Write-Log "Neo.CLI copied from found directory"
    } else {
        Write-Error-Log "Failed to locate any build output. Make sure the build succeeded."
        exit 1
    }
}

Write-Host ""

# Create plugins directory in the Neo.CLI output directory
Write-Log "Setting up plugins directory..."

$CliPluginsDir = Join-Path $OutputDir "Neo.CLI\$TargetFramework\Plugins"
New-Item -ItemType Directory -Path $CliPluginsDir -Force | Out-Null

Write-Log "Plugins directory created at: $CliPluginsDir"
Write-Host ""

# Build and copy all plugins
Write-Log "Building and installing plugins..."
Write-Host ""

# Find all plugin directories
$PluginDirs = Get-ChildItem -Path (Join-Path $RootDir "src\Plugins") -Directory

foreach ($PluginDir in $PluginDirs) {
    if ($PluginDir.Name -eq "obj") {
        Write-Log "Skipping $($PluginDir.Name) - not a plugin directory"
        continue
    }
    
    Process-PluginDirectory -PluginDir $PluginDir.FullName -CliPluginsDir $CliPluginsDir -BuildConfig $BuildConfig -RootDir $RootDir -OutputDir $OutputDir -TargetFramework $TargetFramework -LogFile $LogFile
    $PluginsFound++
}

Write-Log "All plugins have been built and installed"
Write-Log "Found $PluginsFound plugin directories, processed $PluginsProcessed plugin projects"
Write-Host ""

# Calculate build time
$EndTime = Get-Date
$Duration = $EndTime - $StartTime
$DurationString = "{0:hh\:mm\:ss\.ff}" -f $Duration

Write-Log "Build completed successfully in $DurationString!"
Write-Host ""

Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "The Neo node with plugins is available at: $OutputDir\Neo.CLI\$TargetFramework"
Write-Host "You can run it using: cd '$OutputDir\Neo.CLI\$TargetFramework' && dotnet neo-cli.dll"

# Verify essential plugins were installed
$EssentialPlugins = @("DBFTPlugin", "ApplicationLogs", "RpcServer")
foreach ($Plugin in $EssentialPlugins) {
    $PluginDll = Join-Path $CliPluginsDir "$Plugin\$Plugin.dll"
    if (-not (Test-Path $PluginDll)) {
        Write-Warning-Log "Essential plugin $Plugin is missing"
    }
}
