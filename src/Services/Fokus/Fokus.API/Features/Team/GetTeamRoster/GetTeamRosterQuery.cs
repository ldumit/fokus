namespace Fokus.API.Features.Team.GetTeamRoster;

public class GetTeamRosterResponse
{
    public List<TeamDeveloperDto> Developers { get; set; } = new();
    public List<string> SubTeams { get; set; } = new();
}
