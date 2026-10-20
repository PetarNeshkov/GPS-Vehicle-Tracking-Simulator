param([string]$BaseUrl = 'http://localhost:5078')

$ErrorActionPreference = 'Stop'
$history = Invoke-RestMethod "$BaseUrl/api/vehicles/vehicle-1/history"
if ($history.samples.Count -ne 0) {
    throw 'Use a fresh API session with the simulator stopped before running this demonstration.'
}

$settings = @{ speedLimitKmh = 80; speedingDurationSeconds = 30; stopDurationSeconds = 20 } | ConvertTo-Json
Invoke-RestMethod "$BaseUrl/api/vehicles/vehicle-1/settings" -Method Put -ContentType 'application/json' -Body $settings | Out-Null

# Real GPS timestamps and ten-second reporting; shorten the stop threshold, not time.
$longitudes = @(23.310, 23.312, 23.312, 23.312, 23.312, 23.314, 23.316)
for ($index = 0; $index -lt $longitudes.Count; $index++) {
    if ($index -gt 0) { Start-Sleep -Seconds 10 }
    $speed = if ($index -ge 2 -and $index -le 4) { 0 } else { 40 }
    $sample = @{
        timestamp = [DateTimeOffset]::UtcNow.ToString('O')
        latitude = 42.697
        longitude = $longitudes[$index]
        speedKmh = $speed
        heading = 90
    } | ConvertTo-Json
    Invoke-RestMethod "$BaseUrl/api/vehicles/vehicle-1/samples" -Method Post -ContentType 'application/json' -Body $sample | Out-Null
    Write-Host "Sent observation $($index + 1)/$($longitudes.Count)"
}

$routes = Invoke-RestMethod "$BaseUrl/api/vehicles/vehicle-1/segments"
$history = Invoke-RestMethod "$BaseUrl/api/vehicles/vehicle-1/history"
if ($routes.segments.Count -ne 2 -or $history.samples.Count -ne 7) {
    throw 'Unexpected route/history counts. Check that no simulator is sending samples.'
}
Write-Host 'Verified: two separate routes and seven raw observations. Inspect the dashboard and history.'
