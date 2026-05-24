using Fokus.Tests._Fixtures;

namespace Fokus.Tests.Features.Analytics;

public class DeveloperProgressServiceTests
{
    private readonly DeveloperProgressService _sut = new();
    private readonly AppSettings _settings = TestData.DefaultSettings();

    // --- Slice 1: No active sprint ---

    [Fact]
    public void Should_ReturnHasActiveSprintFalse_When_NoActiveSprint()
    {
        var result = _sut.ComputeProgress(
            activeSprint: null,
            activeDevelopers: [],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [],
            statusTransitions: [],
            excludedDeveloperIds: []);

        result.HasActiveSprint.Should().BeFalse();
        result.Sprint.Should().BeNull();
        result.Developers.Should().BeEmpty();
        result.Alerts.Should().BeEmpty();
        result.CurrentDay.Should().Be(0);
        result.TotalDays.Should().Be(0);
    }

    // --- Slice 2: Basic pace and daily breakdown ---

    [Fact]
    public void Should_ComputePaceAndBreakdown_When_DeveloperCompletesTicketOnDay3()
    {
        // Sprint: 10 days, started 4 days ago (so currentDay = 5)
        var sprintStart = DateTime.UtcNow.Date.AddDays(-4);
        var sprintEnd = sprintStart.AddDays(9); // 10 days inclusive
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1");
        var ticket = TestData.FeatureTicket("FOK-1", assigneeId: "dev-1");
        ticket.Assignee = dev;
        var membership = TestData.Membership(sprint.Id, "FOK-1", storyPoints: 5m, ticket: ticket);
        sprint.AddMembership(membership);

        // Ticket completed on day 3 (sprintStart + 2 days)
        var completionTime = sprintStart.AddDays(2).AddHours(14);
        var transitions = new List<StatusTransition>
        {
            TestData.Transition("FOK-1", "In Progress", sprintStart.AddHours(8), "To Do"),
            TestData.Transition("FOK-1", "Done", completionTime, "In Progress")
        };

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: transitions,
            excludedDeveloperIds: []);

        result.HasActiveSprint.Should().BeTrue();
        result.TotalDays.Should().Be(10);
        result.CurrentDay.Should().Be(5);

        var devEntry = result.Developers.Should().ContainSingle().Subject;
        devEntry.AccountId.Should().Be("dev-1");
        devEntry.AssignedSp.Should().Be(5m);
        devEntry.CompletedSp.Should().Be(5m);
        devEntry.CompletionPercent.Should().Be(100m);
        devEntry.CapacityPercent.Should().Be(100);
        // dailyPace = 5 * 100/100 / 10 = 0.5 SP/day
        devEntry.DailyPace.Should().Be(0.5m);

        // Daily breakdown: 5 days (currentDay = 5), ticket completed on day 3
        devEntry.DailyBreakdown.Should().HaveCount(5);
        var day3 = devEntry.DailyBreakdown.Single(d => d.Day == 3);
        day3.CompletedTickets.Should().ContainSingle(t => t.Key == "FOK-1");
        day3.CumulativeSp.Should().Be(5m);
        day3.ExpectedCumulativeSp.Should().Be(1.5m); // 0.5 * 3

