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
