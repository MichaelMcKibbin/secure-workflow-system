using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
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

    await dbContext.Database.MigrateAsync();

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

    var adminEmail = configuration["SeedAdmin:Email"];
    var adminPassword = configuration["SeedAdmin:Password"];

    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        if (app.Environment.IsDevelopment())
        {
            adminEmail = "admin@local.test";
            adminPassword = "Admin123!";
        }
        else
        {
            return;
        }
    }

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser is null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var userResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!userResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create seeded admin user '{adminEmail}': {string.Join(", ", userResult.Errors.Select(e => e.Description))}");
        }
    }
    else if (!adminUser.EmailConfirmed)
    {
        adminUser.EmailConfirmed = true;
        var updateResult = await userManager.UpdateAsync(adminUser);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to confirm seeded admin user '{adminEmail}': {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        var addToRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to add seeded admin user '{adminEmail}' to role 'Admin': {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
        }
    }
}
