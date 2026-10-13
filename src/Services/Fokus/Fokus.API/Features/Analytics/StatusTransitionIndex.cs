namespace Fokus.API.Features.Analytics;

/// <summary>
/// Status transitions grouped by ticket id, built once from a transition list so per-ticket lookups
/// do not rescan the whole list. Keys compare ordinally (case-sensitive); each ticket's transitions
/// keep their order from the source list.
/// </summary>
public sealed class StatusTransitionIndex
{
    private static readonly IReadOnlyList<StatusTransition> NoTransitions = [];

    private readonly Dictionary<string, List<StatusTransition>> _byTicketId;

    private StatusTransitionIndex(Dictionary<string, List<StatusTransition>> byTicketId) =>
        _byTicketId = byTicketId;

    public static StatusTransitionIndex Build(IEnumerable<StatusTransition> transitions)
    {
        ArgumentNullException.ThrowIfNull(transitions);

        var byTicketId = new Dictionary<string, List<StatusTransition>>(StringComparer.Ordinal);
        foreach (var transition in transitions)
        {
            if (!byTicketId.TryGetValue(transition.TicketId, out var ticketTransitions))
            {
                ticketTransitions = [];
                byTicketId[transition.TicketId] = ticketTransitions;
            }
            ticketTransitions.Add(transition);
        }

        return new StatusTransitionIndex(byTicketId);
    }

    public IReadOnlyList<StatusTransition> ForTicket(string ticketId) =>
        _byTicketId.TryGetValue(ticketId, out var ticketTransitions) ? ticketTransitions : NoTransitions;
}
