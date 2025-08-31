using Microsoft.AspNetCore.Identity;
using Noundry.Hydro.Configuration;
using Noundry.Hydro.Demo.Data;
using Noundry.Hydro.Demo.Models;
using Noundry.Hydro.Demo.Services;
using Tuxedo;
using Bowtie;
using Guardian;

var builder = WebApplication.CreateBuilder(args);

// Configure Tuxedo ORM as the primary data access layer (replaces Entity Framework)
builder.Services.AddTuxedo(options =>
{
    options.ConnectionString = "Data Source=noundry_hydro_demo.db"; // SQLite for demo
    options.DatabaseProvider = DatabaseProvider.SQLite;
    options.EnableRetryPolicies = true;
    options.EnableDiagnostics = builder.Environment.IsDevelopment();
    options.CommandTimeout = TimeSpan.FromSeconds(30);
});

// Configure Bowtie migration system for database schema management
builder.Services.AddBowtie(options =>
{
    options.MigrationsAssembly = typeof(TuxedoDataContext).Assembly;
    options.AutoMigrate = builder.Environment.IsDevelopment();
    options.CreateDatabaseIfNotExists = true;
});

// Register Tuxedo data context
builder.Services.AddScoped<TuxedoDataContext>();
builder.Services.AddScoped<ITuxedoContext>(provider => provider.GetRequiredService<TuxedoDataContext>());

// Configure Identity with Tuxedo-based stores (not Entity Framework)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings for demo (relaxed for easier testing)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Sign in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddTuxedoStores<TuxedoDataContext>() // Use Tuxedo stores instead of EF
.AddDefaultTokenProviders();

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Manager", "Admin"));
    options.AddPolicy("CustomerAccess", policy => policy.RequireAuthenticatedUser());
});

// Add Noundry.Hydro ecosystem with full configuration
builder.Services.AddNoundryHydro(options =>
{
    // Enable all ecosystem features
    options.EnableNoundryUI = true;
    options.EnableNoundryTagHelpers = true;
    options.IncludeTailwindCSS = true;
    options.IncludeAlpineJS = true;
    options.EnableTuxedoIntegration = true;
    
    // Development settings
    options.DevelopmentMode = builder.Environment.IsDevelopment();
    
    // Custom styling for demo
    options.Styling.PrimaryColor = "#6366F1"; // Indigo
    options.Styling.SecondaryColor = "#8B5CF6"; // Purple
    options.Styling.SuccessColor = "#10B981"; // Emerald
    options.Styling.WarningColor = "#F59E0B"; // Amber
    options.Styling.ErrorColor = "#EF4444"; // Red
    options.Styling.SupportDarkMode = true;
});

// Add demo-specific services
builder.Services.AddScoped<IDemoDataService, DemoDataService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Add Razor Pages
builder.Services.AddRazorPages(options =>
{
    // Require authentication for admin pages
    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
    options.Conventions.AuthorizePage("/Admin/Dashboard", "AdminOnly");
    options.Conventions.AuthorizePage("/Customers/Index", "CustomerAccess");
    options.Conventions.AuthorizePage("/Orders/Index", "CustomerAccess");
    options.Conventions.AuthorizePage("/Products/Index", "CustomerAccess");
    options.Conventions.AuthorizePage("/Invoices/Index", "CustomerAccess");
});

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Use Noundry.Hydro middleware
app.UseNoundryHydro(app.Environment);

// Configure endpoints
app.MapRazorPages();

// Initialize database with Tuxedo and sample data
await InitializeTuxedoDatabase(app);

app.Run();

static async Task InitializeTuxedoDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var tuxedoContext = services.GetRequiredService<TuxedoDataContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var dataService = services.GetRequiredService<IDemoDataService>();

        // Initialize database schema using Tuxedo/Bowtie
        await tuxedoContext.InitializeDatabaseAsync();

        // Initialize roles and users
        await SeedRolesAndUsers(userManager, roleManager);

        // Generate sample data using Tuxedo
        await dataService.GenerateSampleDataAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing Tuxedo database.");
    }
}

static async Task SeedRolesAndUsers(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
{
    // Create roles
    string[] roleNames = { "Admin", "Manager", "Customer" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Create admin user
    var adminEmail = "admin@noundry.demo";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    // Create manager user
    var managerEmail = "manager@noundry.demo";
    var managerUser = await userManager.FindByEmailAsync(managerEmail);
    if (managerUser == null)
    {
        managerUser = new ApplicationUser
        {
            UserName = managerEmail,
            Email = managerEmail,
            FirstName = "Manager",
            LastName = "User",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(managerUser, "Manager123!");
        if (result.Succeeded)
        {
            await userManager.AddToRolesAsync(managerUser, new[] { "Manager", "Customer" });
        }
    }

    // Create demo customer user
    var customerEmail = "customer@noundry.demo";
    var customerUser = await userManager.FindByEmailAsync(customerEmail);
    if (customerUser == null)
    {
        customerUser = new ApplicationUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            FirstName = "Demo",
            LastName = "Customer",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(customerUser, "Customer123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(customerUser, "Customer");
        }
    }
}