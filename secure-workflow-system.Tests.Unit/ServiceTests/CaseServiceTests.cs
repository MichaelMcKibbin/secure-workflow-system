using Xunit;
using secure_workflow_system.Data;
using secure_workflow_system.Services;
using secure_workflow_system.Tests.Unit.Builders;
using secure_workflow_system.Tests.Unit.Infrastructure;

namespace secure_workflow_system.Tests.Unit.ServiceTests;

public class CaseServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CaseService _caseService;

    public CaseServiceTests()
    {
        _context = TestDbContextFactory.CreateAndSeedTestContext($"TestDb_{Guid.NewGuid()}");
        _caseService = new CaseService(_context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region CreateCaseAsync Tests

    [Fact]
    public async Task CreateCaseAsync_WithValidData_ShouldCreateCase()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var title = "Important Case";
        var description = "Case Description";

        // Act
        var result = await _caseService.CreateCaseAsync(userId, title, description);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(title, result.Title);
        Assert.Equal(description, result.Description);
        Assert.Equal(WorkflowState.New, result.Status);
        Assert.Equal(userId, result.CreatedByUserId);
    }

    [Fact]
    public async Task CreateCaseAsync_WithTrimmedTitle_ShouldTrimWhitespace()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var title = "  Untrimmed Title  ";
        var description = "Description";

        // Act
        var result = await _caseService.CreateCaseAsync(userId, title, description);

        // Assert
        Assert.Equal("Untrimmed Title", result.Title);
    }

    #endregion

    #region GetAllCasesAsync Tests

    [Fact]
    public async Task GetAllCasesAsync_WithNoCases_ShouldReturnEmptyList()
    {
        // Act
        var result = await _caseService.GetAllCasesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllCasesAsync_WithMultipleCases_ShouldReturnAll()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        await _caseService.CreateCaseAsync(userId, "Case 1", "Description 1");
        await _caseService.CreateCaseAsync(userId, "Case 2", "Description 2");
        await _caseService.CreateCaseAsync(userId, "Case 3", "Description 3");

        // Act
        var result = await _caseService.GetAllCasesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllCasesAsync_ShouldReturnNewestFirst()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var case1 = await _caseService.CreateCaseAsync(userId, "Case 1", "First");
        await Task.Delay(10);
        var case2 = await _caseService.CreateCaseAsync(userId, "Case 2", "Second");
        await Task.Delay(10);
        var case3 = await _caseService.CreateCaseAsync(userId, "Case 3", "Third");

        // Act
        var result = await _caseService.GetAllCasesAsync();

        // Assert
        Assert.Equal(case3.Id, result[0].Id);
        Assert.Equal(case2.Id, result[1].Id);
        Assert.Equal(case1.Id, result[2].Id);
    }

    #endregion

    #region GetCaseByIdAsync Tests

    [Fact]
    public async Task GetCaseByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _caseService.GetCaseByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetCasesForUserAsync Tests

    [Fact]
    public async Task GetCasesForUserAsync_WithNoUserCases_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await _caseService.GetCasesForUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCasesForUserAsync_ShouldReturnCreatedCases()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        await _caseService.CreateCaseAsync(userId, "Case 1", "Created by user");
        await _caseService.CreateCaseAsync(userId, "Case 2", "Also created by user");

        // Act
        var result = await _caseService.GetCasesForUserAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetCasesForUserAsync_ShouldReturnAssignedCases()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var assignedUserId = Guid.NewGuid().ToString();
        var caseObj = await _caseService.CreateCaseAsync(creatorId, "Test Case", "Description");
        await _caseService.UpdateCaseStatusAndAssignmentAsync(caseObj.Id, WorkflowState.Assigned.ToString(), assignedUserId);

        // Act
        var result = await _caseService.GetCasesForUserAsync(assignedUserId);

        // Assert
        Assert.Single(result);
        Assert.Equal(caseObj.Id, result[0].Id);
    }

    #endregion

    #region GetCaseStatusHistoryAsync Tests

    [Fact]
    public async Task GetCaseStatusHistoryAsync_WithNoHistory_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var caseObj = await _caseService.CreateCaseAsync(userId, "Test Case", "Description");

        // Act
        var result = await _caseService.GetCaseStatusHistoryAsync(caseObj.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion
}
