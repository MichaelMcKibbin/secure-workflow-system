using secure_workflow_system.Data;
using secure_workflow_system.Services;
using secure_workflow_system.Tests.Unit.Builders;
using secure_workflow_system.Tests.Unit.Infrastructure;
using Xunit;

namespace secure_workflow_system.Tests.Unit.ServiceTests;

public class CaseServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CaseService _service;

    public CaseServiceTests()
    {
        _context = TestDbContextFactory.Create($"TestDb_{Guid.NewGuid()}");
        _service = new CaseService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task CreateCaseAsync_WithValidData_ReturnsCaseWithCorrectFields()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var title = "Test Case";
        var description = "Test Description";

        // Act
        var result = await _service.CreateCaseAsync(userId, title, description);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(title, result.Title);
        Assert.Equal(description, result.Description);
        Assert.Equal(WorkflowState.New, result.Status);
        Assert.Equal(userId, result.CreatedByUserId);
    }

    [Fact]
    public async Task CreateCaseAsync_WithUntrimmedTitle_ReturnsTrimmedTitle()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await _service.CreateCaseAsync(userId, "  Untrimmed Title  ", "Description");

        // Assert
        Assert.Equal("Untrimmed Title", result.Title);
    }

    [Fact]
    public async Task CreateCaseAsync_WithUntrimmedDescription_ReturnsTrimmedDescription()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await _service.CreateCaseAsync(userId, "Title", "  Untrimmed Description  ");

        // Assert
        Assert.Equal("Untrimmed Description", result.Description);
    }

    [Fact]
    public async Task GetAllCasesAsync_WithMultipleCases_ReturnsAllCases()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        await _service.CreateCaseAsync(userId, "Case 1", "Description 1");
        await _service.CreateCaseAsync(userId, "Case 2", "Description 2");
        await _service.CreateCaseAsync(userId, "Case 3", "Description 3");

        // Act
        var result = await _service.GetAllCasesAsync();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllCasesAsync_WithMultipleCases_ReturnsNewestFirst()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var case1 = await _service.CreateCaseAsync(userId, "Case 1", "Description 1");
        await Task.Delay(10);
        var case2 = await _service.CreateCaseAsync(userId, "Case 2", "Description 2");
        await Task.Delay(10);
        var case3 = await _service.CreateCaseAsync(userId, "Case 3", "Description 3");

        // Act
        var result = await _service.GetAllCasesAsync();

        // Assert
        Assert.Equal(case3.Id, result[0].Id);
        Assert.Equal(case2.Id, result[1].Id);
        Assert.Equal(case1.Id, result[2].Id);
    }

    [Fact]
    public async Task GetCaseByIdAsync_WithValidId_ReturnsCorrectCase()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        var result = await _service.GetCaseByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Case", result.Title);
    }

    [Fact]
    public async Task GetCaseByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetCaseByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCasesForUserAsync_WithCasesCreatedByUser_ReturnsUserCases()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        await _service.CreateCaseAsync(userId, "My Case 1", "Description");
        await _service.CreateCaseAsync(userId, "My Case 2", "Description");
        await _service.CreateCaseAsync(otherUserId, "Other Case", "Description");

        // Act
        var result = await _service.GetCasesForUserAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal(userId, c.CreatedByUserId));
    }

    [Fact]
    public async Task GetCasesForUserAsync_WithCaseAssignedToUser_ReturnsAssignedCase()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var assigneeId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(creatorId, "Assigned Case", "Description");
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Assigned.ToString(), assigneeId);

        // Act
        var result = await _service.GetCasesForUserAsync(assigneeId);

        // Assert
        Assert.Single(result);
        Assert.Equal(created.Id, result[0].Id);
    }

    [Fact]
    public async Task GetCaseByIdForUserAsync_WithCaseCreatedByUser_ReturnsCase()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "My Case", "Description");

        // Act
        var result = await _service.GetCaseByIdForUserAsync(created.Id, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithValidTransition_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        var result = await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithValidTransition_UpdatesStatus()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), null);
        var updated = await _service.GetCaseByIdAsync(created.Id);

        // Assert
        Assert.Equal(WorkflowState.Assigned, updated!.Status);
    }

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithAssignedUser_SetsAssignedToUserId()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var assigneeId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(creatorId, "Case", "Description");

        // Act
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), assigneeId);
        var updated = await _service.GetCaseByIdAsync(created.Id);

        // Assert
        Assert.Equal(assigneeId, updated!.AssignedToUserId);
    }

    [Fact]
    public async Task GetCaseStatusHistoryAsync_AfterStatusChange_ReturnsHistoryEntry()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), null);

        // Act
        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert
        Assert.Single(history);
        Assert.Equal(WorkflowState.New.ToString(), history[0].OldStatus);
        Assert.Equal(WorkflowState.Assigned.ToString(), history[0].NewStatus);
    }

    #region Persistence

    [Fact]
    public async Task CreateCaseAsync_CaseIsRetrievableAfterCreation()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var created = await _service.CreateCaseAsync(userId, "Persisted Case", "Description");
        var retrieved = await _service.GetCaseByIdAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Persisted Case", retrieved.Title);
    }

    [Fact]
    public async Task CreateCaseAsync_TwoCasesWithSameTitleDifferentCreators_AreStoredAsSeparateCases()
    {
        // Arrange
        var userId1 = Guid.NewGuid().ToString();
        var userId2 = Guid.NewGuid().ToString();

        // Act
        var case1 = await _service.CreateCaseAsync(userId1, "Duplicate Title", "Description");
        var case2 = await _service.CreateCaseAsync(userId2, "Duplicate Title", "Description");
        var all = await _service.GetAllCasesAsync();

        // Assert
        Assert.NotEqual(case1.Id, case2.Id);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task CreateCaseAsync_TwoCasesWithSameTitleSameCreator_AreStoredAsSeparateCases()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var case1 = await _service.CreateCaseAsync(userId, "Same Title", "Description 1");
        var case2 = await _service.CreateCaseAsync(userId, "Same Title", "Description 2");
        var all = await _service.GetAllCasesAsync();

        // Assert
        Assert.NotEqual(case1.Id, case2.Id);
        Assert.Equal(2, all.Count);
    }

    #endregion

    #region Creator Equals Assignee

    [Fact]
    public async Task GetCasesForUserAsync_WhenUserIsCreatorAndAssignee_ReturnsOnlyOnce()
    {
        // Arrange — user creates a case then it gets assigned back to themselves
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Self Assigned Case", "Description");
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), userId);

        // Act
        var result = await _service.GetCasesForUserAsync(userId);

        // Assert — should not appear twice
        Assert.Single(result);
        Assert.Equal(created.Id, result[0].Id);
    }

    #endregion

    #region Builder Integration

    [Fact]
    public void CaseBuilder_ProducesExpectedCaseForTestSetup()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var caseObj = new CaseBuilder()
            .WithTitle("Builder Case")
            .WithStatus(WorkflowState.InProgress)
            .WithCreatedByUserId(userId)
            .Build();

        // Assert
        Assert.Equal("Builder Case", caseObj.Title);
        Assert.Equal(WorkflowState.InProgress, caseObj.Status);
        Assert.Equal(userId, caseObj.CreatedByUserId);
    }

    #endregion
}
