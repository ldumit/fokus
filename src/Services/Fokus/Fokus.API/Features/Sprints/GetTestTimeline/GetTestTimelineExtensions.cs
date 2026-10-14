namespace Fokus.API.Features.Sprints.GetTestTimeline;

public static class GetTestTimelineExtensions
{
    public static TestTimelineResponse ToTestTimelineResponse(this TestTimelineResult timeline, Sprint sprint, int planningWindowDays) =>
        new(
            HasQaData: true,
            IsXrayEnabled: true,
            SprintStartDate: sprint.StartDate,
            SprintEndDate: sprint.EndDate,
            planningWindowDays,
            timeline.BurnupData.Select(ToBurnupDayEntryDto).ToList(),
            timeline.ScopeChangeOverlay.Select(ToScopeChangeDayEntryDto).ToList(),
            timeline.TestingCrunch.ToTestingCrunchDto(),
            timeline.PostSprintTesting.ToPostSprintTestingDto(),
            timeline.UntestedAtClose.ToUntestedAtCloseDto(),
            timeline.DevToTestGap.ToDevToTestGapDto());

    public static BurnupDayEntryDto ToBurnupDayEntryDto(this BurnupDayEntry burnupDayEntry) =>
        new(
            burnupDayEntry.DayNumber,
            burnupDayEntry.CalendarDate,
            burnupDayEntry.IsWithinSprint,
            CumulativePass: burnupDayEntry.CumulativePass,
            CumulativeFail: burnupDayEntry.CumulativeFail,
            CumulativeTotal: burnupDayEntry.CumulativeTotal,
            DailyPass: burnupDayEntry.DailyPass,
            DailyFail: burnupDayEntry.DailyFail);

    public static ScopeChangeDayEntryDto ToScopeChangeDayEntryDto(this ScopeChangeDayEntry scopeChangeDayEntry) =>
        new(
            scopeChangeDayEntry.DayNumber,
            scopeChangeDayEntry.CalendarDate,
            AddedSp: scopeChangeDayEntry.AddedSp,
            RemovedSp: scopeChangeDayEntry.RemovedSp,
            NetSp: scopeChangeDayEntry.NetSp);

    public static TestingCrunchDto ToTestingCrunchDto(this TestingCrunchResult testingCrunch) =>
        new(
            testingCrunch.IsCrunchFlagged,
            testingCrunch.CrunchPercentage,
            CrunchRunCount: testingCrunch.CrunchRunCount,
            TotalRunCount: testingCrunch.TotalRunCount,
            testingCrunch.CrunchTickets.Select(ToCrunchTicketEntryDto).ToList());

    public static PostSprintTestingDto ToPostSprintTestingDto(this PostSprintTestingResult postSprintTesting) =>
        new(
            postSprintTesting.HasPostSprintTesting,
            postSprintTesting.PostSprintPercentage,
            PostSprintRunCount: postSprintTesting.PostSprintRunCount,
            TotalRunCount: postSprintTesting.TotalRunCount,
            postSprintTesting.PostSprintTickets.Select(ToPostSprintTicketEntryDto).ToList());

    public static UntestedAtCloseDto ToUntestedAtCloseDto(this UntestedAtCloseResult untestedAtClose) =>
        new(
            untestedAtClose.HasUntestedAtClose,
            untestedAtClose.UntestedAtCloseCount,
            untestedAtClose.UntestedAtCloseTickets.Select(ToUntestedTicketEntryDto).ToList());

    public static DevToTestGapDto ToDevToTestGapDto(this DevToTestGapResult devToTestGap) =>
        new(
            MedianGapDays: devToTestGap.MedianGapDays,
            MedianGapDelta: devToTestGap.MedianGapDelta,
            devToTestGap.MedianGapDirection,
            devToTestGap.GapTickets.Select(ToGapTicketEntryDto).ToList());

    public static CrunchTicketEntryDto ToCrunchTicketEntryDto(this CrunchTicketEntry crunchTicketEntry) =>
        new(
            TicketKey: crunchTicketEntry.TicketKey,
            Summary: crunchTicketEntry.Summary,
            AssigneeName: crunchTicketEntry.AssigneeName,
            crunchTicketEntry.StoryPoints,
            crunchTicketEntry.LateRunCount);

    public static PostSprintTicketEntryDto ToPostSprintTicketEntryDto(this PostSprintTicketEntry postSprintTicketEntry) =>
        new(
            TicketKey: postSprintTicketEntry.TicketKey,
            Summary: postSprintTicketEntry.Summary,
            AssigneeName: postSprintTicketEntry.AssigneeName,
            postSprintTicketEntry.StoryPoints,
            postSprintTicketEntry.PostSprintRunCount);

    public static UntestedTicketEntryDto ToUntestedTicketEntryDto(this UntestedTicketEntry untestedTicketEntry) =>
        new(
            TicketKey: untestedTicketEntry.TicketKey,
            Summary: untestedTicketEntry.Summary,
            AssigneeName: untestedTicketEntry.AssigneeName,
            untestedTicketEntry.StoryPoints,
            untestedTicketEntry.DevDoneDate);

    public static GapTicketEntryDto ToGapTicketEntryDto(this GapTicketEntry gapTicketEntry) =>
        new(
            TicketKey: gapTicketEntry.TicketKey,
            Summary: gapTicketEntry.Summary,
            AssigneeName: gapTicketEntry.AssigneeName,
            DevDoneDate: gapTicketEntry.DevDoneDate,
            FirstTestDate: gapTicketEntry.FirstTestDate,
            gapTicketEntry.GapDays);
}
