namespace Fokus.Domain.ValueObjects;

public class HealthWeightConfig
{
    public int Completion { get; set; } = 40;
    public int Disruption { get; set; } = 30;
    public int CarryOver { get; set; } = 30;
}
