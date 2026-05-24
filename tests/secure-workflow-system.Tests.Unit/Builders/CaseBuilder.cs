using secure_workflow_system.Data;

namespace secure_workflow_system.Tests.Unit.Builders;

public class CaseBuilder
{
    private int _id = 1;
    private string _title = "Test Case";
    private string _description = "Test Description";
    private WorkflowState _status = WorkflowState.New;
    private DateTime _createdAtUtc = DateTime.UtcNow;
    private DateTime? _updatedAtUtc = null;
    private string _createdByUserId = Guid.NewGuid().ToString();
    private string? _assignedToUserId = null;

    public CaseBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public CaseBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public CaseBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CaseBuilder WithStatus(WorkflowState status)
    {
        _status = status;
        return this;
    }

    public CaseBuilder WithCreatedByUserId(string userId)
    {
        _createdByUserId = userId;
        return this;
    }

    public CaseBuilder WithAssignedToUserId(string? userId)
    {
        _assignedToUserId = userId;
        return this;
    }

    public CaseBuilder WithCreatedAtUtc(DateTime createdAtUtc)
    {
        _createdAtUtc = createdAtUtc;
        return this;
    }

    public CaseBuilder WithUpdatedAtUtc(DateTime? updatedAtUtc)
    {
        _updatedAtUtc = updatedAtUtc;
        return this;
    }

    public Case Build() => new()
    {
        Id = _id,
        Title = _title,
        Description = _description,
        Status = _status,
        CreatedAtUtc = _createdAtUtc,
        UpdatedAtUtc = _updatedAtUtc,
        CreatedByUserId = _createdByUserId,
        AssignedToUserId = _assignedToUserId
    };
}
