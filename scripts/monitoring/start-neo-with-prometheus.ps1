# Script to start Neo-CLI and setup monitoring with a local Prometheus, Grafana, and Alertmanager stack
param(
    # Neo-CLI metrics endpoint
    [string]$NeoMetricsAddress = "127.0.0.1:9101",
    # Prometheus server listen address
    [string]$PrometheusListenAddress = "127.0.0.1:9090",
    # Grafana server listen address
    [string]$GrafanaListenAddress = "127.0.0.1:3000",
    # Alertmanager listen address
    [string]$AlertmanagerListenAddress = "127.0.0.1:9093",
    # Default to null, script will create a unique timestamped path for Neo data
    [string]$NeoDataPath = $null,
    # Keeps existing Neo data directory if specified
    [switch]$KeepExistingNeoData = $false
)

# Display help information if requested
if ($args -contains "-help" -or $args -contains "--help" -or $args -contains "/?") {
    Write-Host @"
Usage: .\start-neo-with-prometheus.ps1 [options]

Starts Neo-CLI with a local monitoring stack (Prometheus, Grafana, Alertmanager).
Binaries for the monitoring stack will be downloaded if not found.

Options:
  -NeoMetricsAddress <addr>         Neo-CLI metrics endpoint (default: 127.0.0.1:9101)
                                    Neo-CLI will be started with '--prometheus <addr>'
  -PrometheusListenAddress <addr>   Prometheus server listen address (default: 127.0.0.1:9090)
  -GrafanaListenAddress <addr>      Grafana server listen address (default: 127.0.0.1:3000)
  -AlertmanagerListenAddress <addr> Alertmanager listen address (default: 127.0.0.1:9093)
  -NeoDataPath <path>               Path to Neo data directory (default: auto-generated in 'monitoring' folder)
  -KeepExistingNeoData              Keep existing Neo data directory if specified by -NeoDataPath

Example:
  # Start with default settings
  .\start-neo-with-prometheus.ps1

  # Start using a specific Neo data path
  .\start-neo-with-prometheus.ps1 -NeoDataPath C:\MyNeoData -KeepExistingNeoData
"@
    exit 0
}

# Display banner with configuration information
Write-Host @"
======================================================
        Neo with Local Monitoring Setup (PowerShell)
======================================================
"@ -ForegroundColor Cyan

# --- Setup Paths ---
$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$neoRepoRoot = Resolve-Path -LiteralPath (Join-Path -Path $PSScriptRoot -ChildPath "..\..") # Back to repo root
$neoCLIBinDir = Join-Path -Path $neoRepoRoot -ChildPath "bin\\Neo.CLI\\net9.0" # Adjust net9.0 if needed
$neoCLIPath = Join-Path -Path $neoCLIBinDir -ChildPath "neo-cli.dll"
$sourceConfigPath = Join-Path -Path $neoRepoRoot -ChildPath "src\\Neo.CLI\\config.json"
$targetConfigPath = Join-Path -Path $neoCLIBinDir -ChildPath "config.json"

# Monitoring Base Directory (one level up from scripts dir)
$monitoringDir = Resolve-Path -LiteralPath (Join-Path -Path $PSScriptRoot -ChildPath "..")

# --- Expected Local Binary Paths (Relative to monitoring dir) ---
# Users should place binaries here OR ensure they are in PATH
$prometheusDir = Join-Path -Path $monitoringDir -ChildPath "prometheus"
$prometheusLocalBinPath = Join-Path -Path $prometheusDir -ChildPath "prometheus.exe"
$prometheusConfigPath = Join-Path -Path $prometheusDir -ChildPath "prometheus.yml"
$prometheusDataPath = Join-Path -Path $prometheusDir -ChildPath "data"

$grafanaDir = Join-Path -Path $monitoringDir -ChildPath "grafana"
$grafanaLocalBinDir = Join-Path -Path $grafanaDir -ChildPath "bin"
$grafanaLocalBinPath = Join-Path -Path $grafanaLocalBinDir -ChildPath "grafana-server.exe"
$grafanaDataPath = Join-Path -Path $grafanaDir -ChildPath "data"
$grafanaLogsPath = Join-Path -Path $grafanaDir -ChildPath "logs"
# Grafana provisioning is read relative to its execution context or config

