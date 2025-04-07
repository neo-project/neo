# Script to stop the background monitoring processes (Prometheus, Grafana, Alertmanager)

Write-Host "Attempting to stop monitoring services (Prometheus, Grafana, Alertmanager)..." -ForegroundColor Yellow

# --- Setup Paths ---
$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$monitoringDir = Resolve-Path -LiteralPath (Join-Path -Path $PSScriptRoot -ChildPath "..") # One level up from scripts dir
$pidFilePath = Join-Path $monitoringDir "running_pids.json"

$processesStopped = $false

# --- Method 1: Use PID file if it exists ---
if (Test-Path $pidFilePath) {
    Write-Host "Found PID file: $pidFilePath. Stopping processes by PID..." -ForegroundColor Gray
    try {
        $pids = Get-Content -Path $pidFilePath -Raw | ConvertFrom-Json

        # Stop processes in a specific order (optional, but sometimes helpful)
        # Order: Grafana, Alertmanager, Prometheus
        $pidsToStop = @(
            @{ Name = "Grafana"; Id = $pids.Grafana },
            @{ Name = "Alertmanager"; Id = $pids.Alertmanager },
            @{ Name = "Prometheus"; Id = $pids.Prometheus }
        )

        foreach ($procInfo in $pidsToStop) {
            if ($null -ne $procInfo.Id -and $procInfo.Id -gt 0) {
                try {
                    Get-Process -Id $procInfo.Id -ErrorAction Stop | Stop-Process -Force -ErrorAction Stop
                    Write-Host "  Stopped $($procInfo.Name) (PID: $($procInfo.Id))" -ForegroundColor Green
                    $processesStopped = $true
                } catch [Microsoft.PowerShell.Commands.ProcessCommandException] {
                    # Process likely already stopped
                    Write-Host "  $($procInfo.Name) (PID: $($procInfo.Id)) was not running." -ForegroundColor Gray
                } catch {
                    Write-Warning "  Failed to stop $($procInfo.Name) (PID: $($procInfo.Id)): $($_.Exception.Message)"
                }
            } else {
                 Write-Host "  Skipping $($procInfo.Name) (PID not found or invalid in file)." -ForegroundColor Gray
            }
        }

        # Clean up PID file after attempting stops
        Write-Host "Removing PID file: $pidFilePath" -ForegroundColor Gray
        Remove-Item -Path $pidFilePath -Force -ErrorAction SilentlyContinue

    } catch {
        Write-Warning "Error processing PID file '$pidFilePath': $($_.Exception.Message)"
        Write-Warning "Falling back to stopping processes by name/path."
        # Fall through to Method 2
    }
} else {
    Write-Host "PID file not found ($pidFilePath). Attempting to stop processes by name/path..." -ForegroundColor Gray
    # Fall through to Method 2
}

# --- Method 2: Fallback - Stop by name/path (if PID file missing or failed) ---
# This part runs if the PID file was not found or if there was an error reading it.
if (-not $processesStopped -or (-not (Test-Path $pidFilePath))) { # Added condition to avoid running if PIDs were successfully stopped
    Write-Host "Stopping processes by name/path filter..." -ForegroundColor Gray
    $processesToStopByName = @(
        @{ Name = "prometheus"; PathFilter = "*prometheus.exe*" },
        @{ Name = "grafana-server"; PathFilter = "*grafana-server.exe*" },
        @{ Name = "alertmanager"; PathFilter = "*alertmanager.exe*" }
    )

    foreach ($procInfo in $processesToStopByName) {
        try {
             Get-Process -Name $procInfo.Name -ErrorAction Stop | Where-Object { ($_.Path -like $procInfo.PathFilter) -or ([string]::IsNullOrEmpty($_.Path)) } | ForEach-Object {
                try {
                    $processPathInfo = if ([string]::IsNullOrEmpty($_.Path)) { "(Path unavailable)" } else { "($($_.Path))" }
                    Write-Host "  Stopping process $($_.Id) $processPathInfo" -ForegroundColor Gray
                    $_ | Stop-Process -Force -ErrorAction Stop
                    $processesStopped = $true # Mark as stopped even if done by name
                } catch {
                    Write-Warning "  Failed to stop process $($_.Id): $($_.Exception.Message)"
                }
            }
        } catch [Microsoft.PowerShell.Commands.ProcessCommandException] {
            # Expected if process name not found, ignore
            Write-Host "  No running process found matching name '$($procInfo.Name)' and path filter '$($procInfo.PathFilter)'." -ForegroundColor Gray
        } catch {
            Write-Warning "  Error checking process $($procInfo.Name): $($_.Exception.Message)"
        }
    }
}

if ($processesStopped) {
    Write-Host "Monitoring services stop sequence initiated." -ForegroundColor Green
} else {
    Write-Host "No running monitoring services found to stop (based on PID file or name/path)." -ForegroundColor Green
}

exit 0 