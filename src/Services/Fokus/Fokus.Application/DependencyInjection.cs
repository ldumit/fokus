using Fokus.Application.Analytics.Developers;
using Fokus.Application.Analytics.Qa;
using Fokus.Application.Analytics.Sprints;
using Fokus.Application.Sync;
using Fokus.Application.Xray;
using Fokus.Domain.Analytics.Developers;
using Fokus.Domain.Analytics.Qa;
using Fokus.Domain.Analytics.Sprints;
using Fokus.Domain.Analytics.Workflow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fokus.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFokusApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<SprintSummaryCalculator>();
        services.AddScoped<ScopeChangeCalculator>();
        services.AddScoped<CarryOverCalculator>();
        services.AddScoped<BugRatioCalculator>();
        services.AddScoped<CycleTimeCalculator>();
        services.AddScoped<EpicProgressCalculator>();
        services.AddScoped<TestTimelineCalculator>();
        services.AddScoped<DeveloperProgressCalculator>();
        services.AddScoped<DeveloperDetailCalculator>();
        services.AddScoped<DeveloperQualityCalculator>();
        services.AddScoped<DeveloperThroughputCalculator>();
        services.AddScoped<LeaderboardCalculator>();
        services.AddScoped<QaMetricsCalculator>();
        services.AddScoped<QaTrendsCalculator>();
        services.AddScoped<QaWorkloadCalculator>();
        services.AddScoped<WorkflowStageResolver>();

        services.AddScoped<SprintSummaryInputs>();
        services.AddScoped<DeveloperDetailInputs>();
        services.AddScoped<DeveloperQualityInputs>();
        services.AddScoped<QaMetricsInputs>();
        services.AddScoped<QaTrendsInputs>();
        services.AddScoped<QaWorkloadInputs>();

        services.AddScoped<SprintIssueSyncService>();
        services.AddScoped<XrayIssueSyncService>();

        return services;
    }
}
