using secure_workflow_system.Data;
using secure_workflow_system.Services;
using secure_workflow_system.Tests.Unit.Builders;
using secure_workflow_system.Tests.Unit.Infrastructure;
using Xunit;

namespace secure_workflow_system.Tests.Unit.ServiceTests;

public class CaseStatusHistoryTrackingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CaseService _service;

    public CaseStatusHistoryTrackingTests()
    {
        _context = TestDbContextFactory.Create($"TestDb_{Guid.NewGuid()}");
        _service = new CaseService(_context);
    }

    public void Dispose() => _context.Dispose();

    #region Full Workflow Sequence

    [Fact]
    public async Task FullWorkflow_StandardPath_ProducesCorrectHistorySequence()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Assigned.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.InProgress.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Resolved.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Closed.ToString(), null);

        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert — history is newest first, so reverse order of transitions
        Assert.Equal(4, history.Count);
        Assert.Equal(WorkflowState.Resolved.ToString(), history[0].OldStatus);
        Assert.Equal(WorkflowState.Closed.ToString(), history[0].NewStatus);
        Assert.Equal(WorkflowState.InProgress.ToString(), history[1].OldStatus);
        Assert.Equal(WorkflowState.Resolved.ToString(), history[1].NewStatus);
        Assert.Equal(WorkflowState.Assigned.ToString(), history[2].OldStatus);
        Assert.Equal(WorkflowState.InProgress.ToString(), history[2].NewStatus);
        Assert.Equal(WorkflowState.New.ToString(), history[3].OldStatus);
        Assert.Equal(WorkflowState.Assigned.ToString(), history[3].NewStatus);
    }

    [Fact]
    public async Task FullWorkflow_WithBackTransition_ProducesCorrectHistorySequence()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act — walk forward then back from Resolved to InProgress
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Assigned.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.InProgress.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Resolved.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.InProgress.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Resolved.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Closed.ToString(), null);

        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert
        Assert.Equal(6, history.Count);
        Assert.Equal(WorkflowState.Resolved.ToString(), history[0].OldStatus);
        Assert.Equal(WorkflowState.Closed.ToString(), history[0].NewStatus);
        Assert.Equal(WorkflowState.InProgress.ToString(), history[1].OldStatus);
        Assert.Equal(WorkflowState.Resolved.ToString(), history[1].NewStatus);
        Assert.Equal(WorkflowState.Resolved.ToString(), history[2].OldStatus);
        Assert.Equal(WorkflowState.InProgress.ToString(), history[2].NewStatus);
    }

    [Fact]
    public async Task FullWorkflow_FinalStatus_IsClosedAfterFullPath()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Assigned.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.InProgress.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Resolved.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Closed.ToString(), null);

        var result = await _service.GetCaseByIdAsync(created.Id);

        // Assert
        Assert.Equal(WorkflowState.Closed, result!.Status);
    }

    #endregion

    #region ChangedAtUtc

    [Fact]
    public async Task GetCaseStatusHistoryAsync_AfterTransition_ChangedAtUtcIsApproximatelyNow()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");
        var before = DateTime.UtcNow;

        // Act
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Assigned.ToString(), null);
        var after = DateTime.UtcNow;
        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert
        Assert.InRange(history[0].ChangedAtUtc, before, after);
    }

    [Fact]
    public async Task GetCaseStatusHistoryAsync_MultipleTransitions_ChangedAtUtcAdvancesOverTime()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Assigned.ToString(), null);
        await Task.Delay(10);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.InProgress.ToString(), null);

        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert — history[0] is newest, so its ChangedAtUtc should be later
        Assert.True(history[0].ChangedAtUtc >= history[1].ChangedAtUtc);
    }

    #endregion

    #region Builder Usage

    [Fact]
    public void CaseBuilder_WithDefaults_BuildsValidCase()
    {
        // Act
        var caseObj = new CaseBuilder().Build();

        // Assert
        Assert.NotNull(caseObj);
        Assert.Equal("Test Case", caseObj.Title);
        Assert.Equal("Test Description", caseObj.Description);
        Assert.Equal(WorkflowState.New, caseObj.Status);
        Assert.Null(caseObj.AssignedToUserId);
    }

    [Fact]
    public void CaseBuilder_WithCustomValues_BuildsCaseWithCorrectFields()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var assigneeId = Guid.NewGuid().ToString();
        var createdAt = DateTime.UtcNow.AddDays(-5);

        // Act
        var caseObj = new CaseBuilder()
            .WithId(42)
            .WithTitle("Custom Title")
            .WithDescription("Custom Description")
            .WithStatus(WorkflowState.InProgress)
            .WithCreatedByUserId(userId)
            .WithAssignedToUserId(assigneeId)
            .WithCreatedAtUtc(createdAt)
            .Build();

        // Assert
        Assert.Equal(42, caseObj.Id);
        Assert.Equal("Custom Title", caseObj.Title);
        Assert.Equal("Custom Description", caseObj.Description);
        Assert.Equal(WorkflowState.InProgress, caseObj.Status);
        Assert.Equal(userId, caseObj.CreatedByUserId);
        Assert.Equal(assigneeId, caseObj.AssignedToUserId);
        Assert.Equal(createdAt, caseObj.CreatedAtUtc);
    }

    [Fact]
    public void CaseStatusHistoryBuilder_WithDefaults_BuildsValidHistory()
    {
        // Act
        var history = new CaseStatusHistoryBuilder().Build();

        // Assert
        Assert.NotNull(history);
        Assert.Equal(WorkflowState.New.ToString(), history.OldStatus);
        Assert.Equal(WorkflowState.Assigned.ToString(), history.NewStatus);
    }

    [Fact]
    public void CaseStatusHistoryBuilder_WithCustomValues_BuildsHistoryWithCorrectFields()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var changedAt = DateTime.UtcNow.AddHours(-2);

        // Act
        var history = new CaseStatusHistoryBuilder()
            .WithId(10)
            .WithCaseId(5)
            .WithOldStatus(WorkflowState.InProgress)
            .WithNewStatus(WorkflowState.Resolved)
            .WithChangedByUserId(userId)
            .WithChangedAtUtc(changedAt)
            .Build();

        // Assert
        Assert.Equal(10, history.Id);
        Assert.Equal(5, history.CaseId);
        Assert.Equal(WorkflowState.InProgress.ToString(), history.OldStatus);
        Assert.Equal(WorkflowState.Resolved.ToString(), history.NewStatus);
        Assert.Equal(userId, history.ChangedByUserId);
        Assert.Equal(changedAt, history.ChangedAtUtc);
    }

    #endregion
}
