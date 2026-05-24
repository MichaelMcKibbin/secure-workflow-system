using secure_workflow_system.Data;
using secure_workflow_system.Services;
using secure_workflow_system.Tests.Unit.Infrastructure;
using Xunit;

namespace secure_workflow_system.Tests.Unit.ServiceTests;

public class CaseStatusHistoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CaseService _service;

    public CaseStatusHistoryTests()
    {
        _context = TestDbContextFactory.Create($"TestDb_{Guid.NewGuid()}");
        _service = new CaseService(_context);
    }

    public void Dispose() => _context.Dispose();

    #region History Content

    [Fact]
    public async Task GetCaseStatusHistoryAsync_AfterTransition_RecordsCorrectOldAndNewStatus()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), null);
        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert
        Assert.Equal(WorkflowState.New.ToString(), history[0].OldStatus);
        Assert.Equal(WorkflowState.Assigned.ToString(), history[0].NewStatus);
    }

    [Fact]
    public async Task GetCaseStatusHistoryAsync_AfterTransition_RecordsChangedByUserId()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var staffId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(creatorId, "Case", "Description");

        // Act
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), null, staffId);
        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert
        Assert.Equal(staffId, history[0].ChangedByUserId);
    }

    [Fact]
    public async Task GetCaseStatusHistoryAsync_AfterTransition_RecordsCorrectCaseId()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), null);
        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert
        Assert.Equal(created.Id, history[0].CaseId);
    }

    [Fact]
    public async Task GetCaseStatusHistoryAsync_AfterResolvedToInProgressTransition_RecordsEntry()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Assigned.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.InProgress.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Resolved.ToString(), null);

        // Act — back-transition
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.InProgress.ToString(), null);
        var history = await _service.GetCaseStatusHistoryAsync(created.Id);
        var lastEntry = history[0];

        // Assert
        Assert.Equal(WorkflowState.Resolved.ToString(), lastEntry.OldStatus);
        Assert.Equal(WorkflowState.InProgress.ToString(), lastEntry.NewStatus);
    }

    #endregion

    #region History Ordering

    [Fact]
    public async Task GetCaseStatusHistoryAsync_WithMultipleTransitions_ReturnsNewestFirst()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Assigned.ToString(), null);
        await Task.Delay(10);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.InProgress.ToString(), null);
        await Task.Delay(10);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Resolved.ToString(), null);

        // Act
        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert — newest first
        Assert.Equal(WorkflowState.Resolved.ToString(), history[0].NewStatus);
        Assert.Equal(WorkflowState.InProgress.ToString(), history[1].NewStatus);
        Assert.Equal(WorkflowState.Assigned.ToString(), history[2].NewStatus);
    }

    [Fact]
    public async Task GetCaseStatusHistoryAsync_IsIsolatedPerCase()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var case1 = await _service.CreateCaseAsync(userId, "Case 1", "Description");
        var case2 = await _service.CreateCaseAsync(userId, "Case 2", "Description");
        await _service.UpdateCaseStatusAndAssignmentAsync(case1.Id, WorkflowState.Assigned.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(case1.Id, WorkflowState.InProgress.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(case2.Id, WorkflowState.Assigned.ToString(), null);

        // Act
        var history1 = await _service.GetCaseStatusHistoryAsync(case1.Id);
        var history2 = await _service.GetCaseStatusHistoryAsync(case2.Id);

        // Assert
        Assert.Equal(2, history1.Count);
        Assert.Single(history2);
    }

    #endregion

    #region ChangedByUserId Overload

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithChangedByUserIdOverload_ReturnsTrue()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var staffId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(creatorId, "Case", "Description");

        // Act
        var result = await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), null, staffId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithChangedByUserIdOverload_UpdatesStatus()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var staffId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(creatorId, "Case", "Description");

        // Act
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), null, staffId);
        var updated = await _service.GetCaseByIdAsync(created.Id);

        // Assert
        Assert.Equal(WorkflowState.Assigned, updated!.Status);
    }

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithChangedByUserIdOverload_WithNonExistentCase_ReturnsFalse()
    {
        // Arrange
        var staffId = Guid.NewGuid().ToString();

        // Act
        var result = await _service.UpdateCaseStatusAndAssignmentAsync(
            999, WorkflowState.Assigned.ToString(), null, staffId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Navigation Properties

    [Fact]
    public async Task GetCaseByIdAsync_ReturnsCreatedByUserNavigationProperty()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        var result = await _service.GetCaseByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result!.CreatedByUser);
        Assert.Equal(userId, result.CreatedByUser.Id);
    }

    [Fact]
    public async Task GetCaseByIdAsync_WithAssignedUser_ReturnsAssignedToUserNavigationProperty()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var assigneeId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(creatorId, "Case", "Description");
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), assigneeId);

        // Act
        var result = await _service.GetCaseByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result!.AssignedToUser);
        Assert.Equal(assigneeId, result.AssignedToUser!.Id);
    }

    [Fact]
    public async Task GetCaseByIdForUserAsync_ReturnsCreatedByUserNavigationProperty()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        var result = await _service.GetCaseByIdForUserAsync(created.Id, userId);

        // Assert
        Assert.NotNull(result!.CreatedByUser);
        Assert.Equal(userId, result.CreatedByUser.Id);
    }

    #endregion

    #region Timestamps

    [Fact]
    public async Task CreateCaseAsync_SetsCreatedAtUtcToApproximatelyNow()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var before = DateTime.UtcNow;

        // Act
        var result = await _service.CreateCaseAsync(userId, "Case", "Description");
        var after = DateTime.UtcNow;

        // Assert
        Assert.InRange(result.CreatedAtUtc, before, after);
    }

    [Fact]
    public async Task CreateCaseAsync_SetsUpdatedAtUtcToApproximatelyNow()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var before = DateTime.UtcNow;

        // Act
        var result = await _service.CreateCaseAsync(userId, "Case", "Description");
        var after = DateTime.UtcNow;

        // Assert
        Assert.NotNull(result.UpdatedAtUtc);
        Assert.InRange(result.UpdatedAtUtc!.Value, before, after);
    }

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_UpdatesUpdatedAtUtc()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");
        var originalUpdatedAt = created.UpdatedAtUtc;
        await Task.Delay(10);

        // Act
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), null);
        var updated = await _service.GetCaseByIdAsync(created.Id);

        // Assert
        Assert.NotNull(updated!.UpdatedAtUtc);
        Assert.True(updated.UpdatedAtUtc > originalUpdatedAt);
    }

    #endregion
}
