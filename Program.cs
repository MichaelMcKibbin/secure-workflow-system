using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using secure_workflow_system.Components;
using secure_workflow_system.Components.Account;
using secure_workflow_system.Data;
using secure_workflow_system.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
    options.AddPolicy("RequireStaff", p => p.RequireRole("Admin", "Staff"));

    options.AddPolicy("CanCreateCase", p => p.RequireRole("User", "Admin"));
    options.AddPolicy("CanViewCases", p => p.RequireRole("User", "Staff", "Admin"));
    options.AddPolicy("CanManageCases", p => p.RequireRole("Staff", "Admin"));
    options.AddPolicy("CanManageUsers", p => p.RequireRole("Admin"));
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;

    // Optional: explicit password policy (good for report clarity)
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<ICaseService, CaseService>();
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("SecureWorkflowSystem");

var keyRingPath = builder.Configuration["DataProtection:KeyRingPath"]
    ?? Environment.GetEnvironmentVariable("DATA_PROTECTION_KEY_RING_PATH");

if (!string.IsNullOrWhiteSpace(keyRingPath))
{
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
}

var app = builder.Build();

await SeedIdentityAsync(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
   .AllowAnonymous();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.Run();

static async Task SeedIdentityAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    var migrationAttempts = 0;
    while (true)
    {
        try
        {
            await dbContext.Database.MigrateAsync();
            break;
        }
        catch when (migrationAttempts < 5)
        {
            migrationAttempts++;
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    string[] roles = ["Admin", "Staff", "User"];

    foreach (var role in roles)
    {
        if (await roleManager.RoleExistsAsync(role))
        {
            continue;
        }

        var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create role '{role}': {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }
    }

    var adminEmail = configuration["SEED_ADMIN_EMAIL"];
    var adminPassword = configuration["SEED_ADMIN_PASSWORD"];

    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        if (app.Environment.IsDevelopment())
        {
            adminEmail = "admin@local.test";
            adminPassword = "Admin123!";
        }
    }

    await SeedIdentityUserAsync(userManager, adminEmail, adminPassword, "Admin", "seeded admin user");
    await SeedIdentityUserAsync(userManager, configuration["SEED_USER_EMAIL"], configuration["SEED_USER_PASSWORD"], "User", "seeded user");
    await SeedIdentityUserAsync(userManager, configuration["SEED_STAFF_EMAIL"], configuration["SEED_STAFF_PASSWORD"], "Staff", "seeded staff user");

    await SeedSampleCasesAsync(dbContext, userManager, configuration);
}

static async Task SeedSampleCasesAsync(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration)
{
    if (await dbContext.Cases.AnyAsync())
    {
        return;
    }

    var creatorEmails = new[]
    {
        configuration["SEED_ADMIN_EMAIL"],
        configuration["SEED_STAFF_EMAIL"],
        configuration["SEED_USER_EMAIL"],
        "admin@local.test"
    };

    ApplicationUser? creatorUser = null;
    foreach (var email in creatorEmails.Where(email => !string.IsNullOrWhiteSpace(email)))
    {
        creatorUser = await userManager.FindByEmailAsync(email!);
        if (creatorUser is not null)
        {
            break;
        }
    }

    creatorUser ??= await userManager.Users.FirstOrDefaultAsync();
    if (creatorUser is null)
    {
        return;
    }

    ApplicationUser? assignedToUser = null;
    var assignedToEmail = configuration["SEED_USER_EMAIL"];
    if (!string.IsNullOrWhiteSpace(assignedToEmail))
    {
        assignedToUser = await userManager.FindByEmailAsync(assignedToEmail);
    }

    var utcNow = DateTime.UtcNow;

    Case CreateCase(string title, string description, WorkflowState status, int daysAgo, int hoursAfterCreated, bool assignToTestUser)
    {
        var createdAtUtc = utcNow.AddDays(-daysAgo);
        var isAssigned = assignToTestUser && assignedToUser is not null;

        return new Case
        {
            Title = title,
            Description = description,
            Status = isAssigned ? status : WorkflowState.New,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc.AddHours(hoursAfterCreated),
            CreatedByUserId = creatorUser.Id,
            AssignedToUserId = isAssigned ? assignedToUser.Id : null
        };
    }

    var sampleCases = new[]
    {
        CreateCase("Missing intake paperwork", "The intake packet is missing the signed consent form and needs follow-up with the submitter.", WorkflowState.Assigned, 14, 4, true),
        CreateCase("Late evidence upload", "Evidence files were uploaded after the review deadline and should be checked for completeness.", WorkflowState.InProgress, 12, 6, true),
        CreateCase("Address verification needed", "The case file contains an address mismatch between the intake form and the supporting documents.", WorkflowState.Resolved, 10, 3, true),
        CreateCase("Duplicate reference number", "A duplicate reference number was detected during entry and needs consolidation.", WorkflowState.Closed, 9, 2, true),
        CreateCase("Unclear supporting note", "The supporting note is readable but does not provide enough detail for a decision.", WorkflowState.New, 8, 1, false),
        CreateCase("Follow-up call required", "A follow-up call is needed to confirm the next steps with the requester.", WorkflowState.Assigned, 7, 5, true),
        CreateCase("Pending document review", "The uploaded documents are complete but still waiting for a second review.", WorkflowState.InProgress, 6, 4, true),
        CreateCase("Verification complete", "Identity and document verification were completed and the record is ready for closure.", WorkflowState.Resolved, 5, 2, true),
        CreateCase("Historical reference check", "The case is for reference only and does not require immediate action.", WorkflowState.New, 4, 1, false),
        CreateCase("Archived policy inquiry", "A policy question was answered and the case is ready to remain archived.", WorkflowState.New, 3, 1, false)
    };

    dbContext.Cases.AddRange(sampleCases);
    await dbContext.SaveChangesAsync();
}

static async Task SeedIdentityUserAsync(
    UserManager<ApplicationUser> userManager,
    string? email,
    string? password,
    string role,
    string userDescription)
{
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return;
    }

    var user = await userManager.FindByEmailAsync(email);

    if (user is null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsApproved = true
        };

        var userResult = await userManager.CreateAsync(user, password);
        if (!userResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create {userDescription} '{email}': {string.Join(", ", userResult.Errors.Select(e => e.Description))}");
        }
    }
    else if (!user.EmailConfirmed || !user.IsApproved)
    {
        user.EmailConfirmed = true;
        user.IsApproved = true;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to update {userDescription} '{email}': {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
        }
    }

    if (!await userManager.IsInRoleAsync(user, role))
    {
        var addToRoleResult = await userManager.AddToRoleAsync(user, role);
        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to add {userDescription} '{email}' to role '{role}': {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
        }
    }
}
