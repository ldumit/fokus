namespace Fokus.Domain;

public partial class SprintMembership
{
    public static SprintMembership FromJira(JiraIssue dto, Sprint sprint, bool forcedNotCommitted = false)
    {
        var sprintIdStr = sprint.Id.ToString();

        DateTime? addedAt = null;
        DateTime? removedAt = null;

        var histories = dto.Changelog?.Histories;
        if (histories is not null)
        foreach (var history in histories.OrderBy(h => h.Created))
        {
            foreach (var item in history.Items.Where(i => i.Field is "Sprint" or "sprint"))
            {
                var toSprints = item.ToStringValue?.Split(',').Select(s => s.Trim()) ?? [];
                var fromSprints = item.FromString?.Split(',').Select(s => s.Trim()) ?? [];

                var appearsInTo = toSprints.Any(s => s == sprintIdStr);
                var appearsInFrom = fromSprints.Any(s => s == sprintIdStr);

                if (appearsInTo && !appearsInFrom && addedAt is null)
                    addedAt = history.Created;

                if (appearsInFrom && !appearsInTo && removedAt is null)
                    removedAt = history.Created;
            }
        }

        var effectiveAddedAt = addedAt ?? sprint.StartDate;
        var wasCommitted = !forcedNotCommitted && effectiveAddedAt <= sprint.StartDate;

        return new SprintMembership
        {
            SprintId = sprint.Id,
            TicketId = dto.Key,
            AddedAt = effectiveAddedAt,
            RemovedAt = removedAt,
            WasCommitted = wasCommitted,
            FinalStatus = dto.Fields.Status?.Name ?? "Unknown",
            StoryPoints = dto.Fields.StoryPoints
        };
    }
}
