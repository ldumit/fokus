namespace Fokus.Domain;

public class AppSettings
{
    public int Id { get; set; } = 1;
    public int? BoardId { get; set; }
    public List<string> DoneStatuses { get; set; } = ["Done", "Closed"];
    public List<string> WorkflowStages { get; set; } = [];
    public List<string> ExcludedFromScopeStatuses { get; set; } = [];
    public HealthThresholdConfig HealthThresholds { get; set; } = new();
    public HealthWeightConfig HealthWeights { get; set; } = new();
    public int BugRatioAlertThreshold { get; set; } = 50;
    public int BugRatioConsecutiveSprintCount { get; set; } = 2;

    public static AppSettings CreateDefault() => new()
    {
        Id = 1,
        BoardId = null,
        DoneStatuses = ["Done", "Closed"],
        WorkflowStages = [],
        ExcludedFromScopeStatuses = [],
        HealthThresholds = new HealthThresholdConfig(),
        HealthWeights = new HealthWeightConfig(),
        BugRatioAlertThreshold = 50,
        BugRatioConsecutiveSprintCount = 2
    };
}
