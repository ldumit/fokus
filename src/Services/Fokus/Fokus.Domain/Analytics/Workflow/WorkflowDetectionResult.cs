namespace Fokus.Domain.Analytics.Workflow;

public record WorkflowDetectionResult(
    List<string> Stages,
    List<string> Sidelined,
    int TransitionCount,
    int TicketCount,
    int SprintCount);
