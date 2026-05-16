using Microsoft.Extensions.Logging;
using Xray.Contracts;

namespace Fokus.API.Features.Xray;

public record XraySyncResult(
    int TestExecutionsSynced,
    int TestRunsSynced,
    int TestSetsSynced,
    List<string> Warnings);

public class XrayIssueSyncService(
    IXrayClient xrayClient,
    AppSettingsRepository appSettingsRepository,
    TestExecutionRepository testExecutionRepository,
    DeveloperRepository developerRepository,
    ILogger<XrayIssueSyncService> logger)
{
    public async Task<XraySyncResult> SyncXrayForIssuesAsync(
        IReadOnlyList<JiraIssue> jiraIssues,
        CancellationToken ct)
    {
        var warnings = new List<string>();

        var settings = await appSettingsRepository.GetAsync(ct);
        if (!settings.XrayEnabled || string.IsNullOrEmpty(settings.XrayClientId) || string.IsNullOrEmpty(settings.XrayClientSecret))
            return new XraySyncResult(0, 0, 0, warnings);

        var linked = ExtractLinkedIssues(jiraIssues);
        if (linked.TeIssueKeys.Count == 0 && linked.TestSets.Count == 0)
            return new XraySyncResult(0, 0, 0, warnings);

        var bearerToken = await AuthenticateAsync(settings, warnings, ct);
        if (bearerToken is null)
            return new XraySyncResult(0, 0, 0, warnings);

        var xrayResult = await FetchTestExecutionsAsync(bearerToken, linked.TeIssueKeys, warnings, ct);
        var xrayTeByKey = xrayResult.TestExecutions.ToDictionary(t => t.IssueKey ?? t.IssueId, t => t);

        var (testExecutionsSynced, testRunsSynced) = await SyncTestExecutionsAsync(
            linked.TeIssueKeys, jiraIssues, linked.TeLinkMap, xrayTeByKey, warnings, ct);

        var testSetsSynced = await SyncTestSetsAsync(linked.TestSets, warnings, ct);

        return new XraySyncResult(testExecutionsSynced, testRunsSynced, testSetsSynced, warnings);
    }

    private static LinkedIssues ExtractLinkedIssues(IReadOnlyList<JiraIssue> jiraIssues)
    {
        var teIssueKeys = new List<string>();
        var teLinkMap = new Dictionary<string, List<(string TicketKey, TestExecutionLinkType LinkType)>>();
        var testSets = new Dictionary<string, (string Id, string Key, string Summary, string? AssigneeId, string Status)>();

        foreach (var issue in jiraIssues)
        {
            if (issue.Fields.Issuelinks is null) continue;

            foreach (var link in issue.Fields.Issuelinks)
            {
                var linkTypeName = link.Type?.Name ?? "";

                // "Test" link type covers "Tests"/"is tested by"
                // "Blocks" link type covers "blocks"/"is blocked by"
                var isTestLink = linkTypeName.Equals("Test", StringComparison.OrdinalIgnoreCase);
                var isBlocksLink = linkTypeName.Equals("Blocks", StringComparison.OrdinalIgnoreCase);

                if (!isTestLink && !isBlocksLink) continue;

                var linkedIssue = link.OutwardIssue ?? link.InwardIssue;
                if (linkedIssue is null) continue;

                var linkedType = linkedIssue.Fields.Issuetype?.Name ?? "";

                if (isTestLink && linkedType.Equals("Test Execution", StringComparison.OrdinalIgnoreCase))
                {
                    var teKey = linkedIssue.Key;
                    if (!teIssueKeys.Contains(teKey))
                        teIssueKeys.Add(teKey);

                    if (!teLinkMap.ContainsKey(teKey))
                        teLinkMap[teKey] = [];

                    teLinkMap[teKey].Add((issue.Key, TestExecutionLinkType.Tests));
                }
                else if (isTestLink && linkedType.Equals("Test Set", StringComparison.OrdinalIgnoreCase))
                {
                    var tsKey = linkedIssue.Key;
                    if (!testSets.ContainsKey(tsKey))
                    {
                        testSets[tsKey] = (
                            linkedIssue.Id,
                            tsKey,
                            linkedIssue.Fields.Summary,
                            linkedIssue.Fields.Assignee?.AccountId,
                            linkedIssue.Fields.Status?.Name ?? "Unknown"
                        );
                    }
                }
                else if (isBlocksLink && linkedType.Equals("Test Execution", StringComparison.OrdinalIgnoreCase))
                {
                    var teKey = linkedIssue.Key;
                    if (!teIssueKeys.Contains(teKey))
                        teIssueKeys.Add(teKey);

                    if (!teLinkMap.ContainsKey(teKey))
                        teLinkMap[teKey] = [];

                    teLinkMap[teKey].Add((issue.Key, TestExecutionLinkType.Blocks));
                }
            }
        }

        return new LinkedIssues(teIssueKeys, teLinkMap, testSets);
    }

    private async Task<string?> AuthenticateAsync(AppSettings settings, List<string> warnings, CancellationToken ct)
    {
        try
        {
            return await xrayClient.AuthenticateAsync(settings.XrayClientId!, settings.XrayClientSecret!, ct);
        }
        catch (Exception ex)
        {
            var warning = $"Xray authentication failed: {ex.Message}";
            logger.LogWarning(warning);
            warnings.Add(warning);
            return null;
        }
    }

    private async Task<XrayTestExecutionResult> FetchTestExecutionsAsync(
        string bearerToken, List<string> teIssueKeys, List<string> warnings, CancellationToken ct)
    {
        try
        {
            return await xrayClient.GetTestExecutionsAsync(bearerToken, teIssueKeys, ct);
        }
        catch (Exception ex)
        {
            var warning = $"Xray GraphQL query failed: {ex.Message}";
            logger.LogWarning(warning);
            warnings.Add(warning);
            return new XrayTestExecutionResult();
        }
    }

    private async Task<(int TestExecutionsSynced, int TestRunsSynced)> SyncTestExecutionsAsync(
        List<string> teIssueKeys,
        IReadOnlyList<JiraIssue> jiraIssues,
        Dictionary<string, List<(string TicketKey, TestExecutionLinkType LinkType)>> teLinkMap,
        Dictionary<string, XrayTestExecutionDto> xrayTeByKey,
        List<string> warnings,
        CancellationToken ct)
    {
        var testExecutionsSynced = 0;
        var testRunsSynced = 0;

        foreach (var teKey in teIssueKeys)
        {
            try
            {
                xrayTeByKey.TryGetValue(teKey, out var xrayTe);

                var issueId = xrayTe?.IssueId;
                if (string.IsNullOrEmpty(issueId))
                    issueId = FindTeIssueId(jiraIssues, teKey);

                if (string.IsNullOrEmpty(issueId))
                {
                    warnings.Add($"Could not determine issue ID for Test Execution {teKey} — skipped.");
                    continue;
                }

                var runsCount = await SyncSingleTestExecutionAsync(teKey, issueId, xrayTe, teLinkMap, ct);
                testExecutionsSynced++;
                testRunsSynced += runsCount;
            }
            catch (Exception ex)
            {
                var warning = $"Failed to sync Test Execution {teKey}: {ex.Message}";
                logger.LogWarning(warning);
                warnings.Add(warning);
            }
        }

        return (testExecutionsSynced, testRunsSynced);
    }

    private async Task<int> SyncSingleTestExecutionAsync(
        string teKey,
        string issueId,
        XrayTestExecutionDto? xrayTe,
        Dictionary<string, List<(string TicketKey, TestExecutionLinkType LinkType)>> teLinkMap,
        CancellationToken ct)
    {
        var assigneeId = xrayTe?.AssigneeId;
        if (!string.IsNullOrEmpty(assigneeId))
            await UpsertDeveloperFromIdAsync(assigneeId, ct);

        var te = TestExecution.FromXray(
            issueId: issueId,
            issueKey: teKey,
            summary: xrayTe?.Summary ?? teKey,
            status: xrayTe?.Status ?? "Unknown",
            assigneeId: assigneeId,
            createdDate: xrayTe?.CreatedDate ?? DateTime.UtcNow
        );

        await testExecutionRepository.UpsertAsync(te, ct);
        await testExecutionRepository.SaveChangesAsync(ct);

        var links = new List<TestExecutionLink>();
        if (teLinkMap.TryGetValue(teKey, out var ticketLinks))
        {
            foreach (var (ticketKey, linkType) in ticketLinks)
                links.Add(new TestExecutionLink { TestExecutionIssueId = issueId, TicketKey = ticketKey, LinkType = linkType });
        }

        await testExecutionRepository.ReplaceLinksAsync(issueId, links, ct);
        await testExecutionRepository.SaveChangesAsync(ct);

        var runs = new List<TestRun>();
        if (xrayTe?.TestRuns is not null)
        {
            foreach (var runDto in xrayTe.TestRuns)
            {
                if (!string.IsNullOrEmpty(runDto.ExecutedById))
                    await UpsertDeveloperFromIdAsync(runDto.ExecutedById, ct);

                runs.Add(TestRun.FromXray(
                    id: runDto.Id,
                    testExecutionIssueId: issueId,
                    status: ParseTestRunStatus(runDto.StatusName),
                    statusName: runDto.StatusName,
                    startedAt: runDto.StartedAt,
                    finishedAt: runDto.FinishedAt,
                    executedById: runDto.ExecutedById
                ));
            }
        }

        await testExecutionRepository.ReplaceTestRunsAsync(issueId, runs, ct);
        await testExecutionRepository.SaveChangesAsync(ct);

        return runs.Count;
    }

    private async Task<int> SyncTestSetsAsync(
        Dictionary<string, (string Id, string Key, string Summary, string? AssigneeId, string Status)> testSets,
        List<string> warnings,
        CancellationToken ct)
    {
        var testSetsSynced = 0;
        foreach (var (_, ts) in testSets)
        {
            try
            {
                if (!string.IsNullOrEmpty(ts.AssigneeId))
                    await UpsertDeveloperFromIdAsync(ts.AssigneeId, ct);

                var testSet = TestSet.FromJiraIssueLink(
                    issueId: ts.Id,
                    issueKey: ts.Key,
                    summary: ts.Summary,
                    assigneeId: ts.AssigneeId,
                    status: ts.Status
                );

                await testExecutionRepository.UpsertTestSetAsync(testSet, ct);
                await testExecutionRepository.SaveChangesAsync(ct);
                testSetsSynced++;
            }
            catch (Exception ex)
            {
                var warning = $"Failed to sync Test Set {ts.Key}: {ex.Message}";
                logger.LogWarning(warning);
                warnings.Add(warning);
            }
        }
        return testSetsSynced;
    }

    private static string? FindTeIssueId(IReadOnlyList<JiraIssue> jiraIssues, string teKey)
    {
        foreach (var issue in jiraIssues)
        {
            if (issue.Fields.Issuelinks is null) continue;
            foreach (var link in issue.Fields.Issuelinks)
            {
                var linked = link.OutwardIssue ?? link.InwardIssue;
                if (linked?.Key == teKey)
                    return linked.Id;
            }
        }
        return null;
    }

    private async Task UpsertDeveloperFromIdAsync(string accountId, CancellationToken ct)
    {
        var existing = await developerRepository.GetByIdAsync(accountId, ct);
        if (existing is null)
        {
            var dev = new Developer
            {
                Id = accountId,
                DisplayName = accountId, // Display name not available without separate Jira call
                IsActive = true
            };
            await developerRepository.UpsertAsync(dev, ct);
            await developerRepository.SaveChangesAsync(ct);
        }
    }

    private static TestRunStatus ParseTestRunStatus(string statusName) =>
        statusName.ToUpperInvariant() switch
        {
            "PASS" or "PASSED" => TestRunStatus.Pass,
            "FAIL" or "FAILED" => TestRunStatus.Fail,
            "EXECUTING" or "IN PROGRESS" => TestRunStatus.Executing,
            "ABORTED" or "ABANDONED" => TestRunStatus.Aborted,
            _ => TestRunStatus.Todo
        };

    private record LinkedIssues(
        List<string> TeIssueKeys,
        Dictionary<string, List<(string TicketKey, TestExecutionLinkType LinkType)>> TeLinkMap,
        Dictionary<string, (string Id, string Key, string Summary, string? AssigneeId, string Status)> TestSets);
}
