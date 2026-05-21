using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using secure_workflow_system.Components.Pages;
using secure_workflow_system.Data;
using secure_workflow_system.Services;
using Xunit;

namespace secure_workflow_system.Tests.Components.Pages;

public class MyCasesTests : TestContext
{
    [Fact]
    public void MyCases_WithRegularUser_ShouldRenderUserCasesAndStatuses()
    {
        // Arrange
        var userId = "user-123";
        var testCases = new List<Case>
        {
            new()
            {
                Id = 1,
                Title = "Assigned case",
                Description = "A case assigned to the current user.",
                Status = WorkflowState.Assigned,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                CreatedByUserId = "creator-1",
                AssignedToUserId = userId
            },
            new()
            {
                Id = 2,
                Title = "In progress case",
                Description = "A case in progress.",
                Status = WorkflowState.InProgress,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                CreatedByUserId = "creator-2",
                AssignedToUserId = userId
            }
        };

        var caseService = new Mock<ICaseService>();
        caseService
            .Setup(x => x.GetCasesForUserAsync(userId))
            .ReturnsAsync(testCases);

        Services.AddSingleton(caseService.Object);
        Services.AddSingleton<AuthenticationStateProvider>(
            CreateAuthenticationStateProvider(userId, "user@local.test", "User"));

        // Act
        var cut = Render<MyCases>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("My Cases", cut.Markup);
            Assert.Contains("View your assigned cases", cut.Markup);
            Assert.Equal(2, cut.FindAll("tr.case-list-row").Count);
            Assert.Contains("Assigned case", cut.Markup);
            Assert.Contains("In progress case", cut.Markup);
            Assert.Contains("Assigned", cut.Markup);
            Assert.Contains("In Progress", cut.Markup);
        });

        caseService.Verify(x => x.GetCasesForUserAsync(userId), Times.Once);
        caseService.Verify(x => x.GetAllCasesAsync(), Times.Never);
    }

    [Fact]
    public void MyCases_WithStaffUser_ShouldRenderAllCasesHeading()
    {
        // Arrange
        var userId = "staff-123";
        var testCases = new List<Case>
        {
            new()
            {
                Id = 10,
                Title = "Resolved case",
                Description = "A resolved case.",
                Status = WorkflowState.Resolved,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                CreatedByUserId = "creator-10"
            }
        };

        var caseService = new Mock<ICaseService>();
        caseService
            .Setup(x => x.GetAllCasesAsync())
            .ReturnsAsync(testCases);

        Services.AddSingleton(caseService.Object);
        Services.AddSingleton<AuthenticationStateProvider>(
            CreateAuthenticationStateProvider(userId, "staff@local.test", "Staff"));

        // Act
        var cut = Render<MyCases>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("All Cases", cut.Markup);
            Assert.Contains("View all cases in the system", cut.Markup);
            Assert.Contains("Resolved case", cut.Markup);
            Assert.Contains("Resolved", cut.Markup);
        });

        caseService.Verify(x => x.GetAllCasesAsync(), Times.Once);
        caseService.Verify(x => x.GetCasesForUserAsync(It.IsAny<string>()), Times.Never);
    }

    private static AuthenticationStateProvider CreateAuthenticationStateProvider(
        string userId,
        string userName,
        params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new TestAuthenticationStateProvider(principal);
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _authenticationState;

        public TestAuthenticationStateProvider(ClaimsPrincipal user)
        {
            _authenticationState = new AuthenticationState(user);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(_authenticationState);
    }
}

