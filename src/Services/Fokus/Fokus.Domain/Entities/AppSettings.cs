using Fokus.Domain.ValueObjects;

namespace Fokus.Domain.Entities;

public class AppSettings
{
    public int Id { get; set; } = 1;
    public int? BoardId { get; set; }
    public List<string> DoneStatuses { get; set; } = ["Done", "Closed"];
    public List<string> WorkflowStages { get; set; } = [];
    public HealthThresholdConfig HealthThresholds { get; set; } = new();
    public HealthWeightConfig HealthWeights { get; set; } = new();

    public static AppSettings CreateDefault() => new()
    {
        Id = 1,
        BoardId = null,
        DoneStatuses = ["Done", "Closed"],
        WorkflowStages = [],
        HealthThresholds = new HealthThresholdConfig(),
        HealthWeights = new HealthWeightConfig()
    };
}