        // Days 1 and 2 have no completions, cumulative = 0
        devEntry.DailyBreakdown.Single(d => d.Day == 1).CumulativeSp.Should().Be(0m);
        devEntry.DailyBreakdown.Single(d => d.Day == 2).CumulativeSp.Should().Be(0m);
        // Days 4 and 5 inherit day 3 cumulative
        devEntry.DailyBreakdown.Single(d => d.Day == 4).CumulativeSp.Should().Be(5m);
        devEntry.DailyBreakdown.Single(d => d.Day == 5).CumulativeSp.Should().Be(5m);
    }

    // --- Slice 3: Zero assigned SP ---

    [Fact]
    public void Should_ReturnZeroPace_When_DeveloperHasNoAssignedSp()
    {
        var sprint = TestData.ActiveSprint();
        var dev = TestData.ActiveDeveloper("dev-1");

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: [],
            excludedDeveloperIds: []);

        var devEntry = result.Developers.Should().ContainSingle().Subject;
        devEntry.AssignedSp.Should().Be(0m);
        devEntry.DailyPace.Should().Be(0m);
        devEntry.CompletionPercent.Should().Be(0m);
        devEntry.IsBehindPace.Should().BeFalse();
    }

    // --- Slice 4: Grace period suppresses behind-pace ---

    [Fact]
    public void Should_SuppressBehindPace_When_WithinGracePeriod()
    {
        // Sprint started today — currentDay = 1, grace period
        var sprintStart = DateTime.UtcNow.Date;
        var sprintEnd = sprintStart.AddDays(9);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1");
        var ticket = TestData.FeatureTicket("FOK-1", assigneeId: "dev-1");
        ticket.Assignee = dev;
        var membership = TestData.Membership(sprint.Id, "FOK-1", storyPoints: 10m, ticket: ticket);
        sprint.AddMembership(membership);

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: [], // nothing completed
            excludedDeveloperIds: []);

        result.IsGracePeriod.Should().BeTrue();
        result.Alerts.Should().BeEmpty();
        var devEntry = result.Developers.Single();
        devEntry.IsBehindPace.Should().BeFalse();
        devEntry.PaceGapSp.Should().BeNull();
    }

    // --- Slice 5: Behind-pace detection after grace period ---

    [Fact]
    public void Should_DetectBehindPace_When_AfterGracePeriodAndBehindByMoreThanOneDay()
    {
        // Sprint: 10 days, started 3 days ago -> currentDay = 4, not grace period
        var sprintStart = DateTime.UtcNow.Date.AddDays(-3);
        var sprintEnd = sprintStart.AddDays(9);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1", "Dev One");
        var ticket = TestData.FeatureTicket("FOK-1", assigneeId: "dev-1");
        ticket.Assignee = dev;
        var membership = TestData.Membership(sprint.Id, "FOK-1", storyPoints: 10m, ticket: ticket);
        sprint.AddMembership(membership);

        // Developer has completed 0 SP
        // dailyPace = 10 / 10 = 1.0 SP/day
        // expectedCumulative(4) = 4.0, completedSp = 0
        // paceGapSp = 4.0 - 0 = 4.0 > dailyPace (1.0) → isBehindPace = true
        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: [],
            excludedDeveloperIds: []);

        result.IsGracePeriod.Should().BeFalse();
        var devEntry = result.Developers.Single();
        devEntry.DailyPace.Should().Be(1.0m);
        devEntry.IsBehindPace.Should().BeTrue();
        devEntry.PaceGapSp.Should().Be(4.0m);

        // Alert should be generated
        result.Alerts.Should().ContainSingle(a => a.AccountId == "dev-1");
        var alert = result.Alerts.Single();
        alert.GapSp.Should().Be(4.0m);
        alert.GapDays.Should().Be(4.0m); // 4.0 / 1.0 = 4.0 days
    }

    // --- Slice 6: Stall detection ---

    [Fact]
    public void Should_DetectStalledTicket_When_LastTransitionMoreThan2BusinessDaysAgo()
    {
        // Monday-based stall: transition was last Thursday, today is Wednesday next week
        // BusinessDays(Thursday → Wednesday) = Thu→Fri = 1 day, Mon→Tue = 2 days, Tue→Wed = 3 days = 3 business days > 2
        var today = DateTime.UtcNow.Date;

        var sprintStart = today.AddDays(-10);
        var sprintEnd = today.AddDays(4);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1");
        var ticket = TestData.FeatureTicket("FOK-1", assigneeId: "dev-1");
        ticket.Assignee = dev;
        var membership = TestData.Membership(sprint.Id, "FOK-1", storyPoints: 3m, ticket: ticket);
        sprint.AddMembership(membership);

        // Ticket started (transitioned to In Progress during sprint) but never completed
        // Last transition: 5 days ago (well past 2 business days in any week scenario)
        var lastTransition = today.AddDays(-5).AddHours(10);
        var transitions = new List<StatusTransition>
        {
            TestData.Transition("FOK-1", "In Progress", sprintStart.AddDays(1).AddHours(9), "To Do"),
            TestData.Transition("FOK-1", "In Review", lastTransition, "In Progress")
        };

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: transitions,
            excludedDeveloperIds: []);

        var devEntry = result.Developers.Single();
        devEntry.StalledTickets.Should().ContainSingle(t => t.Key == "FOK-1");
        var stalled = devEntry.StalledTickets.Single();
        stalled.DaysSinceLastTransition.Should().BeGreaterThan(2);
    }

    // --- Slice 7: All ticket types included (not feature-only) ---

    [Fact]
    public void Should_IncludeBugSp_When_ComputingAssignedAndCompletedSp()
    {
        var sprintStart = DateTime.UtcNow.Date.AddDays(-4);
        var sprintEnd = sprintStart.AddDays(9);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1");

        var featureTicket = TestData.FeatureTicket("FOK-1", assigneeId: "dev-1");
        featureTicket.Assignee = dev;
        var bugTicket = TestData.BugTicket("FOK-B1", assigneeId: "dev-1");
        bugTicket.Assignee = dev;

        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-1", storyPoints: 3m, ticket: featureTicket));
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-B1", storyPoints: null, ticket: bugTicket)); // null SP → uses DefaultSpPerBug=3

        var completionTime = sprintStart.AddDays(2).AddHours(14);
        var transitions = new List<StatusTransition>
        {
            TestData.Transition("FOK-1", "Done", completionTime, "In Progress"),
            TestData.Transition("FOK-B1", "Done", completionTime, "In Progress")
        };

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: transitions,
            excludedDeveloperIds: []);

        var devEntry = result.Developers.Single();
        devEntry.AssignedSp.Should().Be(6m);   // 3 (feature) + 3 (bug default)
        devEntry.CompletedSp.Should().Be(6m);
    }

    // --- Slice 8: Sub-team filter ---

    [Fact]
    public void Should_FilterBySubTeam_When_SubTeamSpecified()
    {
        var sprint = TestData.ActiveSprint();
        var devA = TestData.ActiveDeveloper("dev-a", "Dev A", subTeam: "Alpha");
        var devB = TestData.ActiveDeveloper("dev-b", "Dev B", subTeam: "Beta");

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [devA, devB],
            capacityRecords: [],
            settings: _settings,
            subTeam: "Alpha",
            allDevelopers: [devA, devB],
            statusTransitions: [],
            excludedDeveloperIds: []);

        result.Developers.Should().ContainSingle(d => d.AccountId == "dev-a");
        result.Developers.Should().NotContain(d => d.AccountId == "dev-b");
    }

    // --- Slice 9: Excluded developer IDs ---

    [Fact]
    public void Should_ExcludeDeveloper_When_InExcludedDeveloperIds()
    {
        var sprint = TestData.ActiveSprint();
        var dev = TestData.ActiveDeveloper("dev-1");

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: [],
            excludedDeveloperIds: ["dev-1"]);

        result.Developers.Should().BeEmpty();
    }

    // ============================================================
    // Step 1: Bug/feature SP partition
    // ============================================================

    // --- Slice 10: Mixed bugs and features — invariant holds ---

    [Fact]
    public void Should_PartitionCompletedSp_When_DeveloperCompletesBothBugsAndFeatures()
    {
        var sprintStart = DateTime.UtcNow.Date.AddDays(-4);
        var sprintEnd = sprintStart.AddDays(9);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1");

        var featureTicket = TestData.FeatureTicket("FOK-1", assigneeId: "dev-1");
        featureTicket.Assignee = dev;
        var bugTicket = TestData.BugTicket("FOK-B1", assigneeId: "dev-1");
        bugTicket.Assignee = dev;

        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-1", storyPoints: 5m, ticket: featureTicket));
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-B1", storyPoints: 3m, ticket: bugTicket));

        var completionTime = sprintStart.AddDays(2).AddHours(14);
        var transitions = new List<StatusTransition>
        {
            TestData.Transition("FOK-1", "Done", completionTime, "In Progress"),
            TestData.Transition("FOK-B1", "Done", completionTime, "In Progress")
        };

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: transitions,
            excludedDeveloperIds: []);

        var devEntry = result.Developers.Single();
        devEntry.CompletedSp.Should().Be(8m);
        devEntry.FeatureCompletedSp.Should().Be(5m);
        devEntry.BugCompletedSp.Should().Be(3m);
        (devEntry.FeatureCompletedSp + devEntry.BugCompletedSp).Should().Be(devEntry.CompletedSp);
    }

    // --- Slice 11: Only bugs completed — featureCompletedSp is zero ---

    [Fact]
    public void Should_ZeroFeatureSp_When_DeveloperCompletesOnlyBugs()
    {
        var sprintStart = DateTime.UtcNow.Date.AddDays(-4);
        var sprintEnd = sprintStart.AddDays(9);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1");
        var bugTicket = TestData.BugTicket("FOK-B1", assigneeId: "dev-1");
        bugTicket.Assignee = dev;
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-B1", storyPoints: 4m, ticket: bugTicket));

        var completionTime = sprintStart.AddDays(1).AddHours(10);
        var transitions = new List<StatusTransition>
        {
            TestData.Transition("FOK-B1", "Done", completionTime, "In Progress")
        };

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: transitions,
            excludedDeveloperIds: []);

        var devEntry = result.Developers.Single();
        devEntry.FeatureCompletedSp.Should().Be(0m);
        devEntry.BugCompletedSp.Should().Be(4m);
    }

    // --- Slice 12: Nothing completed — both partitions zero ---

    [Fact]
    public void Should_ZeroBothPartitions_When_DeveloperCompletesNothing()
    {
        var sprint = TestData.ActiveSprint();
        var dev = TestData.ActiveDeveloper("dev-1");
        var ticket = TestData.FeatureTicket("FOK-1", assigneeId: "dev-1");
        ticket.Assignee = dev;
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-1", storyPoints: 5m, ticket: ticket));

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: [],
            excludedDeveloperIds: []);

        var devEntry = result.Developers.Single();
        devEntry.FeatureCompletedSp.Should().Be(0m);
        devEntry.BugCompletedSp.Should().Be(0m);
    }

    // --- Slice 13: Null-SP bug uses DefaultSpPerBug in bug partition ---

    [Fact]
    public void Should_UseDefaultSpPerBug_When_BugHasNullSp()
    {
        var sprintStart = DateTime.UtcNow.Date.AddDays(-4);
        var sprintEnd = sprintStart.AddDays(9);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1");
        var bugTicket = TestData.BugTicket("FOK-B1", assigneeId: "dev-1");
        bugTicket.Assignee = dev;
        // storyPoints: null → uses DefaultSpPerBug = 3
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-B1", storyPoints: null, ticket: bugTicket));

        var completionTime = sprintStart.AddDays(1).AddHours(10);
        var transitions = new List<StatusTransition>
        {
            TestData.Transition("FOK-B1", "Done", completionTime, "In Progress")
        };

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: transitions,
            excludedDeveloperIds: []);

        var devEntry = result.Developers.Single();
        devEntry.BugCompletedSp.Should().Be(3m); // DefaultSpPerBug = 3
        devEntry.FeatureCompletedSp.Should().Be(0m);
    }

    // ============================================================
    // Step 2: Delta-based alerting
    // ============================================================

    // --- Slice 14: Grace period — alerts empty regardless of gap ---

    [Fact]
    public void Should_SuppressAlerts_When_GracePeriod()
    {
        // Sprint starts today → currentDay = 1, grace period
        var sprintStart = DateTime.UtcNow.Date;
        var sprintEnd = sprintStart.AddDays(9);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1");
        var ticket = TestData.FeatureTicket("FOK-1", assigneeId: "dev-1");
        ticket.Assignee = dev;
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-1", storyPoints: 10m, ticket: ticket));

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: [],
            excludedDeveloperIds: []);

        result.IsGracePeriod.Should().BeTrue();
        result.Alerts.Should().BeEmpty();
    }

    // --- Slice 15: Worsening — gap grew since previous day, appears in alerts ---

    [Fact]
    public void Should_ShowWorseningAlert_When_GapGrewSincePreviousDay()
    {
        // Sprint: 10 days, started 3 days ago → currentDay = 4, post-grace
        // dailyPace = 10/10 = 1.0 SP/day, nothing completed
        // gap(day3) = 3.0, gap(day4) = 4.0 → gapDelta = 1.0 → worsening
        var sprintStart = DateTime.UtcNow.Date.AddDays(-3);
        var sprintEnd = sprintStart.AddDays(9);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1", "Dev One");
        var ticket = TestData.FeatureTicket("FOK-1", assigneeId: "dev-1");
        ticket.Assignee = dev;
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-1", storyPoints: 10m, ticket: ticket));

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: [],
            excludedDeveloperIds: []);

        result.IsGracePeriod.Should().BeFalse();
        var alert = result.Alerts.Should().ContainSingle().Subject;
        alert.AccountId.Should().Be("dev-1");
        alert.Direction.Should().Be("worsening");
        alert.GapDelta.Should().Be(1.0m);
    }

    // --- Slice 16: Improving — developer completes work, gap shrank ---

    [Fact]
    public void Should_ShowImprovingAlert_When_GapShrankSincePreviousDay()
    {
        // Sprint: 10 days, started 3 days ago → currentDay = 4, post-grace
        // dailyPace = 1.0 SP/day
        // gap(day3) = 1.0*3 - 0 = 3.0  (nothing done by end of day 3)
        // gap(day4) = 1.0*4 - 10 = -6.0 (all 10 SP completed on day 4)
        // gapDelta = -6.0 - 3.0 = -9.0 < 0 → improving
        var sprintStart = DateTime.UtcNow.Date.AddDays(-3);
        var sprintEnd = sprintStart.AddDays(9);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1", "Dev One");
        var ticket = TestData.FeatureTicket("FOK-1", assigneeId: "dev-1");
        ticket.Assignee = dev;
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-1", storyPoints: 10m, ticket: ticket));

        // Complete all 10 SP on day 4
        var completionTime = sprintStart.AddDays(3).AddHours(10);
        var transitions = new List<StatusTransition>
        {
            TestData.Transition("FOK-1", "Done", completionTime, "In Progress")
        };

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: transitions,
            excludedDeveloperIds: []);

        result.IsGracePeriod.Should().BeFalse();
        var alert = result.Alerts.Should().ContainSingle().Subject;
        alert.AccountId.Should().Be("dev-1");
        alert.Direction.Should().Be("improving");
        alert.GapDelta.Should().Be(-9.0m);
    }

    // --- Slice 17: Stable — gap unchanged, NOT in alerts ---

    [Fact]
    public void Should_NotIncludeInAlerts_When_GapUnchanged()
    {
        // Sprint: 10 days, started 3 days ago → currentDay = 4, post-grace
        // dailyPace = 1.0 SP/day; complete exactly 1 SP each day → gap stays 0 → stable
        var sprintStart = DateTime.UtcNow.Date.AddDays(-3);
        var sprintEnd = sprintStart.AddDays(9);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1", "Dev One");

        var t1 = TestData.FeatureTicket("FOK-1", assigneeId: "dev-1"); t1.Assignee = dev;
        var t2 = TestData.FeatureTicket("FOK-2", assigneeId: "dev-1"); t2.Assignee = dev;
        var t3 = TestData.FeatureTicket("FOK-3", assigneeId: "dev-1"); t3.Assignee = dev;
        var t4 = TestData.FeatureTicket("FOK-4", assigneeId: "dev-1"); t4.Assignee = dev;
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-1", storyPoints: 1m, ticket: t1));
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-2", storyPoints: 1m, ticket: t2));
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-3", storyPoints: 1m, ticket: t3));
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-4", storyPoints: 1m, ticket: t4));

        // Remaining 6 tickets (assigned but not completed; total assigned = 10)
        for (var i = 5; i <= 10; i++)
        {
            var tx = TestData.FeatureTicket($"FOK-{i}", assigneeId: "dev-1"); tx.Assignee = dev;
            sprint.AddMembership(TestData.Membership(sprint.Id, $"FOK-{i}", storyPoints: 1m, ticket: tx));
        }

        var transitions = new List<StatusTransition>
        {
            TestData.Transition("FOK-1", "Done", sprintStart.AddHours(12), "In Progress"),
            TestData.Transition("FOK-2", "Done", sprintStart.AddDays(1).AddHours(12), "In Progress"),
            TestData.Transition("FOK-3", "Done", sprintStart.AddDays(2).AddHours(12), "In Progress"),
            TestData.Transition("FOK-4", "Done", sprintStart.AddDays(3).AddHours(12), "In Progress"),
        };

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: transitions,
            excludedDeveloperIds: []);

        result.IsGracePeriod.Should().BeFalse();
        result.Alerts.Should().BeEmpty(); // stable → not in alerts
    }

    // --- Slice 18: New-stall — direction is new-stall when ticket just became stalled today ---
    // Requires today to be a business day. CountBusinessDays counts from day AFTER 'from' to 'to'.
    // Last transition = SubtractBusinessDays(today, 3):
    //   count to today = 3 > 2 → stalled today
    //   count to yesterday = 2 → not stalled yesterday → new-stall
    // Skipped on weekends (Saturday/Sunday) where today has 0 business days in any window.

    [Fact]
    public void Should_ShowNewStallAlert_When_TicketJustBecameStalled()
    {
        var today = DateTime.UtcNow.Date;

        // Skip on weekends: stall detection is business-day based; today has 0 business weight.
        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return;

        // Also skip if yesterday is a weekend (Friday scenario needs yesterday=Thu which is fine,
        // but if yesterday is Sat/Sun the boundary math changes).
        // Specifically: if today is Monday, yesterday=Sunday contributes 0 business days.
        // SubtractBusinessDays(Mon, 3) = Wed-prev-week.
        // Count from Thu to Mon = Thu,Fri,Mon = 3 ✓; count from Thu to Sun = Thu,Fri = 2 ✓ (Sun is not counted).
        // Monday case is fine. Only Sat/Sun as TODAY breaks the "stalled today" assumption.

        var lastTransitionDate = SubtractBusinessDays(today, 3);
        var sprintStart = today.AddDays(-5);
        var sprintEnd = sprintStart.AddDays(9);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var dev = TestData.ActiveDeveloper("dev-1", "Dev One");
        var ticket = TestData.FeatureTicket("FOK-1", assigneeId: "dev-1");
        ticket.Assignee = dev;
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-1", storyPoints: 10m, ticket: ticket));

        var transitions = new List<StatusTransition>
        {
            TestData.Transition("FOK-1", "In Progress", sprintStart.AddHours(8), "To Do"),
            TestData.Transition("FOK-1", "In Review", lastTransitionDate.AddHours(9), "In Progress")
        };

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [dev],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [dev],
            statusTransitions: transitions,
            excludedDeveloperIds: []);

        result.IsGracePeriod.Should().BeFalse();
        var alert = result.Alerts.Should().ContainSingle().Subject;
        alert.AccountId.Should().Be("dev-1");
        alert.Direction.Should().Be("new-stall");
    }

    /// <summary>Subtracts <paramref name="businessDays"/> Mon-Fri days from <paramref name="date"/>.</summary>
    private static DateTime SubtractBusinessDays(DateTime date, int businessDays)
    {
        var result = date;
        var remaining = businessDays;
        while (remaining > 0)
        {
            result = result.AddDays(-1);
            if (result.DayOfWeek != DayOfWeek.Saturday && result.DayOfWeek != DayOfWeek.Sunday)
                remaining--;
        }
        return result;
    }

    // --- Slice 19: Sort order — worsening group before improving group ---

    [Fact]
    public void Should_SortAlerts_WithWorseningGroupBeforeImprovingGroup()
    {
        // dev-a: 10 SP, nothing completed on day 4 → worsening (gapDelta = 1.0)
        // dev-b: 10 SP, completes all on day 4 → improving (gapDelta = -9.0)
        var sprintStart = DateTime.UtcNow.Date.AddDays(-3);
        var sprintEnd = sprintStart.AddDays(9);
        var sprint = TestData.ActiveSprint(startDate: sprintStart, endDate: sprintEnd);

        var devA = TestData.ActiveDeveloper("dev-a", "Dev A");
        var devB = TestData.ActiveDeveloper("dev-b", "Dev B");

        var ticketA = TestData.FeatureTicket("FOK-A", assigneeId: "dev-a"); ticketA.Assignee = devA;
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-A", storyPoints: 10m, ticket: ticketA));

        var ticketB = TestData.FeatureTicket("FOK-B", assigneeId: "dev-b"); ticketB.Assignee = devB;
        sprint.AddMembership(TestData.Membership(sprint.Id, "FOK-B", storyPoints: 10m, ticket: ticketB));

        var transitions = new List<StatusTransition>
        {
            TestData.Transition("FOK-B", "Done", sprintStart.AddDays(3).AddHours(10), "In Progress")
        };

        var result = _sut.ComputeProgress(
            activeSprint: sprint,
            activeDevelopers: [devA, devB],
            capacityRecords: [],
            settings: _settings,
            subTeam: null,
            allDevelopers: [devA, devB],
            statusTransitions: transitions,
            excludedDeveloperIds: []);

        result.Alerts.Should().HaveCount(2);
        result.Alerts[0].Direction.Should().Be("worsening");
        result.Alerts[1].Direction.Should().Be("improving");
        result.Alerts[0].AccountId.Should().Be("dev-a");
        result.Alerts[1].AccountId.Should().Be("dev-b");
    }
}
