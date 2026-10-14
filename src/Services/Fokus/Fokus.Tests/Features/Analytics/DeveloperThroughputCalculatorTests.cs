namespace Fokus.Tests.Features.Analytics;

public class DeveloperThroughputCalculatorTests
{
    private static readonly DateTime FirstSprintStart = new(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);

    private readonly DeveloperThroughputCalculator _sut = new();
    private readonly AppSettings _settings = TestData.DefaultSettings();
    private readonly Developer _developer = TestData.ActiveDeveloper("dev-1");
    private readonly List<StatusTransition> _transitions = [];

    private static Sprint ClosedSprint(int id)
    {
        var start = FirstSprintStart.AddDays((id - 1) * 14);
        var sprint = TestData.ActiveSprint(id, startDate: start, endDate: start.AddDays(13));
        sprint.State = SprintState.Closed;
        return sprint;
    }

    private void AddCompletedTicket(Sprint sprint, decimal storyPoints)
    {
        var key = $"FOK-{sprint.Id}";
        var ticket = TestData.FeatureTicket(key, assigneeId: _developer.Id);
        ticket.Assignee = _developer;
        sprint.AddMembership(TestData.Membership(sprint.Id, key, storyPoints: storyPoints, ticket: ticket,
            addedAt: sprint.StartDate));
        _transitions.Add(TestData.Transition(key, "In Progress", sprint.StartDate.AddDays(1), "To Do"));
        _transitions.Add(TestData.Transition(key, "Done", sprint.StartDate.AddDays(3)));
    }

    private decimal? RollingAverage(List<Sprint> loadedSprints, int targetSprintId, List<DeveloperSprintCapacity> capacities) =>
        _sut.ComputeThroughput(
                loadedSprints, [targetSprintId], [_developer], capacities, _settings,
                subTeam: null, allDevelopers: [_developer], _transitions)
            .Developers.Single().SprintBreakdowns.Single().RollingAverageSpCompleted;

    private static List<Sprint> LoadWindow(List<Sprint> ascendingClosed, IReadOnlyList<int> windowIds, int targetSprintId) =>
        ascendingClosed.Where(s => windowIds.Contains(s.Id) || s.Id == targetSprintId).ToList();

    private (List<Sprint> Ascending, Sprint Target, List<DeveloperSprintCapacity> Capacities) HistoryWithZeroCapacityP1()
    {
        var p3 = ClosedSprint(1);
        var p2 = ClosedSprint(2);
        var p1 = ClosedSprint(3);
        var target = ClosedSprint(4);
        AddCompletedTicket(p3, 4m);
        AddCompletedTicket(p2, 3m);
        AddCompletedTicket(p1, 10m);
        AddCompletedTicket(target, 5m);
        var capacities = new List<DeveloperSprintCapacity>
        {
            new() { DeveloperAccountId = _developer.Id, SprintId = p1.Id, CapacityPercent = 0 }
        };
        return ([p3, p2, p1, target], target, capacities);
    }

    // --- Slice 1: Window reaches past a 0%-capacity sprint ---

    [Fact]
    public void Should_AverageTargetAndTwoEarlierQualifyingSprints_When_PriorSprintHasZeroCapacity()
    {
        var (ascending, target, capacities) = HistoryWithZeroCapacityP1();

        var windowIds = DeveloperThroughputCalculator.SelectRollingWindowSprintIds(ascending, [target.Id]);
        var rollingAverage = RollingAverage(LoadWindow(ascending, windowIds, target.Id), target.Id, capacities);

        rollingAverage.Should().Be(4m);
    }

    // --- Slice 2: The old two-sprint window loses the average ---

    [Fact]
    public void Should_ReturnNull_When_OnlyTwoSprintsBeforeTargetAreLoaded()
    {
        var (ascending, target, capacities) = HistoryWithZeroCapacityP1();

        var twoSprintWindow = ascending.Skip(1).ToList();
        var rollingAverage = RollingAverage(twoSprintWindow, target.Id, capacities);

        rollingAverage.Should().BeNull();
    }

    // --- Slice 3: Fewer than three qualifying sprints in the whole history ---

    [Fact]
    public void Should_ReturnNull_When_HistoryHoldsFewerThanThreeQualifyingSprints()
    {
        var p2 = ClosedSprint(1);
        var p1 = ClosedSprint(2);
        var target = ClosedSprint(3);
        AddCompletedTicket(p2, 3m);
        AddCompletedTicket(p1, 10m);
        AddCompletedTicket(target, 5m);
        var capacities = new List<DeveloperSprintCapacity>
        {
            new() { DeveloperAccountId = _developer.Id, SprintId = p1.Id, CapacityPercent = 0 }
        };
        List<Sprint> ascending = [p2, p1, target];

        var windowIds = DeveloperThroughputCalculator.SelectRollingWindowSprintIds(ascending, [target.Id]);
        var rollingAverage = RollingAverage(LoadWindow(ascending, windowIds, target.Id), target.Id, capacities);

        rollingAverage.Should().BeNull();
    }
}
