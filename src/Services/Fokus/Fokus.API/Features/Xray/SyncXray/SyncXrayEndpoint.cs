using Fokus.API.Features.Xray;

namespace Fokus.API.Features.Xray.SyncXray;

public class SyncXrayRequest
{
    public int[] SprintIds { get; set; } = [];
}

public class SyncXrayRequestValidator : Validator<SyncXrayRequest>
{
    public SyncXrayRequestValidator()
    {
        // SprintIds may be empty — empty means "all synced sprints" (server resolves scope)
        RuleForEach(x => x.SprintIds)
            .GreaterThan(0)
            .WithMessage("Each sprint ID must be greater than 0.");
    }
}

public class SyncXrayResponse
{
    public int TestExecutionsSynced { get; set; }
    public int TestRunsSynced { get; set; }
    public int TestSetsSynced { get; set; }
    public string[] Warnings { get; set; } = [];
}

[HttpPost("/api/xray/sync")]
[Tags("Xray")]
[Authorize(Roles = "Admin")]
public class SyncXrayEndpoint(
    AppSettingsRepository settingsRepository,
    SprintRepository sprintRepository,
    IJiraClient jiraClient,
    XrayIssueSyncService xraySyncService)
    : Endpoint<SyncXrayRequest, SyncXrayResponse>
{
    public override async Task HandleAsync(SyncXrayRequest req, CancellationToken ct)
    {
        var settings = await settingsRepository.GetAsync(ct);
        if (!settings.XrayEnabled)
            throw new BadRequestException("Xray is not enabled.");

        // Empty SprintIds means "all synced sprints"
        List<Sprint> sprints;
        if (req.SprintIds.Length == 0)
            sprints = await sprintRepository.GetAllAsync(ct);
        else
            sprints = await sprintRepository.GetSprintsWithMembershipsAsync(req.SprintIds.ToList(), ct);

        if (sprints.Count == 0)
            throw new BadRequestException("No synced sprints found. Run a Jira sync first.");

        // Re-fetch fresh JiraIssue data for each sprint (issue links live on Jira DTO, not stored Ticket)
        var allJiraIssues = new List<JiraIssue>();
        foreach (var sprint in sprints)
        {
            var issues = await jiraClient.GetSprintIssuesAsync(sprint.Id, ct);
            allJiraIssues.AddRange(issues);
        }

        // Deduplicate — a ticket may appear in multiple sprints
        allJiraIssues = allJiraIssues.DistinctBy(i => i.Key).ToList();

        var result = await xraySyncService.SyncXrayForIssuesAsync(allJiraIssues, ct);

        await SendOkAsync(new SyncXrayResponse
        {
            TestExecutionsSynced = result.TestExecutionsSynced,
            TestRunsSynced = result.TestRunsSynced,
            TestSetsSynced = result.TestSetsSynced,
            Warnings = result.Warnings.ToArray()
        }, ct);
    }
}