$alertmanagerDir = Join-Path -Path $monitoringDir -ChildPath "alertmanager"
$alertmanagerLocalBinPath = Join-Path -Path $alertmanagerDir -ChildPath "alertmanager.exe"
$alertmanagerConfigPath = Join-Path -Path $alertmanagerDir -ChildPath "alertmanager.yml"
$alertmanagerDataPath = Join-Path -Path $alertmanagerDir -ChildPath "data"


# --- Check for binaries in PATH or Local --- 
function Find-MonitoringBinary {
    param(
        [string]$BinaryName,        # e.g., "prometheus.exe"
        [string]$LocalBinaryPath    # e.g., "monitoring\prometheus\prometheus.exe"
    )

    # 1. Check the specific local path first
    if (Test-Path $LocalBinaryPath) {
        Write-Host "Found $BinaryName locally at $LocalBinaryPath." -ForegroundColor Green
        return $LocalBinaryPath
    }

    # 2. If not found locally, check system PATH
    $pathBinary = Get-Command $BinaryName -ErrorAction SilentlyContinue
    if ($pathBinary) {
        $foundPath = $pathBinary.Source
        Write-Host "Found $BinaryName in system PATH at $foundPath." -ForegroundColor Green
        return $foundPath
    }

    # Not found anywhere
    Write-Error "$BinaryName not found."
    Write-Error "Please install $BinaryName and either place it in '$LocalBinaryPath' or ensure it's available in your system PATH."
    Write-Error "Refer to the monitoring/README.md for installation instructions."
    return $null
}


# --- Validate/Locate Binaries ---
Write-Host "Locating monitoring binaries..." -ForegroundColor Cyan

$prometheusBinPath = Find-MonitoringBinary -BinaryName "prometheus.exe" -LocalBinaryPath $prometheusLocalBinPath
if (-not $prometheusBinPath) { exit 1 }

$grafanaBinPath = Find-MonitoringBinary -BinaryName "grafana-server.exe" -LocalBinaryPath $grafanaLocalBinPath
if (-not $grafanaBinPath) { exit 1 }

$alertmanagerBinPath = Find-MonitoringBinary -BinaryName "alertmanager.exe" -LocalBinaryPath $alertmanagerLocalBinPath
if (-not $alertmanagerBinPath) { exit 1 }


# --- Validate Neo CLI & Configs ---
if (-not (Test-Path $neoCLIPath)) {
    Write-Error "Neo-CLI not found at $neoCLIPath. Please build the project first (e.g., run 'dotnet build' in '$($neoRepoRoot)\\src\\Neo.CLI')."
    exit 1
}
if (-not (Test-Path $sourceConfigPath)) {
    Write-Error "Source Neo-CLI config not found at $sourceConfigPath."
    exit 1
}
if (-not (Test-Path $prometheusConfigPath)) {
    Write-Error "Prometheus config not found at $prometheusConfigPath. Please ensure it exists (it should have been created in previous steps)."
    exit 1
}
if (-not (Test-Path $alertmanagerConfigPath)) {
    Write-Error "Alertmanager config not found at $alertmanagerConfigPath. Please ensure it exists (it should have been created in previous steps)."
    exit 1
}

# --- Ensure Grafana Provisioning Dirs Exist ---
# The Grafana config files themselves (datasource, dashboard provider) should have been created previously.
# We just need to ensure the directories exist for Grafana to read them.
$grafanaProvisioningDir = Join-Path -Path $monitoringDir -ChildPath "grafana\\provisioning" # Relative to monitoring dir
$grafanaDatasourcesDir = Join-Path -Path $grafanaProvisioningDir -ChildPath "datasources"
$grafanaDashboardsDir = Join-Path -Path $grafanaProvisioningDir -ChildPath "dashboards"
if (-not (Test-Path $grafanaDatasourcesDir)) { New-Item -Path $grafanaDatasourcesDir -ItemType Directory -Force | Out-Null }
if (-not (Test-Path $grafanaDashboardsDir)) { New-Item -Path $grafanaDashboardsDir -ItemType Directory -Force | Out-Null }


