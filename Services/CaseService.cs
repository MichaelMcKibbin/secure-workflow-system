using Microsoft.EntityFrameworkCore;
using secure_workflow_system.Data;

namespace secure_workflow_system.Services;

public class CaseService(ApplicationDbContext dbContext) : ICaseService
{
    public async Task<Case> CreateCaseAsync(string userId, string title, string description)
    {
        var utcNow = DateTime.UtcNow;

        var workflowCase = new Case
        {
            Title = title.Trim(),
            Description = description.Trim(),
            Status = WorkflowState.New,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            CreatedByUserId = userId
        };

        dbContext.Cases.Add(workflowCase);
        await dbContext.SaveChangesAsync();

        return workflowCase;
    }

    public async Task<IReadOnlyList<Case>> GetCasesForUserAsync(string userId)
    {
        return await dbContext.Cases
            .AsNoTracking()
            .Where(c => c.CreatedByUserId == userId || c.AssignedToUserId == userId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Case>> GetAllCasesAsync()
    {
        return await dbContext.Cases
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<Case?> GetCaseByIdForUserAsync(int caseId, string userId)
    {
        return await dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId && (c.CreatedByUserId == userId || c.AssignedToUserId == userId));
    }

    public async Task<Case?> GetCaseByIdAsync(int caseId)
    {
        return await dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId);
    }

    public async Task<bool> UpdateCaseStatusAndAssignmentAsync(int caseId, string status, string? assignedToUserId)
    {
        var workflowCase = await dbContext.Cases.FirstOrDefaultAsync(c => c.Id == caseId);
        if (workflowCase is null)
        {
            return false;
        }

        if (!Enum.TryParse<WorkflowState>(status.Trim(), ignoreCase: true, out var newStatus))
        {
            return false;
        }

        workflowCase.Status = newStatus;
        workflowCase.AssignedToUserId = string.IsNullOrWhiteSpace(assignedToUserId) ? null : assignedToUserId;
        workflowCase.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateCaseStatusAndAssignmentAsync(int caseId, string status, string? assignedToUserId, string changedByUserId)
    {
        var workflowCase = await dbContext.Cases.FirstOrDefaultAsync(c => c.Id == caseId);
        if (workflowCase is null)
        {
            return false;
        }

        if (!Enum.TryParse<WorkflowState>(status.Trim(), ignoreCase: true, out var newStatus))
        {
            return false;
        }

        var statusChanged = workflowCase.Status != newStatus;

        if (statusChanged && !Case.IsValidTransition(workflowCase.Status, newStatus))
        {
            return false;
        }

        if (statusChanged)
        {
            var history = new CaseStatusHistory
            {
                CaseId = caseId,
                OldStatus = workflowCase.Status.ToString(),
                NewStatus = newStatus.ToString(),
                ChangedByUserId = changedByUserId,
                ChangedAtUtc = DateTime.UtcNow
            };
            dbContext.CaseStatusHistories.Add(history);
        }

        workflowCase.Status = newStatus;
        workflowCase.AssignedToUserId = string.IsNullOrWhiteSpace(assignedToUserId) ? null : assignedToUserId;
        workflowCase.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<CaseStatusHistory>> GetCaseStatusHistoryAsync(int caseId)
    {
        return await dbContext.CaseStatusHistories
            .AsNoTracking()
            .Where(h => h.CaseId == caseId)
            .OrderByDescending(h => h.ChangedAtUtc)
            .ToListAsync();
    }
}
