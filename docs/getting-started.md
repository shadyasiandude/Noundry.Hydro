# Getting Started with Noundry.Hydro 🚀

This guide will walk you through creating your first Noundry.Hydro application from scratch.

## Prerequisites

- [.NET 6, 8, or 9 SDK](https://dotnet.microsoft.com/download)
- A code editor (Visual Studio, VS Code, Rider, etc.)
- Basic knowledge of ASP.NET Core and C#

## Step 1: Create a New Project

```bash
# Create a new Razor Pages project
dotnet new webapp -o MyNoundryApp
cd MyNoundryApp

# Or create an MVC project
dotnet new mvc -o MyNoundryApp
cd MyNoundryApp
```

## Step 2: Install Noundry.Hydro

```bash
dotnet add package Noundry.Hydro --version 1.0.0-preview1
```

## Step 3: Configure Services

Update your `Program.cs`:

```csharp
using Noundry.Hydro.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add Noundry.Hydro with full ecosystem
builder.Services.AddNoundryHydro(options =>
{
    // Core settings
    options.EnableNoundryUI = true;
    options.EnableNoundryTagHelpers = true;
    options.IncludeTailwindCSS = true;
    options.IncludeAlpineJS = true;
    options.EnableTuxedoIntegration = true;
    
    // Development settings
    options.DevelopmentMode = builder.Environment.IsDevelopment();
    
    // Styling configuration
    options.Styling.PrimaryColor = "#3B82F6";
    options.Styling.SupportDarkMode = true;
});

// Add other services as needed
builder.Services.AddRazorPages(); // or AddControllersWithViews() for MVC

var app = builder.Build();

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Use Noundry.Hydro middleware
app.UseNoundryHydro(app.Environment);

// Configure endpoints
app.MapRazorPages(); // or app.MapControllerRoute(...) for MVC

app.Run();
```

## Step 4: Update View Imports

Add to `Views/_ViewImports.cshtml` (MVC) or `Pages/_ViewImports.cshtml` (Razor Pages):

```razor
@using Noundry.Hydro.Components
@addTagHelper *, YourProjectName
@addTagHelper *, Noundry.Hydro
@addTagHelper *, Noundry.TagHelpers
@addTagHelper *, Noundry.UI
```

## Step 5: Update Layout

Update your `Views/Shared/_Layout.cshtml` or `Pages/Shared/_Layout.cshtml`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - My Noundry App</title>
    
    <!-- Noundry.Hydro configuration -->
    <meta name="hydro-config" />
    
    <!-- Styles -->
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
    <link rel="stylesheet" href="~/css/noundry-hydro.css" asp-append-version="true" />
    
    <!-- Optional: Include Tailwind CSS -->
    <script src="https://cdn.tailwindcss.com"></script>
</head>
<body>
    <header>
        <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3">
            <div class="container">
                <a class="navbar-brand" asp-area="" asp-page="/Index">My Noundry App</a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="navbar-collapse collapse d-sm-inline-flex justify-content-between">
                    <ul class="navbar-nav flex-grow-1">
                        <li class="nav-item">
                            <a class="nav-link text-dark" asp-area="" asp-page="/Index">Home</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark" asp-area="" asp-page="/Privacy">Privacy</a>
                        </li>
                    </ul>
                </div>
            </div>
        </nav>
    </header>
    
    <div class="container">
        <main role="main" class="pb-3">
            @RenderBody()
        </main>
    </div>

    <footer class="border-top footer text-muted">
        <div class="container">
            &copy; 2023 - My Noundry App
        </div>
    </footer>

    <!-- Scripts -->
    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/site.js" asp-append-version="true"></script>
    
    <!-- Noundry.Hydro Scripts -->
    <script defer src="~/hydro/hydro.js" asp-append-version="true"></script>
    <script defer src="~/hydro/alpine.js" asp-append-version="true"></script>
    
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

## Step 6: Create Your First Component

Create a directory structure for your components. For Razor Pages:
```
Pages/
  Components/
    Counter/
      Counter.cs
      Counter.cshtml
```

**Pages/Components/Counter/Counter.cs**:
```csharp
using Noundry.Hydro.Components;

namespace MyNoundryApp.Pages.Components.Counter;

public class Counter : NoundryHydroComponent
{
    public int Count { get; set; }
    public string Message { get; set; } = "";

    public void Increment()
    {
        Count++;
        ShowToast($"Count increased to {Count}!", "success");
        
        if (Count == 10)
        {
            Message = "🎉 You've reached 10 clicks!";
            DispatchNoundryEvent(new CounterMilestone { Count = Count });
        }
        else if (Count > 10)
        {
            Message = $"🚀 You're on fire! {Count} clicks!";
        }
    }

    public void Decrement()
    {
        if (Count > 0)
        {
            Count--;
            ShowToast($"Count decreased to {Count}", "info");
            
            if (Count == 0)
            {
                Message = "Back to zero! 🔄";
            }
        }
        else
        {
            ShowToast("Can't go below zero!", "warning");
        }
    }

    public void Reset()
    {
        Count = 0;
        Message = "";
        ShowToast("Counter reset!", "info");
        DispatchNoundryEvent(new CounterReset());
    }
}

public record CounterMilestone(int Count);
public record CounterReset();
```

**Pages/Components/Counter/Counter.cshtml**:
```razor
@model Counter

<div class="noundry-card max-w-md mx-auto">
    <div class="noundry-card-header text-center">
        <h3 class="noundry-card-title">Interactive Counter</h3>
        <p class="text-gray-600 text-sm">Click the buttons to interact!</p>
    </div>
    
    <div class="noundry-card-body">
        <div class="text-center mb-6">
            <div class="text-6xl font-bold text-blue-600 mb-2">@Model.Count</div>
            
            @if (!string.IsNullOrEmpty(Model.Message))
            {
                <div class="text-lg font-semibold text-green-600 bg-green-50 rounded-lg p-2">
                    @Model.Message
                </div>
            }
        </div>
        
        <div class="flex gap-2 justify-center">
            <button on:click="@(() => Model.Decrement())" 
                    class="noundry-btn noundry-btn-secondary px-4 py-2">
                ➖ Decrease
            </button>
            
            <button on:click="@(() => Model.Increment())" 
                    class="noundry-btn noundry-btn-primary px-4 py-2">
                ➕ Increase
            </button>
            
            <button on:click="@(() => Model.Reset())" 
                    class="noundry-btn noundry-btn-danger px-4 py-2">
                🔄 Reset
            </button>
        </div>
        
        <div class="mt-4 text-center text-sm text-gray-500">
            <p>This component updates in real-time without page refreshes!</p>
        </div>
    </div>
</div>
```

## Step 7: Use the Component

Update your `Pages/Index.cshtml` or `Views/Home/Index.cshtml`:

```razor
@page
@model IndexModel
@{
    ViewData["Title"] = "Welcome to Noundry.Hydro";
}

<div class="text-center mb-8">
    <h1 class="text-4xl font-bold text-gray-900 mb-2">Welcome to Noundry.Hydro!</h1>
    <p class="text-xl text-gray-600">
        Build reactive ASP.NET Core apps with zero JavaScript complexity
    </p>
</div>

<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
    <!-- Feature cards using Noundry components -->
    <div class="noundry-card">
        <div class="noundry-card-header">
            <h3 class="noundry-card-title">🚀 Reactive Components</h3>
        </div>
        <div class="noundry-card-body">
            <p>Build interactive components that update in real-time without writing JavaScript.</p>
        </div>
    </div>
    
    <div class="noundry-card">
        <div class="noundry-card-header">
            <h3 class="noundry-card-title">🎨 Beautiful UI</h3>
        </div>
        <div class="noundry-card-body">
            <p>44+ pre-built components with Tailwind CSS styling and accessibility built-in.</p>
        </div>
    </div>
    
    <div class="noundry-card">
        <div class="noundry-card-header">
            <h3 class="noundry-card-title">🗄️ Powerful ORM</h3>
        </div>
        <div class="noundry-card-body">
            <p>High-performance Tuxedo ORM with LINQ expressions and multi-database support.</p>
        </div>
    </div>
</div>

<!-- Demo component -->
<div class="mb-8">
    <h2 class="text-2xl font-bold text-center mb-4">Try the Interactive Counter</h2>
    <counter />
</div>

<!-- Alert examples -->
<div class="space-y-4">
    <h2 class="text-2xl font-bold">Component Examples</h2>
    
    @{
        // Using the Alert helper method
        Html.Raw(Alert("This is a success message!", "success", true));
        Html.Raw(Alert("This is an info message!", "info", true));
        Html.Raw(Alert("This is a warning message!", "warning", true));
        Html.Raw(Alert("This is an error message!", "error", true));
    }
</div>
```

## Step 8: Run Your Application

```bash
dotnet run
```

Navigate to `https://localhost:7000` (or your configured port) and you should see your Noundry.Hydro application with the interactive counter component!

## Next Steps

### Add More Components

Create additional components for common scenarios:

1. **Form Component** - For user input with validation
2. **Data Table Component** - For displaying and manipulating data
3. **Modal Component** - For dialogs and overlays
4. **Navigation Component** - For dynamic menus

### Add Database Integration

```csharp
// In Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register Tuxedo services
builder.Services.AddTuxedo();
```

### Add Authentication

```csharp
// In Program.cs
builder.Services.AddDefaultIdentity<IdentityUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Use authentication
app.UseAuthentication();
app.UseAuthorization();
```

### Explore Advanced Features

1. **Real-time Updates** - Use polling and events for live data
2. **Form Validation** - Build complex forms with client-side validation
3. **File Uploads** - Handle file uploads reactively
4. **Progressive Web App** - Add PWA capabilities
5. **Testing** - Unit test your components

## Troubleshooting

### Common Issues

1. **Component not rendering**: Check that TagHelpers are properly registered in `_ViewImports.cshtml`

2. **JavaScript errors**: Ensure scripts are loaded in the correct order and paths are correct

3. **Styles not applying**: Verify CSS files are properly referenced and Tailwind classes are available

4. **Component events not working**: Check that Alpine.js is loaded and component markup is correct

### Getting Help

- Check the [comprehensive documentation](../README.md)
- Look at the [demo project](../examples/Noundry.Hydro.Demo/) for complete examples
- Join our [Discord community](https://discord.gg/noundry) for real-time help
- Open an issue on [GitHub](https://github.com/plsft/Noundry.Hydro/issues)

## What's Next?

Now that you have a working Noundry.Hydro application, explore these guides:

- [🎯 Component Development Guide](components.md)
- [🎨 Styling and Theming Guide](styling.md)
- [🗄️ Data Access with Tuxedo](data-access.md)
- [🔐 Authentication and Security](security.md)
- [📱 Progressive Web Apps](pwa.md)
- [🧪 Testing Your Components](testing.md)
- [🚀 Deployment Guide](deployment.md)

Happy coding with Noundry.Hydro! 🎉