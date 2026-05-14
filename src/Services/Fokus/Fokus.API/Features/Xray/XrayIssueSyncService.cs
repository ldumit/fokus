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

        // Step 2: Extract issue links from JiraIssues
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

                // Determine the linked issue from outward or inward
                var linkedIssue = link.OutwardIssue ?? link.InwardIssue;
                if (linkedIssue is null) continue;

                var linkedType = linkedIssue.Fields.Issuetype?.Name ?? "";

                if (isTestLink && linkedType.Equals("Test Execution", StringComparison.OrdinalIgnoreCase))
                {
                    var teKey = linkedIssue.Key;
                    var teId = linkedIssue.Id;
                    if (!teIssueKeys.Contains(teKey))
                        teIssueKeys.Add(teKey);

                    if (!teLinkMap.ContainsKey(teKey))
                        teLinkMap[teKey] = new List<(string, TestExecutionLinkType)>();

                    teLinkMap[teKey].Add((issue.Key, TestExecutionLinkType.Tests));
                }
                else if (isTestLink && linkedType.Equals("Test Set", StringComparison.OrdinalIgnoreCase))
                {
                    var tsKey = linkedIssue.Key;
                    var tsId = linkedIssue.Id;
                    if (!testSets.ContainsKey(tsKey))
                    {
                        testSets[tsKey] = (
                            tsId,
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
                        teLinkMap[teKey] = new List<(string, TestExecutionLinkType)>();

                    teLinkMap[teKey].Add((issue.Key, TestExecutionLinkType.Blocks));
                }
            }
        }

        if (teIssueKeys.Count == 0 && testSets.Count == 0)
            return new XraySyncResult(0, 0, 0, warnings);

        // Step 3: Authenticate with Xray
        string bearerToken;
        try
        {
            bearerToken = await xrayClient.AuthenticateAsync(settings.XrayClientId, settings.XrayClientSecret, ct);
        }
        catch (Exception ex)
        {
            var warning = $"Xray authentication failed: {ex.Message}";
            logger.LogWarning(warning);
            warnings.Add(warning);
            return new XraySyncResult(0, 0, 0, warnings);
        }

        // Step 4: Fetch test runs from Xray GraphQL
        XrayTestExecutionResult xrayResult;
        try
        {
            xrayResult = await xrayClient.GetTestExecutionsAsync(bearerToken, teIssueKeys, ct);
        }
        catch (Exception ex)
        {
            var warning = $"Xray GraphQL query failed: {ex.Message}";
            logger.LogWarning(warning);
            warnings.Add(warning);
            xrayResult = new XrayTestExecutionResult();
        }

        // Build a lookup from issueKey to XrayTestExecutionDto
        var xrayTeByKey = xrayResult.TestExecutions.ToDictionary(t => t.IssueKey ?? t.IssueId, t => t);

        var testExecutionsSynced = 0;
        var testRunsSynced = 0;

        // Steps 5-7: Map and persist Test Executions, Links, and Runs
        foreach (var teKey in teIssueKeys)
        {
            try
            {
                // Get Xray data for this TE (may be absent if Xray returned no data for it)
                xrayTeByKey.TryGetValue(teKey, out var xrayTe);

                // We need issueId — use from Xray data if available, else skip
                // (issueId is required for the domain entity PK)
                var issueId = xrayTe?.IssueId;
                if (string.IsNullOrEmpty(issueId))
                {
                    // Fallback: try to find the ID from jira issue links
                    issueId = FindTeIssueId(jiraIssues, teKey);
                }

                if (string.IsNullOrEmpty(issueId))
                {
                    warnings.Add($"Could not determine issue ID for Test Execution {teKey} — skipped.");
                    continue;
                }

                // Upsert assignee developer if present
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

                // Step 6: Replace links for this TE
                var links = new List<TestExecutionLink>();
                if (teLinkMap.TryGetValue(teKey, out var ticketLinks))
                {
                    foreach (var (ticketKey, linkType) in ticketLinks)
                    {
                        links.Add(new TestExecutionLink
                        {
                            TestExecutionIssueId = issueId,
                            TicketKey = ticketKey,
                            LinkType = linkType
                        });
                    }
                }
                await testExecutionRepository.ReplaceLinksAsync(issueId, links, ct);
                await testExecutionRepository.SaveChangesAsync(ct);

                // Step 7: Replace test runs for this TE
                var runs = new List<TestRun>();
                if (xrayTe?.TestRuns is not null)
                {
                    foreach (var runDto in xrayTe.TestRuns)
                    {
                        var runStatus = ParseTestRunStatus(runDto.StatusName);

                        if (!string.IsNullOrEmpty(runDto.ExecutedById))
                            await UpsertDeveloperFromIdAsync(runDto.ExecutedById, ct);

                        runs.Add(TestRun.FromXray(
                            id: runDto.Id,
                            testExecutionIssueId: issueId,
                            status: runStatus,
                            statusName: runDto.StatusName,
                            startedAt: runDto.StartedAt,
                            finishedAt: runDto.FinishedAt,
                            executedById: runDto.ExecutedById
                        ));
                    }
                }
                await testExecutionRepository.ReplaceTestRunsAsync(issueId, runs, ct);
                await testExecutionRepository.SaveChangesAsync(ct);

                testExecutionsSynced++;
                testRunsSynced += runs.Count;
            }
            catch (Exception ex)
            {
                var warning = $"Failed to sync Test Execution {teKey}: {ex.Message}";
                logger.LogWarning(warning);
                warnings.Add(warning);
            }
        }

        // Step 8: Map and persist Test Sets
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

        return new XraySyncResult(testExecutionsSynced, testRunsSynced, testSetsSynced, warnings);
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

    private static TestRunStatus ParseTestRunStatus(string statusName)
    {
        return statusName.ToUpperInvariant() switch
        {
            "PASS" or "PASSED" => TestRunStatus.Pass,
            "FAIL" or "FAILED" => TestRunStatus.Fail,
            "EXECUTING" or "IN PROGRESS" => TestRunStatus.Executing,
            "ABORTED" or "ABANDONED" => TestRunStatus.Aborted,
            _ => TestRunStatus.Todo
        };
    }
}
