namespace Fokus.API.Features.Settings.GetSettings;

public class GetSettingsResponse
{
    public int? BoardId { get; set; }
    public List<string> DoneStatuses { get; set; } = [];
    public List<string> WorkflowStages { get; set; } = [];
    public HealthThresholdConfig HealthThresholds { get; set; } = new();
    public HealthWeightConfig HealthWeights { get; set; } = new();
    public int BugRatioAlertThreshold { get; set; }
    public int BugRatioConsecutiveSprintCount { get; set; }
    public int SyncBackSprintCount { get; set; }
    public int PlanningWindowDays { get; set; }
    public int DefaultSpPerBug { get; set; }
    public bool XrayEnabled { get; set; }
    public string? XrayClientId { get; set; }
    public string? XrayClientSecret { get; set; }
}
