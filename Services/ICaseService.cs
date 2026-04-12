using secure_workflow_system.Data;

namespace secure_workflow_system.Services;

public interface ICaseService
{
    Task<Case> CreateCaseAsync(string userId, string title, string description);
    Task<IReadOnlyList<Case>> GetCasesForUserAsync(string userId);
    Task<IReadOnlyList<Case>> GetAllCasesAsync();
    Task<Case?> GetCaseByIdForUserAsync(int caseId, string userId);
    Task<Case?> GetCaseByIdAsync(int caseId);
    Task<bool> UpdateCaseStatusAndAssignmentAsync(int caseId, string status, string? assignedToUserId);
    Task<bool> UpdateCaseStatusAndAssignmentAsync(int caseId, string status, string? assignedToUserId, string changedByUserId);
    Task<IReadOnlyList<CaseStatusHistory>> GetCaseStatusHistoryAsync(int caseId);
}
