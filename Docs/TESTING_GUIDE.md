# Comprehensive Testing Guide

## Overview

The secure-workflow-system project includes comprehensive testing across three levels:
- **Unit Tests** - Fast, isolated tests for business logic and services
- **Integration Tests** - Real database tests with PostgreSQL via TestContainers
- **Component Tests** - Blazor UI component testing with BUnit

## Test Projects Structure

### 1. secure-workflow-system.Tests.Unit (new)
Unit tests for services and models using xUnit framework with in-memory database.

**Location:** `secure-workflow-system.Tests.Unit/`

**Key Components:**
- `ServiceTests/CaseServiceTests.cs` - CRUD operations (20+ tests)
- `ServiceTests/CaseServiceErrorHandlingTests.cs` - Error scenarios and edge cases
- `ServiceTests/CaseStatusHistoryTrackingTests.cs` - Status transition tracking
- `ModelTests/CaseModelTests.cs` - Model validation and business rules
- `Builders/CaseBuilder.cs` - Fluent builder for test data
- `Builders/CaseStatusHistoryBuilder.cs` - Test data for history
- `Infrastructure/TestDbContextFactory.cs` - In-memory database setup

**Running Unit Tests:** (temporarily removed)
```bash
dotnet test secure-workflow-system.Tests.Unit/
```

### 2. secure-workflow-system.Tests.Integration (temporarily removed)
Integration tests with real PostgreSQL database using TestContainers.

**Location:** `secure-workflow-system.Tests.Integration/`

**Key Components:**
- `CaseServiceIntegrationTests.cs` - Database persistence, relationships, performance
- `Infrastructure/PostgreSqlFixture.cs` - TestContainers PostgreSQL setup

**Running Integration Tests:** (temporarily removed)
```bash
dotnet test secure-workflow-system.Tests.Integration/
```

**Note:** Requires Docker to be running for TestContainers.

### 3. secure-workflow-system.Tests.Components (temporarily removed)
Blazor component testing with BUnit for UI testing.

**Location:** `secure-workflow-system.Tests.Components/`

**Key Components:**
- `Pages/MyCasesComponentTests.cs` - Layout toggle, data display, authorization
- `Pages/CaseDetailsComponentTests.cs` - Case details rendering, user display
- `Infrastructure/AuthenticationMockHelper.cs` - Mock authentication for testing

**Running Component Tests:** (temporarily removed)
```bash
dotnet test secure-workflow-system.Tests.Components/ 
```

## Test Coverage by Area

### Case Service (CaseService.cs)

| Operation | Unit Tests | Integration Tests | Status |
|-----------|-----------|-----------------|--------|
| Create Case | ✅ 3 tests | ✅ 1 test | Complete |
| Get All Cases | ✅ 3 tests | ✅ 1 test | Complete |
| Get Case by ID | ✅ 3 tests | ✅ 1 test | Complete |
| Get Cases for User | ✅ 3 tests | ✅ 1 test | Complete |
| Update Status | ✅ 3 tests | ✅ 1 test | Complete |
| Get Status History | ✅ 2 tests | ✅ 1 test | Complete |

### Workflow Validation

| Scenario | Tests | Status |
|----------|-------|--------|
| Valid Transitions | ✅ 5 tests (theory) | Complete |
| Invalid Transitions | ✅ 6 tests (theory) | Complete |
| Multi-step Workflows | ✅ 1 test | Complete |

### Components

| Component | Tests | Coverage |
|-----------|-------|----------|
| MyCases | ✅ 10 tests | Layout toggle, data display, authorization, empty states |
| CaseDetails | ✅ 12 tests | User display, status history, status badges |

## Running All Tests

### Run all tests
```bash
dotnet test
```

### Run with code coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run specific test class
```bash
dotnet test --filter="ClassName=CaseServiceTests"
```

### Run specific test method
```bash
dotnet test --filter="FullyQualifiedName~CaseServiceTests.CreateCaseAsync_WithValidData_ShouldCreateCase"
```

### Run tests in parallel
```bash
dotnet test -p:ParallelizeTestCollections=true
```

## CI/CD Integration

Tests run automatically on:
- **Pull Requests** to `main` or `develop` branches
- **Pushes** to `develop` branch

