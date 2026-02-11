# Test script to verify the Program navigation property is loaded
Write-Host "Testing Program navigation property loading..." -ForegroundColor Cyan

# Start API in background
Write-Host "Starting API..." -ForegroundColor Yellow
$apiJob = Start-Job -ScriptBlock {
    Set-Location "D:\Programming\Langate\MedicaidApplicationAssistant\src\MAA.API"
    dotnet run
}

# Wait for API to start
Start-Sleep -Seconds 8

try {
    # Test the rules evaluation endpoint
    Write-Host "`nTesting eligibility evaluation..." -ForegroundColor Yellow
    
    $payload = @{
        stateCode = "TX"
        householdSize = 2
        monthlyIncomeCents = 150000
        age = 30
        hasDisability = $false
        isPregnant = $false
        receivesSsi = $false
        isCitizen = $true
        assetsCents = 100000
    } | ConvertTo-Json

    $response = Invoke-RestMethod `
        -Uri "http://localhost:5008/api/rules/evaluate" `
        -Method POST `
        -Body $payload `
        -ContentType "application/json"

    Write-Host "`n✓ Success!" -ForegroundColor Green
    Write-Host "  State: $($response.stateCode)"
    Write-Host "  Overall Status: $($response.overallStatus)"
    Write-Host "  Matched Programs: $($response.matchedPrograms.Count)"
    
    if ($response.matchedPrograms.Count -gt 0) {
        Write-Host "`n  Programs Found:" -ForegroundColor Green
        foreach ($program in $response.matchedPrograms) {
            if ($program.programName -eq "Unknown Program") {
                Write-Host "    ✗ $($program.programName) - ProgramId: $($program.programId)" -ForegroundColor Red
            } else {
                Write-Host "    ✓ $($program.programName) (Confidence: $($program.confidenceScore)%)" -ForegroundColor Green
            }
        }
    }

    if ($response.failedProgramEvaluations.Count -gt 0) {
        Write-Host "`n  Failed Evaluations: $($response.failedProgramEvaluations.Count)"
        foreach ($program in $response.failedProgramEvaluations | Select-Object -First 3) {
            Write-Host "    - $($program.programName)" -ForegroundColor Yellow
        }
    }

} catch {
    Write-Host "`n✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
} finally {
    # Stop API
    Write-Host "`nStopping API..." -ForegroundColor Yellow
    Stop-Job -Job $apiJob
    Remove-Job -Job $apiJob -Force
}

Write-Host "`nTest complete." -ForegroundColor Cyan
