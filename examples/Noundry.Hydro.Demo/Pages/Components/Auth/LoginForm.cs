using Microsoft.AspNetCore.Identity;
using Noundry.Hydro.Components;
using Noundry.Hydro.Demo.Models;
using System.ComponentModel.DataAnnotations;

namespace Noundry.Hydro.Demo.Pages.Components.Auth;

public class LoginForm : NoundryHydroComponent
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginForm> _logger;

    public LoginForm(SignInManager<ApplicationUser> signInManager, ILogger<LoginForm> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [Required, EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public override void Mount()
    {
        ReturnUrl = Request.Query["returnUrl"].FirstOrDefault();
        
        // Pre-fill demo credentials for easier testing
        if (Options.DevelopmentMode)
        {
            Email = "admin@noundry.demo";
            Password = "Admin123!";
        }
    }

    public async Task SignIn()
    {
        ErrorMessage = null;
        
        if (!Validate())
        {
            ShowToast("Please correct the errors below", "error");
            return;
        }

        IsLoading = true;
        
        try
        {
            var result = await _signInManager.PasswordSignInAsync(
                Email, Password, RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully", Email);
                ShowToast("Welcome back! Login successful.", "success");
                
                // Dispatch login event for other components
                DispatchNoundryEvent(new UserLoggedIn 
                { 
                    Email = Email, 
                    Timestamp = DateTime.UtcNow 
                }, Scope.Global);

                // Redirect after successful login
                Client.ExecuteScript($"setTimeout(() => window.location.href = '{ReturnUrl ?? "/"}', 1000);");
            }
            else if (result.RequiresTwoFactor)
            {
                ShowToast("Two-factor authentication required", "info");
                Location($"/Identity/Account/LoginWith2fa?returnUrl={ReturnUrl}");
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out", Email);
                ErrorMessage = "This account has been locked out, please try again later.";
                ShowToast("Account locked out", "error");
            }
            else
            {
                _logger.LogWarning("Invalid login attempt for {Email}", Email);
                ErrorMessage = "Invalid email or password. Please try again.";
                ShowToast("Invalid credentials", "error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt for {Email}", Email);
            ErrorMessage = "An error occurred during login. Please try again.";
            ShowToast("Login error occurred", "error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ClearForm()
    {
        Email = "";
        Password = "";
        RememberMe = false;
        ErrorMessage = null;
        ShowToast("Form cleared", "info");
    }

    public void FillDemoCredentials(string userType)
    {
        (Email, Password) = userType.ToLowerInvariant() switch
        {
            "admin" => ("admin@noundry.demo", "Admin123!"),
            "manager" => ("manager@noundry.demo", "Manager123!"),
            "customer" => ("customer@noundry.demo", "Customer123!"),
            _ => ("", "")
        };

        ShowToast($"Demo {userType} credentials filled", "info");
    }
}

public record UserLoggedIn(string Email, DateTime Timestamp);