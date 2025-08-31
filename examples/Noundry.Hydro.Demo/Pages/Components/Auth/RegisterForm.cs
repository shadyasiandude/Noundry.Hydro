using Microsoft.AspNetCore.Identity;
using Noundry.Hydro.Components;
using Noundry.Hydro.Demo.Models;
using System.ComponentModel.DataAnnotations;

namespace Noundry.Hydro.Demo.Pages.Components.Auth;

public class RegisterForm : NoundryHydroComponent
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<RegisterForm> _logger;

    public RegisterForm(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterForm> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [Required, MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = "";

    [Required, EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = "";

    [Display(Name = "I agree to the Terms and Conditions")]
    public bool AgreeToTerms { get; set; }

    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public override void Mount()
    {
        ReturnUrl = Request.Query["returnUrl"].FirstOrDefault();
    }

    public async Task Register()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (!AgreeToTerms)
        {
            ModelState.AddModelError(nameof(AgreeToTerms), "You must agree to the Terms and Conditions");
        }

        if (!Validate())
        {
            ShowToast("Please correct the errors below", "error");
            return;
        }

        IsLoading = true;

        try
        {
            var user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                FirstName = FirstName,
                LastName = LastName,
                EmailConfirmed = true, // Auto-confirm for demo
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} created a new account with password", Email);

                // Assign default role
                await _userManager.AddToRoleAsync(user, "Customer");

                // Sign in the user immediately (for demo purposes)
                await _signInManager.SignInAsync(user, isPersistent: false);

                SuccessMessage = "Account created successfully! Welcome to Noundry.Hydro Demo.";
                ShowToast("Registration successful! Welcome!", "success");

                // Dispatch registration event
                DispatchNoundryEvent(new UserRegistered 
                { 
                    Email = Email, 
                    FullName = $"{FirstName} {LastName}",
                    Timestamp = DateTime.UtcNow 
                }, Scope.Global);

                // Redirect after successful registration
                Client.ExecuteScript($"setTimeout(() => window.location.href = '{ReturnUrl ?? "/"}', 2000);");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                ErrorMessage = $"Registration failed: {errors}";
                ShowToast("Registration failed", "error");

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", Email);
            ErrorMessage = "An error occurred during registration. Please try again.";
            ShowToast("Registration error occurred", "error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ClearForm()
    {
        FirstName = "";
        LastName = "";
        Email = "";
        Password = "";
        ConfirmPassword = "";
        AgreeToTerms = false;
        ErrorMessage = null;
        SuccessMessage = null;
        ShowToast("Form cleared", "info");
    }

    public async Task CheckEmailAvailability()
    {
        if (string.IsNullOrWhiteSpace(Email) || !new EmailAddressAttribute().IsValid(Email))
            return;

        var existingUser = await _userManager.FindByEmailAsync(Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(Email), "This email address is already registered.");
            ShowToast("Email already exists", "warning");
        }
        else
        {
            ShowToast("Email available", "success");
        }
    }

    public void ValidatePasswordStrength()
    {
        if (string.IsNullOrWhiteSpace(Password))
            return;

        var strengthScore = CalculatePasswordStrength(Password);
        var strengthText = strengthScore switch
        {
            >= 4 => "Very Strong",
            3 => "Strong",
            2 => "Medium",
            1 => "Weak",
            _ => "Very Weak"
        };

        var strengthColor = strengthScore switch
        {
            >= 4 => "success",
            3 => "success",
            2 => "warning",
            1 => "warning",
            _ => "error"
        };

        ShowToast($"Password strength: {strengthText}", strengthColor);
    }

    private static int CalculatePasswordStrength(string password)
    {
        var score = 0;
        if (password.Length >= 8) score++;
        if (password.Any(char.IsUpper)) score++;
        if (password.Any(char.IsLower)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c))) score++;
        return score;
    }
}

public record UserRegistered(string Email, string FullName, DateTime Timestamp);