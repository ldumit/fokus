using Fokus.Domain.ValueObjects;

namespace Fokus.API.Features.Settings.SaveSettings;

public class SaveSettingsRequest
{
    public int? BoardId { get; set; }
    public List<string> DoneStatuses { get; set; } = [];
    public List<string> WorkflowStages { get; set; } = [];
    public HealthThresholdConfig HealthThresholds { get; set; } = new();
    public HealthWeightConfig HealthWeights { get; set; } = new();
}
