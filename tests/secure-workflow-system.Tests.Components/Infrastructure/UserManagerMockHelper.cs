using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using secure_workflow_system.Data;

namespace secure_workflow_system.Tests.Components.Infrastructure;

public static class UserManagerMockHelper
{
    /// <summary>
    /// Creates a Mock&lt;UserManager&lt;ApplicationUser&gt;&gt; with all required
    /// constructor dependencies satisfied. Configure additional behaviour on
    /// the returned mock before registering it with the test context.
    /// </summary>
    public static Mock<UserManager<ApplicationUser>> CreateMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        var passwordHasher = new Mock<IPasswordHasher<ApplicationUser>>();
        var userValidators = new List<IUserValidator<ApplicationUser>>();
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new Mock<IdentityErrorDescriber>();
        var services = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<UserManager<ApplicationUser>>>();

        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            options.Object,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            keyNormalizer.Object,
            errors.Object,
            services.Object,
            logger.Object);
    }

    /// <summary>
    /// Sets up GetUsersInRoleAsync to return the given users for the given role.
    /// </summary>
    public static Mock<UserManager<ApplicationUser>> WithUsersInRole(
        this Mock<UserManager<ApplicationUser>> mock,
        string role,
        IList<ApplicationUser> users)
    {
        mock.Setup(m => m.GetUsersInRoleAsync(role))
            .ReturnsAsync(users);
        return mock;
    }

    /// <summary>
    /// Sets up GetUsersInRoleAsync to return empty lists for all three app roles.
    /// </summary>
    public static Mock<UserManager<ApplicationUser>> WithNoUsersInAnyRole(
        this Mock<UserManager<ApplicationUser>> mock)
    {
        mock.WithUsersInRole("User", new List<ApplicationUser>());
        mock.WithUsersInRole("Staff", new List<ApplicationUser>());
        mock.WithUsersInRole("Admin", new List<ApplicationUser>());
        return mock;
    }
}
