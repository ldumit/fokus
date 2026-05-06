using Fokus.API.Infrastructure.Jira.Dtos;
using Fokus.Domain.Entities;
using Fokus.Domain.Enums;

namespace Fokus.API.Infrastructure.Jira.Mapping;

public static class JiraMapper
{
    public static Sprint MapSprint(JiraSprint dto, string boardName)
    {
        var state = dto.State.ToLowerInvariant() switch
        {
            "active" => SprintState.Active,
            "closed" => SprintState.Closed,
            "future" => SprintState.Future,
            _ => SprintState.Active
        };

        return new Sprint
        {
            Id = dto.Id,
            Name = dto.Name,
            StartDate = dto.StartDate ?? DateTime.UtcNow,
            EndDate = dto.EndDate ?? DateTime.UtcNow.AddDays(14),
            BoardId = dto.OriginBoardId,
            BoardName = boardName,
            State = state,
            SyncedAt = DateTime.UtcNow
        };
    }

    public static Ticket MapTicket(JiraIssue dto)
    {
        return new Ticket
        {
            Key = dto.Key,
            Summary = dto.Fields.Summary,
            IssueType = dto.Fields.Issuetype?.Name ?? "Unknown",
            StoryPoints = dto.Fields.StoryPoints,
            EpicKey = dto.Fields.EpicKey,
            EpicName = dto.Fields.EpicName,
            AssigneeId = dto.Fields.Assignee?.AccountId,
            Priority = dto.Fields.Priority?.Name ?? "Medium",
            CurrentStatus = dto.Fields.Status?.Name ?? "Unknown",
            CreatedDate = dto.Fields.Created ?? DateTime.UtcNow,
            ResolvedDate = dto.Fields.Resolutiondate
        };
    }

    public static Developer? MapDeveloper(JiraIssue dto)
    {
        if (dto.Fields.Assignee is null) return null;

        var assignee = dto.Fields.Assignee;
        var avatarUrl = assignee.AvatarUrls.TryGetValue("48x48", out var url) ? url : null;

        return new Developer
        {
            AccountId = assignee.AccountId,
            DisplayName = assignee.DisplayName,
            AvatarUrl = avatarUrl,
            IsActive = true
        };
    }

    public static SprintMembership MapMembership(JiraIssue dto, Sprint sprint, bool forcedNotCommitted = false)
    {
        var sprintIdStr = sprint.Id.ToString();

        // Find earliest changelog entry where sprint field changed TO include this sprint
        DateTime? addedAt = null;
        DateTime? removedAt = null;

        foreach (var history in dto.Changelog.Histories.OrderBy(h => h.Created))
        {
            foreach (var item in history.Items.Where(i => i.Field == "Sprint" || i.Field == "sprint"))
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
            TicketKey = dto.Key,
            AddedAt = effectiveAddedAt,
            RemovedAt = removedAt,
            WasCommitted = wasCommitted,
            FinalStatus = dto.Fields.Status?.Name ?? "Unknown",
            StoryPoints = dto.Fields.StoryPoints
        };
    }

    public static List<StatusTransition> MapStatusTransitions(JiraIssue dto)
    {
        var transitions = new List<StatusTransition>();

        foreach (var history in dto.Changelog.Histories.OrderBy(h => h.Created))
        {
            foreach (var item in history.Items.Where(i => i.Field == "status"))
            {
                transitions.Add(new StatusTransition
                {
                    TicketKey = dto.Key,
                    FromStatus = item.FromString ?? string.Empty,
                    ToStatus = item.ToStringValue ?? string.Empty,
                    Timestamp = history.Created,
                    AuthorId = history.Author.AccountId
                });
            }
        }

        return transitions;
    }
}