# --- Setup Neo Data Directory ---
if ($KeepExistingNeoData -and (-not [string]::IsNullOrEmpty($NeoDataPath)) -and (Test-Path $NeoDataPath)) {
    Write-Host "Using existing Neo data directory: $NeoDataPath" -ForegroundColor Yellow
} else {
    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    # Create data dir inside monitoring folder for neatness
    $NeoDataPath = Join-Path -Path $monitoringDir -ChildPath "neo-data_$timestamp"
    Write-Host "Creating new Neo data directory: $NeoDataPath" -ForegroundColor Yellow
    New-Item -Path $NeoDataPath -ItemType Directory -Force | Out-Null
}
# Get absolute path for use in config.json
$absoluteNeoDataPath = (Resolve-Path -LiteralPath $NeoDataPath).ProviderPath


# --- Kill Existing Processes ---
Write-Host "Stopping any existing Neo-CLI (dotnet), Prometheus, Grafana, Alertmanager processes..." -ForegroundColor Yellow
$processesToStop = @(
    @{ Name = "dotnet"; PathFilter = "*$($neoCLIPath -replace '\\', '\\')*" }, # Match specific neo-cli dll path
    @{ Name = "prometheus"; PathFilter = "*prometheus.exe*" },
    @{ Name = "grafana-server"; PathFilter = "*grafana-server.exe*" },
    @{ Name = "alertmanager"; PathFilter = "*alertmanager.exe*" }
)

$killed = $false
foreach ($procInfo in $processesToStop) {
    # Use try-catch for Get-Process as it errors if no process found matching name
    try {
         Get-Process -Name $procInfo.Name -ErrorAction Stop | Where-Object { $_.Path -like $procInfo.PathFilter } | ForEach-Object {
            try {
                Write-Host "  Stopping process $($_.Id) ($($_.Path))" -ForegroundColor Gray
                $_ | Stop-Process -Force -ErrorAction Stop
                $killed = $true
            } catch {
                Write-Warning "Failed to stop process $($_.Id): $($_.Exception.Message)"
            }
        }
    } catch [Microsoft.PowerShell.Commands.ProcessCommandException] {
        # Expected if process name not found, ignore
    } catch {
        Write-Warning "Error checking process $($procInfo.Name): $($_.Exception.Message)"
    }
}

if ($killed) {
    Write-Host "Waiting for processes to terminate..." -ForegroundColor Gray
    Start-Sleep -Seconds 5
} else {
    Write-Host "No relevant running processes found to stop." -ForegroundColor Green
}

# --- Configure Neo-CLI (Basic Setup) ---
Write-Host "Configuring Neo-CLI..." -ForegroundColor Cyan
# Copy the default config to the bin directory
Copy-Item -Path $sourceConfigPath -Destination $targetConfigPath -Force

# Modify config for data path and logging (using JSON manipulation)
try {
    $configContent = Get-Content -Path $targetConfigPath -Raw
    $config = $configContent | ConvertFrom-Json

    # Update Storage Path (use the correctly escaped absolute path)
    $config.ApplicationConfiguration.Storage.Path = "$absoluteNeoDataPath\\Data_LevelDB_{0}"

    # Enable Logging
    $config.ApplicationConfiguration.Logger.Active = $true
    $config.ApplicationConfiguration.Logger.ConsoleOutput = $true
    $config.ApplicationConfiguration.Logger.Path = "$absoluteNeoDataPath\\Logs"

    # Remove Prometheus section if exists (we use command line arg)
    if ($config.ApplicationConfiguration.PSObject.Properties.Name.Contains("Prometheus")) {
        $config.ApplicationConfiguration.PSObject.Properties.Remove("Prometheus")
    }

    # Save updated config
    $config | ConvertTo-Json -Depth 10 | Set-Content -Path $targetConfigPath -NoNewline
    Write-Host "  Neo-CLI config updated ($targetConfigPath): Set Data/Log paths." -ForegroundColor Gray
} catch {
     Write-Error ("Failed to update Neo-CLI config at '$targetConfigPath': " + $($_.Exception.Message))
     exit 1
}


