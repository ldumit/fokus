namespace Fokus.Tests.Features.Analytics;

public class TransitionAttributionCheckerTests
{
    private const string TicketId = "FOK-1";

    private static readonly DateTime SprintStart = new(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SprintEnd = new(2026, 3, 15, 18, 0, 0, DateTimeKind.Utc);

    private readonly List<string> _orderedStages;
    private readonly int _startIndex;
    private readonly int _endIndex;
    private readonly List<StatusTransition> _transitions = [];

    public TransitionAttributionCheckerTests()
    {
        var settings = TestData.DefaultSettings();
        (_orderedStages, _startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        _endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, _orderedStages);
    }

    private (bool IsStarted, DateTime? StartedAt) Started(string ticketId = TicketId, int? startIndex = null) =>
        TransitionAttributionChecker.IsStartedInSprint(
            ticketId, StatusTransitionIndex.Build(_transitions), SprintStart, SprintEnd, _orderedStages, startIndex ?? _startIndex);

    private (bool IsCompleted, DateTime? CompletedAt) Completed(string ticketId = TicketId, int? endIndex = null) =>
        TransitionAttributionChecker.IsCompletedInSprint(
            ticketId, StatusTransitionIndex.Build(_transitions), SprintStart, SprintEnd, _orderedStages, endIndex ?? _endIndex);

    // --- Slice 1: Transition inside the sprint window ---

    [Fact]
    public void Should_ReturnStartedAndCompletedWithTimestamps_When_TransitionsFallInsideSprint()
    {
        var startedAt = SprintStart.AddDays(2);
        var completedAt = SprintStart.AddDays(5);
        _transitions.Add(TestData.Transition(TicketId, "In Progress", startedAt, "To Do"));
        _transitions.Add(TestData.Transition(TicketId, "Done", completedAt));

        Started().Should().Be((true, (DateTime?)startedAt));
        Completed().Should().Be((true, (DateTime?)completedAt));
    }

    // --- Slice 2: Transitions outside the sprint window ---

    [Fact]
    public void Should_ReturnNotStartedOrCompleted_When_TransitionsFallBeforeOrAfterSprint()
    {
        _transitions.Add(TestData.Transition(TicketId, "In Progress", SprintStart.AddTicks(-1), "To Do"));
        _transitions.Add(TestData.Transition(TicketId, "Done", SprintEnd.AddTicks(1)));

        Started().Should().Be((false, (DateTime?)null));
        Completed().Should().Be((false, (DateTime?)null));
    }

    // --- Slice 3: Window boundaries are inclusive ---

    [Fact]
    public void Should_CountTransitions_When_TheyFallExactlyOnSprintStartAndEnd()
    {
        _transitions.Add(TestData.Transition(TicketId, "In Progress", SprintStart, "To Do"));
        _transitions.Add(TestData.Transition(TicketId, "Done", SprintEnd));

        Started().Should().Be((true, (DateTime?)SprintStart));
        Completed().Should().Be((true, (DateTime?)SprintEnd));
    }

    // --- Slice 4: The earliest qualifying transition wins ---

    [Fact]
    public void Should_ReturnEarliestQualifyingTimestamp_When_SeveralTransitionsQualifyInAnyListOrder()
    {
        var later = SprintStart.AddDays(6);
        var earliest = SprintStart.AddDays(1);
        _transitions.Add(TestData.Transition(TicketId, "Done", later));
        _transitions.Add(TestData.Transition(TicketId, "To Do", SprintStart.AddHours(1), "Backlog"));
        _transitions.Add(TestData.Transition(TicketId, "Done", earliest));

        Started().Should().Be((true, (DateTime?)earliest));
        Completed().Should().Be((true, (DateTime?)earliest));
    }

    // --- Slice 5: Stages below the boundary do not qualify ---

    [Fact]
    public void Should_NotComplete_When_TicketOnlyReachesStageBeforeEndBoundary()
    {
        var startedAt = SprintStart.AddDays(1);
        _transitions.Add(TestData.Transition(TicketId, "To Do", SprintStart.AddHours(1), "Backlog"));
        _transitions.Add(TestData.Transition(TicketId, "In Progress", startedAt, "To Do"));

        Started().Should().Be((true, (DateTime?)startedAt));
        Completed().Should().Be((false, (DateTime?)null));
    }

    // --- Slice 6: Ticket ids match ordinally, case-sensitive ---

    [Fact]
    public void Should_NotMatch_When_TicketIdDiffersOnlyInCase()
    {
        _transitions.Add(TestData.Transition("fok-1", "In Progress", SprintStart.AddDays(1), "To Do"));
        _transitions.Add(TestData.Transition("fok-1", "Done", SprintStart.AddDays(2)));
        _transitions.Add(TestData.Transition("FOK-2", "Done", SprintStart.AddDays(2)));

        Started().Should().Be((false, (DateTime?)null));
        Completed().Should().Be((false, (DateTime?)null));
        Completed("fok-1").Should().Be((true, (DateTime?)SprintStart.AddDays(2)));
    }

    // --- Slice 7: A negative boundary index never attributes ---

    [Fact]
    public void Should_ReturnFalse_When_BoundaryIndexIsNegative()
    {
        _transitions.Add(TestData.Transition(TicketId, "In Progress", SprintStart.AddDays(1), "To Do"));
        _transitions.Add(TestData.Transition(TicketId, "Done", SprintStart.AddDays(2)));

        Started(startIndex: -1).Should().Be((false, (DateTime?)null));
        Completed(endIndex: -1).Should().Be((false, (DateTime?)null));
    }

    // --- Slice 8: A ticket with no transitions ---

    [Fact]
    public void Should_ReturnFalse_When_TicketHasNoTransitions()
    {
        _transitions.Add(TestData.Transition("FOK-2", "Done", SprintStart.AddDays(2)));

        Started().Should().Be((false, (DateTime?)null));
        Completed().Should().Be((false, (DateTime?)null));
    }
}
