$names = @("AppShell", "WorkflowAutoDetection", "SprintSummaryCard", "DeveloperThroughput")
$labels = @("F7 - App Shell", "F6 - Workflow Auto-Detection", "F8 - Sprint Summary Card", "F9 - Developer Throughput")

$logDir = "logs\overnight-$(Get-Date -Format 'yyyy-MM-dd')"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

Write-Host "Starting overnight pipeline at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan

for ($i = 0; $i -lt $names.Count; $i++) {
    $feature = $names[$i]
    $label = $labels[$i]
    $logFile = Join-Path $logDir "$feature.log"

    Write-Host "=== [$label] Starting at $(Get-Date -Format 'HH:mm:ss') ===" -ForegroundColor Cyan

    Start-Transcript -Path $logFile -Force | Out-Null

    $prompt = "[UNATTENDED] be team-lead. Implement feature $feature from docs/features/$feature.md. Run the full pipeline: architect plans, developer implements, architect does Step 1 done check, reviewer does Step 2 code review. Follow .claude/rules/agents-workflow.md."

    claude -p $prompt --permission-mode dontAsk --model opus

    Stop-Transcript | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Host "=== [$label] FAILED (exit code $LASTEXITCODE) - aborting ===" -ForegroundColor Red
        break
    }

    Write-Host "=== [$label] Finished at $(Get-Date -Format 'HH:mm:ss') ===" -ForegroundColor Green
}

Write-Host "Pipeline complete at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'). Logs in $logDir"
