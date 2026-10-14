namespace Fokus.Domain.Analytics.Workflow;

public record TransitionEdge(string FromStatus, string ToStatus, int Count);

public record TransitionEdgeData(
    List<TransitionEdge> Edges,
    int TransitionCount,
    int TicketCount,
    int SprintCount);