**Workflow File:** `.github/workflows/ci.yml`

**Test Stages:**
1. ✅ Build verification
2. ✅ Unit Tests
3. ✅ Component Tests
4. ✅ Integration Tests (requires Docker)
5. ✅ Code coverage collection

## Test Patterns and Best Practices

### Test Naming Convention
Tests follow the pattern: `MethodName_Scenario_ExpectedOutcome`

Examples:
- `CreateCaseAsync_WithValidData_ShouldCreateCase`
- `UpdateCaseStatusAndAssignmentAsync_WithInvalidTransition_ShouldReturnFalse`

### Arrange-Act-Assert (AAA) Pattern
All tests follow the AAA pattern:
```csharp
[Fact]
public async Task Example_Test()
{
	// Arrange - Setup test data
	var userId = Guid.NewGuid().ToString();
	var testCase = await _caseService.CreateCaseAsync(userId, "Title", "Desc");

	// Act - Perform the operation
	var result = await _caseService.GetCaseByIdAsync(testCase.Id);

	// Assert - Verify expectations
	Assert.NotNull(result);
	Assert.Equal("Title", result.Title);
}
```

### Using Builders for Test Data
```csharp
var testCase = new CaseBuilder()
	.WithTitle("Important Case")
	.WithStatus(WorkflowState.Assigned)
	.WithAssignedTo(assignedUserId, "john.doe")
	.Build();
```

### Mocking Services
```csharp
var mockCaseService = new Mock<ICaseService>();
mockCaseService
	.Setup(x => x.GetCasesForUserAsync(It.IsAny<string>()))
	.ReturnsAsync(testCases);

_testContext.Services.AddScoped(_ => mockCaseService.Object);
```

### Testing Authentication
```csharp
_testContext.AddAuthenticatedUser("user-id", "username", "Staff", "Admin");
```

## Test Data Management

### In-Memory Database (Unit Tests)
- Each test gets its own isolated database instance
- Data is not persisted between tests
- Fast test execution (~milliseconds per test)

### TestContainers PostgreSQL (Integration Tests)
- Uses real PostgreSQL database
- Database is created fresh for each test run
- Requires Docker to be running
- Takes longer but tests real database behavior

## Troubleshooting

### Tests Fail with "Database context not initialized"
**Solution:** Ensure `PostgreSqlFixture` is properly inherited and initialized
```csharp
[Collection("PostgreSQL Collection")]
public class MyIntegrationTest
{
	private readonly PostgreSqlFixture _fixture;
	public MyIntegrationTest(PostgreSqlFixture fixture) => _fixture = fixture;
}
```

### Component Tests Fail with "No AuthenticationStateProvider"
**Solution:** Add authentication mock before rendering component
```csharp
_testContext.AddAuthenticatedUser("user-id", "username");
var component = _testContext.RenderComponent<MyCases>();
```

### TestContainers Fail with Docker Connection Error
**Solution:** Ensure Docker is running
```bash
docker --version  # Verify Docker is installed
docker info       # Verify Docker daemon is running
```

## Performance Considerations

- **Unit Tests:** ~30ms each (total ~2 seconds for 50 tests)
- **Component Tests:** ~50ms each (total ~1 second for 20 tests)
- **Integration Tests:** ~500ms each (total ~15 seconds for 30 tests)

**Total Test Suite:** ~18 seconds

## Code Coverage Goals

- **Services (CaseService):** Target 90%+ coverage
- **Models:** Target 95%+ coverage
- **Components:** Target 80%+ coverage

## Adding New Tests

### When Adding a New Feature
1. Write unit tests first (TDD approach)
2. Write integration tests for database interactions
3. Write component tests if UI changes
4. Update this documentation

### Test File Location
- Unit tests: `secure-workflow-system.Tests.Unit/ServiceTests/` or `ModelTests/`
- Integration tests: `secure-workflow-system.Tests.Integration/`
- Component tests: `secure-workflow-system.Tests.Components/Pages/`

## Continuous Integration Status
Build status and test results are visible in GitHub Actions for each commit and PR.

See: https://github.com/MichaelMcKibbin/secure-workflow-system/actions

