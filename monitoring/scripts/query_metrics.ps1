# Simple script to query the Neo node Prometheus metrics endpoint using PowerShell.

# --- Configuration ---
$HostAddress = "127.0.0.1" # Replace with your node's IP address if not running locally
$Port = 9100               # Replace with the port configured in your Neo node's config.json
$Endpoint = "http://${HostAddress}:${Port}/metrics"

# --- Script Logic ---
Write-Host "Querying Neo node metrics at: $Endpoint"
Write-Host "-------------------------------------------"

try {
    # Use Invoke-RestMethod to fetch the metrics
    $response = Invoke-RestMethod -Uri $Endpoint -Method Get -ErrorAction Stop

    # Output the raw metrics (Prometheus format is plain text)
    Write-Host $response

    Write-Host "`n-------------------------------------------"
    Write-Host "Metrics retrieved successfully."
}
catch {
    Write-Error "Failed to retrieve metrics from $Endpoint. Error: $_"
    Write-Host "Please ensure the Neo node is running and the Prometheus service is enabled and configured correctly."
    # Exit with a non-zero status code to indicate failure
    exit 1
} 