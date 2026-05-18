using secure_workflow_system.Data;

namespace secure_workflow_system.Tests.Unit.Builders;

/// <summary>
/// Fluent builder for creating test CaseStatusHistory objects
/// </summary>
public class CaseStatusHistoryBuilder
{
    private CaseStatusHistory _history = new();

    public CaseStatusHistoryBuilder()
    {
        _history = new CaseStatusHistory
        {
            Id = 1,
            CaseId = 1,
            OldStatus = WorkflowState.New.ToString(),
            NewStatus = WorkflowState.Assigned.ToString(),
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = Guid.NewGuid().ToString(),
            ChangedByUser = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "admin" }
        };
    }

    public CaseStatusHistoryBuilder WithId(int id)
    {
        _history.Id = id;
        return this;
    }

    public CaseStatusHistoryBuilder WithCaseId(int caseId)
    {
        _history.CaseId = caseId;
        return this;
    }

    public CaseStatusHistoryBuilder WithOldStatus(WorkflowState oldStatus)
    {
        _history.OldStatus = oldStatus.ToString();
        return this;
    }

    public CaseStatusHistoryBuilder WithNewStatus(WorkflowState newStatus)
    {
        _history.NewStatus = newStatus.ToString();
        return this;
    }

    public CaseStatusHistoryBuilder WithChangedBy(string userId, string userName = "admin")
    {
        _history.ChangedByUserId = userId;
        _history.ChangedByUser = new ApplicationUser { Id = userId, UserName = userName };
        return this;
    }

    public CaseStatusHistoryBuilder WithChangedAt(DateTime changedAt)
    {
        _history.ChangedAtUtc = changedAt;
        return this;
    }

    public CaseStatusHistory Build()
    {
        return _history;
    }
}
