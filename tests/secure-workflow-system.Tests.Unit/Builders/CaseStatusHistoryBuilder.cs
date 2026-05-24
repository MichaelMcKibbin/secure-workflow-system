using secure_workflow_system.Data;

namespace secure_workflow_system.Tests.Unit.Builders;

public class CaseStatusHistoryBuilder
{
    private int _id = 1;
    private int _caseId = 1;
    private string _oldStatus = WorkflowState.New.ToString();
    private string _newStatus = WorkflowState.Assigned.ToString();
    private string _changedByUserId = Guid.NewGuid().ToString();
    private DateTime _changedAtUtc = DateTime.UtcNow;

    public CaseStatusHistoryBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public CaseStatusHistoryBuilder WithCaseId(int caseId)
    {
        _caseId = caseId;
        return this;
    }

    public CaseStatusHistoryBuilder WithOldStatus(WorkflowState oldStatus)
    {
        _oldStatus = oldStatus.ToString();
        return this;
    }

    public CaseStatusHistoryBuilder WithNewStatus(WorkflowState newStatus)
    {
        _newStatus = newStatus.ToString();
        return this;
    }

    public CaseStatusHistoryBuilder WithChangedByUserId(string userId)
    {
        _changedByUserId = userId;
        return this;
    }

    public CaseStatusHistoryBuilder WithChangedAtUtc(DateTime changedAtUtc)
    {
        _changedAtUtc = changedAtUtc;
        return this;
    }

    public CaseStatusHistory Build() => new()
    {
        Id = _id,
        CaseId = _caseId,
        OldStatus = _oldStatus,
        NewStatus = _newStatus,
        ChangedByUserId = _changedByUserId,
        ChangedAtUtc = _changedAtUtc
    };
}
