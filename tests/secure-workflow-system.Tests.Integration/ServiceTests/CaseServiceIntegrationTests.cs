using Microsoft.EntityFrameworkCore;
using secure_workflow_system.Data;
using secure_workflow_system.Services;
using secure_workflow_system.Tests.Integration.Infrastructure;
using Xunit;

namespace secure_workflow_system.Tests.Integration.ServiceTests;

[Collection("PostgreSQL Collection")]
public class CaseServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private ApplicationDbContext _context = null!;
    private CaseService _service = null!;

    public CaseServiceIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _context = _fixture.CreateContext();
        _service = new CaseService(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Clean up all data between tests so they remain isolated
        _context.CaseStatusHistories.RemoveRange(_context.CaseStatusHistories);
        _context.Cases.RemoveRange(_context.Cases);
        _context.Users.RemoveRange(_context.Users);
        await _context.SaveChangesAsync();
        await _context.DisposeAsync();
    }

    #region Persistence

    [Fact]
    public async Task CreateCaseAsync_WithValidData_PersistsToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var created = await _service.CreateCaseAsync(userId, "Integration Case", "Description");

        // Assert — verify with a fresh context to confirm it was actually saved
        await using var verifyContext = _fixture.CreateContext();
        var persisted = await verifyContext.Cases.FindAsync(created.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Integration Case", persisted.Title);
    }

    [Fact]
    public async Task CreateCaseAsync_AutoCreatesUserIfNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        await _service.CreateCaseAsync(userId, "Case", "Description");

        // Assert
        await using var verifyContext = _fixture.CreateContext();
        var userExists = await verifyContext.Users.AnyAsync(u => u.Id == userId);
        Assert.True(userExists);
    }

    [Fact]
    public async Task CreateCaseAsync_WithExistingUser_DoesNotCreateDuplicateUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        await _service.CreateCaseAsync(userId, "Case 1", "Description");
        await _service.CreateCaseAsync(userId, "Case 2", "Description");

        // Assert
        await using var verifyContext = _fixture.CreateContext();
        var userCount = await verifyContext.Users.CountAsync(u => u.Id == userId);
        Assert.Equal(1, userCount);
    }

    #endregion

    #region Relationships

    [Fact]
    public async Task GetCaseByIdAsync_EagerLoadsCreatedByUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        var result = await _service.GetCaseByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CreatedByUser);
        Assert.Equal(userId, result.CreatedByUser.Id);
    }

    [Fact]
    public async Task GetCaseByIdAsync_WithAssignedUser_EagerLoadsAssignedToUser()
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
        Assert.NotNull(result);
        Assert.NotNull(result.AssignedToUser);
        Assert.Equal(assigneeId, result.AssignedToUser!.Id);
    }

    [Fact]
    public async Task GetCaseStatusHistoryAsync_EagerLoadsChangedByUser()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var staffId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(creatorId, "Case", "Description");
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), null, staffId);

        // Act
        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert
        Assert.Single(history);
        Assert.NotNull(history[0].ChangedByUser);
        Assert.Equal(staffId, history[0].ChangedByUser.Id);
    }

    #endregion

    #region Constraints

    [Fact]
    public async Task CaseStatusHistory_CascadeDeletesWhenCaseIsDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), null);

        // Act — delete the case directly via context
        var caseToDelete = await _context.Cases.FindAsync(created.Id);
        _context.Cases.Remove(caseToDelete!);
        await _context.SaveChangesAsync();

        // Assert — history should be cascade deleted
        await using var verifyContext = _fixture.CreateContext();
        var historyCount = await verifyContext.CaseStatusHistories
            .CountAsync(h => h.CaseId == created.Id);
        Assert.Equal(0, historyCount);
    }

    [Fact]
    public async Task Cases_WithSameTitleAndDifferentCreators_AreStoredSeparately()
    {
        // Arrange
        var userId1 = Guid.NewGuid().ToString();
        var userId2 = Guid.NewGuid().ToString();

        // Act
        var case1 = await _service.CreateCaseAsync(userId1, "Same Title", "Description");
        var case2 = await _service.CreateCaseAsync(userId2, "Same Title", "Description");

        // Assert
        Assert.NotEqual(case1.Id, case2.Id);
        await using var verifyContext = _fixture.CreateContext();
        var count = await verifyContext.Cases.CountAsync(c => c.Title == "Same Title");
        Assert.Equal(2, count);
    }

    #endregion

    #region Ordering

    [Fact]
    public async Task GetAllCasesAsync_ReturnsNewestFirst()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var case1 = await _service.CreateCaseAsync(userId, "Case 1", "Description");
        await Task.Delay(10);
        var case2 = await _service.CreateCaseAsync(userId, "Case 2", "Description");
        await Task.Delay(10);
        var case3 = await _service.CreateCaseAsync(userId, "Case 3", "Description");

        // Act
        var result = await _service.GetAllCasesAsync();

        // Assert
        Assert.Equal(case3.Id, result[0].Id);
        Assert.Equal(case2.Id, result[1].Id);
        Assert.Equal(case1.Id, result[2].Id);
    }

    [Fact]
    public async Task GetCaseStatusHistoryAsync_ReturnsNewestFirst()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Assigned.ToString(), null);
        await Task.Delay(10);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.InProgress.ToString(), null);

        // Act
        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert
        Assert.Equal(WorkflowState.InProgress.ToString(), history[0].NewStatus);
        Assert.Equal(WorkflowState.Assigned.ToString(), history[1].NewStatus);
    }

    #endregion

    #region Full Workflow

    [Fact]
    public async Task FullWorkflow_StandardPath_PersistsAllStateCorrectly()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var staffId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(creatorId, "Workflow Case", "Description");

        // Act — walk through full workflow
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), staffId, staffId);
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.InProgress.ToString(), staffId, staffId);
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Resolved.ToString(), null, staffId);
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Closed.ToString(), null, staffId);

        // Assert — verify final state with fresh context
        await using var verifyContext = _fixture.CreateContext();
        var finalCase = await verifyContext.Cases.FindAsync(created.Id);
        var history = await verifyContext.CaseStatusHistories
            .Where(h => h.CaseId == created.Id)
            .OrderByDescending(h => h.ChangedAtUtc)
            .ToListAsync();

        Assert.Equal(WorkflowState.Closed, finalCase!.Status);
        Assert.Equal(4, history.Count);
        Assert.Equal(WorkflowState.Closed.ToString(), history[0].NewStatus);
    }

    #endregion
}
