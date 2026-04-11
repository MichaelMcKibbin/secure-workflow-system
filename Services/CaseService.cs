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
            Status = "New",
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
            .Where(c => c.CreatedByUserId == userId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<Case?> GetCaseByIdForUserAsync(int caseId, string userId)
    {
        return await dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId && c.CreatedByUserId == userId);
    }
}
