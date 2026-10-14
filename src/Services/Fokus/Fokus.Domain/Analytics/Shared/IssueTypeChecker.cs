namespace Fokus.Domain.Analytics.Shared;

public static class IssueTypeChecker
{
    public static bool IsBug(SprintMembership m) =>
        m.Ticket?.IssueType == "Bug";
}
