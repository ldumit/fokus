namespace Fokus.Domain.ValueObjects;

public class HealthThresholdConfig
{
    public decimal CompletionGreen { get; set; } = 80;
    public decimal CompletionAmber { get; set; } = 60;
    public decimal DisruptionGreen { get; set; } = 10;
    public decimal DisruptionAmber { get; set; } = 25;
    public decimal CarryOverGreen { get; set; } = 10;
    public decimal CarryOverAmber { get; set; } = 25;
}
