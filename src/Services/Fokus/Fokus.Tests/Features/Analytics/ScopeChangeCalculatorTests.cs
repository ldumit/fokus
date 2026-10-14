namespace Fokus.Tests.Features.Analytics;

public class ScopeChangeCalculatorTests
{
    private static readonly DateTime SprintStart = new(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SprintEnd = new(2026, 3, 11, 9, 0, 0, DateTimeKind.Utc);

    private readonly ScopeChangeCalculator _sut = new();
    private readonly AppSettings _settings = TestData.DefaultSettings();

    private static DateTime Day(int dayNumber, int hour = 10) =>
        SprintStart.Date.AddDays(dayNumber - 1).AddHours(hour);

    private static Sprint NewSprint() => TestData.ActiveSprint(startDate: SprintStart, endDate: SprintEnd);

    private static SprintMembership Bug(
        Sprint sprint, string key, decimal? sp, DateTime addedAt,
        DateTime? removedAt = null, string finalStatus = "In Progress") =>
        TestData.Membership(sprint.Id, key, storyPoints: sp, removedAt: removedAt,
            ticket: TestData.BugTicket(key), addedAt: addedAt, finalStatus: finalStatus);

    private List<BurnupDataPoint> Burnup(Sprint sprint, List<StatusTransition> transitions) =>
        _sut.ComputeSingleSprint(sprint, null, transitions, _settings, subTeam: null).BurnupData;

    // --- Slice 1: Feature and committed-total series are unchanged ---

    [Fact]
    public void Should_KeepFeatureAndCommittedSeries_When_FeaturesAndBugsAreAddedAndRemoved()
    {
        var sprint = NewSprint();
        var beforeStart = SprintStart.AddDays(-1);
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-1", storyPoints: 5m, addedAt: beforeStart));
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-2", storyPoints: 3m, addedAt: Day(3),
            removedAt: Day(6), wasCommitted: false));
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-3", storyPoints: 8m, addedAt: beforeStart,
            removedAt: Day(2)));
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-4", storyPoints: 2m, addedAt: Day(4),
            wasCommitted: false));
        sprint.AddMembership(Bug(sprint, "FOK-B1", null, beforeStart));
        sprint.AddMembership(Bug(sprint, "FOK-B2", 1m, Day(2), removedAt: Day(3)));

        var transitions = new List<StatusTransition>
        {
            TestData.Transition("FOK-1", "In Progress", Day(1), "To Do"),
            TestData.Transition("FOK-1", "Done", Day(3)),
            TestData.Transition("FOK-2", "In Progress", Day(4), "To Do"),
            TestData.Transition("FOK-3", "In Progress", Day(1, 11), "To Do"),
            TestData.Transition("FOK-4", "In Progress", Day(5), "To Do"),
            TestData.Transition("FOK-B1", "Done", Day(5)),
        };

        var burnup = Burnup(sprint, transitions);

        burnup.Select(p => p.TotalScopeSp).Should().Equal(5m, 5m, 5m, 5m, 7m, 7m, 7m, 7m, 7m, 7m);
        burnup.Select(p => p.TotalScopeTickets).Should().Equal(1, 1, 1, 1, 2, 2, 2, 2, 2, 2);
        burnup.Select(p => p.CompletedSp).Should().Equal(0m, 0m, 5m, 5m, 5m, 5m, 5m, 5m, 5m, 5m);
        burnup.Select(p => p.CompletedTickets).Should().Equal(0, 0, 1, 1, 1, 1, 1, 1, 1, 1);
        burnup.Select(p => p.CommittedTotalSp).Should().Equal(13m, 5m, 8m, 10m, 10m, 7m, 7m, 7m, 7m, 7m);
        burnup.Select(p => p.CommittedTotalTickets).Should().Equal(2, 1, 2, 3, 3, 2, 2, 2, 2, 2);
    }

    // --- Slice 2: Bug present at sprint start, removed mid-sprint ---

    [Fact]
    public void Should_CountBugUntilRemovalDay_When_BugPresentAtStartIsRemovedOnDay3()
    {
        var sprint = NewSprint();
        sprint.AddMembership(Bug(sprint, "FOK-B1", 2m, SprintStart.AddDays(-1), removedAt: Day(3, 12)));

        var burnup = Burnup(sprint, []);

        burnup.Select(p => p.BugSp).Should().Equal(2m, 2m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
        burnup.Select(p => p.BugTickets).Should().Equal(1, 1, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    // --- Slice 3: Bug added mid-sprint, removed later ---

    [Fact]
    public void Should_CountBugFromAddedDayToRemovalDay_When_BugAddedDay2AndRemovedDay4()
    {
        var sprint = NewSprint();
        sprint.AddMembership(Bug(sprint, "FOK-B1", 2m, Day(2), removedAt: Day(4)));

        var burnup = Burnup(sprint, []);

        burnup.Select(p => p.BugSp).Should().Equal(0m, 2m, 2m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
        burnup.Select(p => p.BugTickets).Should().Equal(0, 1, 1, 0, 0, 0, 0, 0, 0, 0);
    }

    // --- Slice 4: Bug completed, then removed — exits once ---

    [Fact]
    public void Should_ExitOnceOnCompletionDay_When_BugCompletedDay3ThenRemovedDay5()
    {
        var sprint = NewSprint();
        sprint.AddMembership(Bug(sprint, "FOK-B1", 2m, SprintStart.AddDays(-1), removedAt: Day(5)));
        var transitions = new List<StatusTransition> { TestData.Transition("FOK-B1", "Done", Day(3)) };

        var burnup = Burnup(sprint, transitions);

        burnup.Select(p => p.BugSp).Should().Equal(2m, 2m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
        burnup.Select(p => p.BugTickets).Should().Equal(1, 1, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    // --- Slice 5: Completion before entry — enters and exits the same day ---

    [Fact]
    public void Should_NeverGoNegative_When_BugCompletedDay2ButAddedDay4()
    {
        var sprint = NewSprint();
        sprint.AddMembership(Bug(sprint, "FOK-B1", 2m, Day(4)));
        var transitions = new List<StatusTransition> { TestData.Transition("FOK-B1", "Done", Day(2)) };

        var burnup = Burnup(sprint, transitions);

        burnup.Select(p => p.BugSp).Should().AllSatisfy(sp => sp.Should().Be(0m));
        burnup.Select(p => p.BugTickets).Should().AllSatisfy(count => count.Should().Be(0));
    }

    // --- Slice 6: Excluded final status never enters the area ---

    [Fact]
    public void Should_IgnoreBug_When_FinalStatusIsExcludedAndBugIsRemovedMidSprint()
    {
        _settings.ExcludedFromScopeStatuses = ["Cancelled"];
        var sprint = NewSprint();
        sprint.AddMembership(Bug(sprint, "FOK-B1", 2m, SprintStart.AddDays(-1), removedAt: Day(3),
            finalStatus: "Cancelled"));

        var burnup = Burnup(sprint, []);

        burnup.Select(p => p.BugSp).Should().AllSatisfy(sp => sp.Should().Be(0m));
        burnup.Select(p => p.BugTickets).Should().AllSatisfy(count => count.Should().Be(0));
    }
}