# --- Start Prometheus Server (in background) ---
Write-Host "Starting Prometheus Server..." -ForegroundColor Green
# Use the located $prometheusBinPath
$prometheusArgs = @(
    "--config.file=`"$prometheusConfigPath`"",
    "--storage.tsdb.path=`"$prometheusDataPath`"",
    "--web.listen-address=$PrometheusListenAddress",
    "--web.enable-lifecycle",
    "--log.level=info"
)
Write-Host "  Command: & \"$prometheusBinPath\" $($prometheusArgs -join ' ')" -ForegroundColor Magenta
$prometheusProcess = Start-Process -FilePath "$prometheusBinPath" -ArgumentList $prometheusArgs -WindowStyle Minimized -PassThru
Write-Host "  Prometheus started (PID: $($prometheusProcess.Id)). UI: http://$PrometheusListenAddress" -ForegroundColor Cyan
Start-Sleep -Seconds 2 # Give Prometheus time to start


# --- Start Alertmanager (in background) ---
Write-Host "Starting Alertmanager..." -ForegroundColor Green
# Use the located $alertmanagerBinPath
$alertmanagerArgs = @(
    "--config.file=`"$alertmanagerConfigPath`"",
    "--storage.path=`"$alertmanagerDataPath`"",
    "--web.listen-address=$AlertmanagerListenAddress",
    "--log.level=info"
)
Write-Host "  Command: & \"$alertmanagerBinPath\" $($alertmanagerArgs -join ' ')" -ForegroundColor Magenta
$alertmanagerProcess = Start-Process -FilePath "$alertmanagerBinPath" -ArgumentList $alertmanagerArgs -WindowStyle Minimized -PassThru
Write-Host "  Alertmanager started (PID: $($alertmanagerProcess.Id)). UI: http://$AlertmanagerListenAddress" -ForegroundColor Cyan
Start-Sleep -Seconds 1


# --- Start Grafana (in background) ---
Write-Host "Starting Grafana..." -ForegroundColor Green
# Use the located $grafanaBinPath and determine $grafanaHomePath based on it
$grafanaExeDir = Split-Path -Parent $grafanaBinPath
# Infer homepath: If found in PATH, we might not know the homepath reliably.
# If found locally (e.g., monitoring/grafana/bin/grafana-server.exe), homepath is monitoring/grafana
$grafanaHomePath = ""
if ($grafanaBinPath -like "$($monitoringDir)*") { # Check if path is within monitoring dir
    $grafanaHomePath = Resolve-Path -LiteralPath (Join-Path -Path $grafanaExeDir -ChildPath "..") # Go up one level from 'bin'
    Write-Host "  Inferred Grafana home path: $grafanaHomePath" -ForegroundColor Gray
} else {
    Write-Warning "Grafana found in PATH ($grafanaBinPath). Cannot automatically determine Grafana home path needed for config/provisioning."
    Write-Warning "Attempting to start Grafana without -homepath. Ensure Grafana's default/installed configuration points to provisioning files if needed."
}

# Ensure data/logs dirs exist relative to inferred homepath if possible
if ($grafanaHomePath) {
    $grafanaDataPath = Join-Path -Path $grafanaHomePath -ChildPath "data"
    $grafanaLogsPath = Join-Path -Path $grafanaHomePath -ChildPath "logs"
    if (-not (Test-Path $grafanaDataPath)) { New-Item -Path $grafanaDataPath -ItemType Directory -Force | Out-Null }
    if (-not (Test-Path $grafanaLogsPath)) { New-Item -Path $grafanaLogsPath -ItemType Directory -Force | Out-Null }
}

