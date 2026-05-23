namespace Fokus.Tests._Fixtures;

public static class TestData
{
    public static AppSettings DefaultSettings() => new()
    {
        Id = 1,
        BoardId = 1,
        DoneStatuses = ["Done"],
        WorkflowStages = ["In Progress"],
        ExcludedFromScopeStatuses = [],
        DefaultSpPerBug = 3,
        CycleTimeStartStage = "In Progress",
        CycleTimeEndStage = "Done",
        PlanningWindowDays = 2
    };

    public static Sprint ActiveSprint(
        int id = 1,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.Date.AddDays(-5);
        var end = endDate ?? DateTime.UtcNow.Date.AddDays(9);
        return new Sprint
        {
            Id = id,
            Name = $"Sprint {id}",
            StartDate = start,
            EndDate = end,
            BoardId = 1,
            BoardName = "Test Board",
            State = SprintState.Active,
            SyncedAt = DateTime.UtcNow
        };
    }

    public static Developer ActiveDeveloper(string id = "dev-1", string displayName = "Dev One", string? subTeam = null) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            IsActive = true,
            DefaultCapacityPercent = 100,
            SubTeam = subTeam
        };

    public static Ticket FeatureTicket(string key = "FOK-1", string? assigneeId = null) =>
        new()
        {
            Id = key,
            Summary = $"Feature {key}",
            IssueType = "Story",
            Priority = "Medium",
            CurrentStatus = "In Progress",
            CreatedDate = DateTime.UtcNow.AddDays(-10),
            AssigneeId = assigneeId
        };

    public static Ticket BugTicket(string key = "FOK-B1", string? assigneeId = null) =>
        new()
        {
            Id = key,
            Summary = $"Bug {key}",
            IssueType = "Bug",
            Priority = "High",
            CurrentStatus = "In Progress",
            CreatedDate = DateTime.UtcNow.AddDays(-3),
            AssigneeId = assigneeId
        };

    public static SprintMembership Membership(
        int sprintId,
        string ticketId,
        decimal? storyPoints = 3m,
        DateTime? removedAt = null,
        Ticket? ticket = null) =>
        new()
        {
            SprintId = sprintId,
            TicketId = ticketId,
            AddedAt = DateTime.UtcNow.AddDays(-5),
            RemovedAt = removedAt,
            WasCommitted = true,
            FinalStatus = "In Progress",
            StoryPoints = storyPoints,
            Ticket = ticket ?? FeatureTicket(ticketId)
        };

    public static StatusTransition Transition(
        string ticketId,
        string toStatus,
        DateTime timestamp,
        string fromStatus = "In Progress") =>
        new()
        {
            Id = 0,
            TicketId = ticketId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Timestamp = timestamp
        };
}
