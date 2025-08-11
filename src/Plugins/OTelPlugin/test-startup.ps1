# Neo OpenTelemetry Plugin Startup Test Script (PowerShell)
# This script tests that the plugin starts correctly with Neo

param(
    [string]$NeoCliPath = "C:\neo-cli",
    [int]$PrometheusPort = 9090,
    [int]$WaitSeconds = 30
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Neo OpenTelemetry Plugin Startup Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

function Test-PluginFiles {
    Write-Host "Checking plugin files..." -ForegroundColor Yellow
    
    $pluginDir = Join-Path $PSScriptRoot "bin\Release\net9.0"
    if (-not (Test-Path $pluginDir)) {
        $pluginDir = Join-Path $PSScriptRoot "bin\Debug\net9.0"
    }
    
    if (Test-Path "$pluginDir\OTelPlugin.dll") {
        Write-Host "✓ Plugin DLL found" -ForegroundColor Green
    } else {
        Write-Host "✗ Plugin DLL not found. Please build first: dotnet build" -ForegroundColor Red
        return $false
    }
    
    if (Test-Path "$PSScriptRoot\OTelPlugin.json") {
        Write-Host "✓ Configuration file found" -ForegroundColor Green
        
        # Validate JSON
        try {
            $config = Get-Content "$PSScriptRoot\OTelPlugin.json" -Raw | ConvertFrom-Json
            if ($config.PluginConfiguration.Enabled) {
                Write-Host "✓ Plugin is enabled in configuration" -ForegroundColor Green
            } else {
                Write-Host "⚠ Plugin is disabled in configuration" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "✗ Invalid JSON configuration" -ForegroundColor Red
            return $false
        }
    } else {
        Write-Host "✗ Configuration file not found" -ForegroundColor Red
        return $false
    }
    
    return $true
}

function Test-Prerequisites {
    Write-Host ""
    Write-Host "Checking prerequisites..." -ForegroundColor Yellow
    
    # Check .NET runtime
    try {
        $dotnetVersion = dotnet --version
        Write-Host "✓ .NET runtime found: $dotnetVersion" -ForegroundColor Green
    } catch {
        Write-Host "✗ .NET runtime not found" -ForegroundColor Red
        return $false
    }
    
    # Check if port is available
    $listener = Get-NetTCPConnection -LocalPort $PrometheusPort -ErrorAction SilentlyContinue
    if ($listener) {
        Write-Host "⚠ Port $PrometheusPort is already in use" -ForegroundColor Yellow
        return $false
    } else {
        Write-Host "✓ Port $PrometheusPort is available" -ForegroundColor Green
    }
    
    return $true
}

function Test-MetricsEndpoint {
    param([int]$Port)
    
    Write-Host ""
    Write-Host "Testing metrics endpoint..." -ForegroundColor Yellow
    
    $url = "http://localhost:$Port/metrics"
    
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
        
        if ($response.StatusCode -eq 200) {
            Write-Host "✓ Prometheus endpoint is accessible" -ForegroundColor Green
            
            # Check for Neo metrics
            if ($response.Content -match "neo_blockchain_height") {
                Write-Host "✓ Neo metrics are being exposed" -ForegroundColor Green
                
                # Display sample metrics
                Write-Host ""
                Write-Host "Sample metrics:" -ForegroundColor Cyan
                $metrics = $response.Content -split "`n" | Where-Object { $_ -match "^neo_" } | Select-Object -First 5
                foreach ($metric in $metrics) {
                    Write-Host "  $metric" -ForegroundColor Gray
                }
                
                return $true
            } else {
                Write-Host "⚠ Endpoint accessible but Neo metrics not found yet" -ForegroundColor Yellow
                return $false
            }
        }
    } catch {
        Write-Host "✗ Cannot reach metrics endpoint at $url" -ForegroundColor Red
        Write-Host "  Error: $_" -ForegroundColor Red
        return $false
    }
    
    return $false
}

function Start-NeoWithPlugin {
    Write-Host ""
    Write-Host "Starting Neo node with OpenTelemetry plugin..." -ForegroundColor Yellow
    
    # Check if Neo CLI exists
    if (-not (Test-Path $NeoCliPath)) {
        Write-Host "✗ Neo CLI not found at $NeoCliPath" -ForegroundColor Red
        Write-Host "  Please set -NeoCliPath parameter" -ForegroundColor Yellow
        return $false
    }
    
    # Copy plugin to Neo
    $targetPluginDir = Join-Path $NeoCliPath "Plugins\OTelPlugin"
    
    Write-Host "  Copying plugin to $targetPluginDir" -ForegroundColor Gray
    
    if (-not (Test-Path $targetPluginDir)) {
        New-Item -ItemType Directory -Path $targetPluginDir -Force | Out-Null
    }
    
    # Copy plugin files
    $sourceDir = Join-Path $PSScriptRoot "bin\Release\net9.0"
    if (-not (Test-Path $sourceDir)) {
        $sourceDir = Join-Path $PSScriptRoot "bin\Debug\net9.0"
    }
    
    Copy-Item "$sourceDir\*" -Destination $targetPluginDir -Recurse -Force
    Copy-Item "$PSScriptRoot\OTelPlugin.json" -Destination $targetPluginDir -Force
    
    Write-Host "✓ Plugin copied to Neo CLI" -ForegroundColor Green
    
    # Start Neo node
    $neoCli = Join-Path $NeoCliPath "neo-cli.dll"
    if (-not (Test-Path $neoCli)) {
        Write-Host "✗ neo-cli.dll not found" -ForegroundColor Red
        return $false
    }
    
    Write-Host "  Starting Neo node..." -ForegroundColor Gray
    $process = Start-Process -FilePath "dotnet" -ArgumentList $neoCli -WorkingDirectory $NeoCliPath -PassThru -WindowStyle Hidden
    
    if ($process) {
        Write-Host "✓ Neo node started (PID: $($process.Id))" -ForegroundColor Green
        return $process
    } else {
        Write-Host "✗ Failed to start Neo node" -ForegroundColor Red
        return $null
    }
}

# Main execution
Write-Host "Step 1: Verifying plugin files" -ForegroundColor Cyan
Write-Host "-------------------------------" -ForegroundColor Cyan

if (-not (Test-PluginFiles)) {
    Write-Host ""
    Write-Host "❌ Plugin verification failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Checking prerequisites" -ForegroundColor Cyan
Write-Host "-------------------------------" -ForegroundColor Cyan

if (-not (Test-Prerequisites)) {
    Write-Host ""
    Write-Host "❌ Prerequisites check failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 3: Neo node startup test" -ForegroundColor Cyan
Write-Host "------------------------------" -ForegroundColor Cyan

$process = Start-NeoWithPlugin

if ($process) {
    Write-Host "  Waiting $WaitSeconds seconds for initialization..." -ForegroundColor Gray
    Start-Sleep -Seconds $WaitSeconds
    
    # Test metrics endpoint
    if (Test-MetricsEndpoint -Port $PrometheusPort) {
        Write-Host ""
        Write-Host "✅ SUCCESS: OpenTelemetry plugin is working!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "1. Access metrics at: http://localhost:$PrometheusPort/metrics" -ForegroundColor White
        Write-Host "2. Connect to Neo CLI and run: telemetry status" -ForegroundColor White
        Write-Host "3. Import Grafana dashboards from grafana-dashboards/" -ForegroundColor White
        Write-Host ""
        Write-Host "Neo node is running with PID: $($process.Id)" -ForegroundColor Yellow
        Write-Host "To stop: Stop-Process -Id $($process.Id)" -ForegroundColor Yellow
    } else {
        Write-Host ""
        Write-Host "⚠ WARNING: Plugin may not be fully initialized yet" -ForegroundColor Yellow
        Write-Host "Try accessing http://localhost:$PrometheusPort/metrics in a few moments" -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "❌ Failed to start Neo node" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan