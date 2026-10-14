namespace Fokus.Tests.Features.Analytics;

public class SprintSummaryCalculatorTests
{
    private static readonly DateTime FirstSprintStart = new(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);

    private readonly SprintSummaryCalculator _sut = new();
    private readonly AppSettings _settings = TestData.DefaultSettings();

    private static Sprint ClosedSprint(int id)
    {
        var start = FirstSprintStart.AddDays((id - 1) * 14);
        var sprint = TestData.ActiveSprint(id, startDate: start, endDate: start.AddDays(13));
        sprint.State = SprintState.Closed;
        return sprint;
    }

    private static void AddTicket(Sprint sprint, string key, Developer? assignee = null)
    {
        var ticket = TestData.FeatureTicket(key, assigneeId: assignee?.Id);
        ticket.Assignee = assignee!;
        sprint.AddMembership(TestData.Membership(sprint.Id, key, ticket: ticket, addedAt: sprint.StartDate));
    }

    private List<ZombieTicket> Zombies(
        Sprint selected, List<Sprint> window, Dictionary<string, int> zombieSprintCounts, string? subTeam = null) =>
        _sut.ComputeSummary(
            selected, window, activeDevelopers: [], _settings, subTeam,
            allEpicTickets: [], capacityLookup: new(), allDevelopers: [], statusTransitions: [],
            zombieSprintCounts).Flags.ZombieTickets;

    // --- Slice 1: Count comes from the whole history, not the window ---

    [Fact]
    public void Should_FlagWithHistoryCount_When_WindowHoldsOnlyOneOfTheTicketsSprints()
    {
        var selected = ClosedSprint(1);
        AddTicket(selected, "FOK-1");

        var zombies = Zombies(selected, [selected], new() { ["FOK-1"] = 6 });

        zombies.Should().ContainSingle().Which.Should().BeEquivalentTo(new { TicketKey = "FOK-1", SprintCount = 6 });
    }

    // --- Slice 2: Candidate set stays the window's tickets ---

    [Fact]
    public void Should_FlagWindowTicket_When_TicketIsAbsentFromSelectedSprintButInHistory()
    {
        var prior = ClosedSprint(1);
        var selected = ClosedSprint(2);
        AddTicket(prior, "FOK-2");
        AddTicket(selected, "FOK-1");

        var zombies = Zombies(selected, [prior, selected], new() { ["FOK-2"] = 4 });

        zombies.Should().ContainSingle().Which.TicketKey.Should().Be("FOK-2");
    }

    // --- Slice 3: Ordered by true count, descending ---

    [Fact]
    public void Should_OrderZombiesByHistoryCountDescending_When_TwoZombiesInWindow()
    {
        var selected = ClosedSprint(1);
        AddTicket(selected, "FOK-1");
        AddTicket(selected, "FOK-2");

        var zombies = Zombies(selected, [selected], new() { ["FOK-1"] = 4, ["FOK-2"] = 6 });

        zombies.Select(z => (z.TicketKey, z.SprintCount)).Should().Equal(("FOK-2", 6), ("FOK-1", 4));
    }

    // --- Slice 4: Absent from the history counts — not a zombie ---

    [Fact]
    public void Should_NotFlag_When_WindowTicketIsAbsentFromHistoryCounts()
    {
        var sprints = new List<Sprint> { ClosedSprint(1), ClosedSprint(2), ClosedSprint(3) };
        sprints.ForEach(s => AddTicket(s, "FOK-1"));

        var zombies = Zombies(sprints[2], sprints, new());

        zombies.Should().BeEmpty();
    }

    // --- Slice 5: Sub-team filter still applies to candidates ---

    [Fact]
    public void Should_HideZombie_When_TicketBelongsToAnotherSubTeam()
    {
        var alpha = TestData.ActiveDeveloper("dev-a", "Dev A", subTeam: "Alpha");
        var beta = TestData.ActiveDeveloper("dev-b", "Dev B", subTeam: "Beta");
        var selected = ClosedSprint(1);
        AddTicket(selected, "FOK-1", beta);
        AddTicket(selected, "FOK-2", alpha);

        var zombies = Zombies(selected, [selected], new() { ["FOK-1"] = 5, ["FOK-2"] = 3 }, subTeam: "Alpha");

        zombies.Should().ContainSingle().Which.TicketKey.Should().Be("FOK-2");
    }
}
