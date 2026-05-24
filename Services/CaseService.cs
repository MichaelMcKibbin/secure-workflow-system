using Microsoft.EntityFrameworkCore;
using secure_workflow_system.Data;

namespace secure_workflow_system.Services;

public class CaseService(ApplicationDbContext dbContext) : ICaseService
{
    public async Task<Case> CreateCaseAsync(string userId, string title, string description)
    {
        var utcNow = DateTime.UtcNow;
        await EnsureUserExistsAsync(userId);

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
            .Include(c => c.CreatedByUser)
            .Include(c => c.AssignedToUser)
            .FirstOrDefaultAsync(c => c.Id == caseId && (c.CreatedByUserId == userId || c.AssignedToUserId == userId));
    }

    public async Task<Case?> GetCaseByIdAsync(int caseId)
    {
        return await dbContext.Cases
            .AsNoTracking()
            .Include(c => c.CreatedByUser)
            .Include(c => c.AssignedToUser)
            .FirstOrDefaultAsync(c => c.Id == caseId);
    }

    public async Task<bool> UpdateCaseStatusAndAssignmentAsync(int caseId, string status, string? assignedToUserId)
    {
        var workflowCase = await dbContext.Cases.FirstOrDefaultAsync(c => c.Id == caseId);
        if (workflowCase is null)
        {
            return false;
        }

        var changedByUserId = string.IsNullOrWhiteSpace(assignedToUserId)
            ? workflowCase.CreatedByUserId
            : assignedToUserId;

        return await UpdateCaseStatusAndAssignmentCoreAsync(workflowCase, status, assignedToUserId, changedByUserId);
    }

    public async Task<bool> UpdateCaseStatusAndAssignmentAsync(int caseId, string status, string? assignedToUserId, string changedByUserId)
    {
        var workflowCase = await dbContext.Cases.FirstOrDefaultAsync(c => c.Id == caseId);
        if (workflowCase is null)
        {
            return false;
        }

        return await UpdateCaseStatusAndAssignmentCoreAsync(workflowCase, status, assignedToUserId, changedByUserId);
    }

    public async Task<IReadOnlyList<CaseStatusHistory>> GetCaseStatusHistoryAsync(int caseId)
    {
        return await dbContext.CaseStatusHistories
            .AsNoTracking()
            .Include(h => h.ChangedByUser)
            .Where(h => h.CaseId == caseId)
            .OrderByDescending(h => h.ChangedAtUtc)
            .ToListAsync();
    }

    private async Task<bool> UpdateCaseStatusAndAssignmentCoreAsync(Case workflowCase, string status, string? assignedToUserId, string changedByUserId)
    {
        if (!Enum.TryParse<WorkflowState>(status.Trim(), ignoreCase: true, out var newStatus))
        {
            return false;
        }

        var statusChanged = workflowCase.Status != newStatus;

        if (statusChanged && !Case.IsValidTransition(workflowCase.Status, newStatus))
        {
            return false;
        }

        var effectiveChangedByUserId = string.IsNullOrWhiteSpace(changedByUserId)
            ? workflowCase.CreatedByUserId
            : changedByUserId;

        await EnsureUserExistsAsync(workflowCase.CreatedByUserId);
        await EnsureUserExistsAsync(assignedToUserId);
        await EnsureUserExistsAsync(effectiveChangedByUserId);

        if (statusChanged)
        {
            var history = new CaseStatusHistory
            {
                CaseId = workflowCase.Id,
                OldStatus = workflowCase.Status.ToString(),
                NewStatus = newStatus.ToString(),
                ChangedByUserId = effectiveChangedByUserId,
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

    private async Task EnsureUserExistsAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var alreadyTracked = dbContext.Users.Local.Any(u => u.Id == userId);
        if (alreadyTracked)
        {
            return;
        }

        var exists = await dbContext.Users.AnyAsync(user => user.Id == userId);
        if (exists)
        {
            return;
        }

        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = userId,
            NormalizedUserName = userId.ToUpperInvariant()
        });
    }
}