$grafanaArgs = @()
if ($grafanaHomePath) {
    # Only add -homepath if we inferred it
    $grafanaArgs += @("-homepath", "`"$grafanaHomePath`"")
}
# Add other args if necessary

Write-Host "  Command: & \"$grafanaBinPath\" $($grafanaArgs -join ' ')" -ForegroundColor Magenta

# Start Grafana, handling empty args
try {
    if ($grafanaArgs.Count -gt 0) {
        $grafanaProcess = Start-Process -FilePath "$grafanaBinPath" -ArgumentList $grafanaArgs -WindowStyle Minimized -PassThru -ErrorAction Stop
    } else {
        # Call without ArgumentList if no args are needed
        $grafanaProcess = Start-Process -FilePath "$grafanaBinPath" -WindowStyle Minimized -PassThru -ErrorAction Stop
    }
    Write-Host "  Grafana started (PID: $($grafanaProcess.Id)). UI: http://$GrafanaListenAddress (Default login: admin/admin)" -ForegroundColor Cyan
} catch {
    Write-Error "Failed to start Grafana: $($_.Exception.Message)"
    # Continue without Grafana if it fails?
    # exit 1 # Or exit if Grafana is critical
}

Start-Sleep -Seconds 5 # Grafana can take longer to start fully


# --- Start Neo-CLI (in foreground) ---
Write-Host "Starting Neo-CLI..." -ForegroundColor Green
$neoCliArgs = @(
    # Config file is implicitly loaded from bin dir, no need for --config
    "--prometheus", $NeoMetricsAddress, # Pass prometheus endpoint via command line
    "--verbose", "Info" # Set log level
)
$dotnetArgs = @("`"$neoCLIPath`"") + $neoCliArgs # Quote DLL path

Write-Host "  Command: dotnet $($dotnetArgs -join ' ')" -ForegroundColor Magenta
Write-Host "  Working Directory: $neoCLIBinDir" -ForegroundColor Cyan
Write-Host "  Data Directory: $absoluteNeoDataPath" -ForegroundColor Cyan
Write-Host "  Neo Metrics Endpoint: http://$NeoMetricsAddress/metrics" -ForegroundColor Cyan

Write-Host ""
Write-Host "Local Monitoring Stack URLs:" -ForegroundColor Green
Write-Host "  • Prometheus:   http://$PrometheusListenAddress" -ForegroundColor Cyan
Write-Host "  • Grafana:      http://$GrafanaListenAddress (Login: admin/admin)" -ForegroundColor Cyan
Write-Host "  • Alertmanager: http://$AlertmanagerListenAddress" -ForegroundColor Cyan

Write-Host ""
Write-Host "(Use Ctrl+C to stop Neo-CLI; Monitoring stack will keep running in background)" -ForegroundColor Cyan
Write-Host "To stop background processes later, use Stop-Process -Id <PID> or Task Manager."
Write-Host ""

# Store PIDs for potential later cleanup script
$pids = @{
    Prometheus = $prometheusProcess.Id
    Alertmanager = $alertmanagerProcess.Id
    Grafana = $grafanaProcess.Id
}
$pids | ConvertTo-Json | Set-Content -Path (Join-Path $monitoringDir "running_pids.json") -Encoding UTF8

$neoCliProcess = $null
try {
    # Execute dotnet in the correct directory and wait for it
    Push-Location $neoCLIBinDir
    # Start and wait, capture process info
    $neoCliProcess = Start-Process -FilePath "dotnet" -ArgumentList $dotnetArgs -NoNewWindow -Wait -PassThru
    Pop-Location

    if ($neoCliProcess.ExitCode -ne 0) {
         Write-Error "Neo-CLI exited unexpectedly with code $($neoCliProcess.ExitCode)."
    } else {
         Write-Host "Neo-CLI exited normally." -ForegroundColor Green
    }

} catch {
    Write-Error "Error starting or running Neo-CLI: $($_.Exception.Message)"
    # Ensure we pop location even on error if push succeeded
    if ($PWD.Path -eq $neoCLIBinDir) { Pop-Location }
    # Optional: Stop background processes if Neo-CLI fails to start?
    # Stop-Process -Id $prometheusProcess.Id -Force -ErrorAction SilentlyContinue
    # Stop-Process -Id $alertmanagerProcess.Id -Force -ErrorAction SilentlyContinue
    # Stop-Process -Id $grafanaProcess.Id -Force -ErrorAction SilentlyContinue
    exit 1 # Exit script if Neo-CLI failed
}

Write-Host "Neo-CLI has stopped. Local monitoring components (Prometheus, Grafana, Alertmanager) are still running." -ForegroundColor Yellow
Write-Host "You can stop them manually using their PIDs: " -ForegroundColor Yellow
$pids | Format-Table -AutoSize | Out-String | Write-Host -ForegroundColor Gray

# Clean up PID file
Remove-Item -Path (Join-Path $monitoringDir "running_pids.json") -ErrorAction SilentlyContinue

exit $neoCliProcess.ExitCode
