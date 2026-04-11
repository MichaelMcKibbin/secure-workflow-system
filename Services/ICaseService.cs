using secure_workflow_system.Data;

namespace secure_workflow_system.Services;

public interface ICaseService
{
    Task<Case> CreateCaseAsync(string userId, string title, string description);
    Task<IReadOnlyList<Case>> GetCasesForUserAsync(string userId);
    Task<Case?> GetCaseByIdForUserAsync(int caseId, string userId);
}
