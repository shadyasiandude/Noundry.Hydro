using Noundry.Hydro.Components;
using Noundry.Hydro.Demo.Models;
using Noundry.Hydro.Demo.Services;
using System.ComponentModel.DataAnnotations;
using Guardian;
using Assertive;

namespace Noundry.Hydro.Demo.Pages.Components.Customers;

public class CustomerForm : NoundryHydroComponent
{
    private readonly ICustomerService _customerService;

    public CustomerForm(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [Required, MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, MaxLength(200)]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = "";

    [Phone, MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(20)]
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; } = "USA";

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Display(Name = "Active Customer")]
    public bool IsActive { get; set; } = true;

    public bool IsLoading { get; set; }
    public bool IsEditMode { get; set; }
    public int? CustomerId { get; set; }

    public override async Task MountAsync()
    {
        // Check if we're in edit mode
        if (Request.Query.ContainsKey("customerId"))
        {
            if (int.TryParse(Request.Query["customerId"], out var id))
            {
                await LoadCustomer(id);
            }
        }
    }

    private async Task LoadCustomer(int id)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer != null)
            {
                IsEditMode = true;
                CustomerId = customer.Id;
                FirstName = customer.FirstName;
                LastName = customer.LastName;
                Email = customer.Email;
                Phone = customer.Phone;
                Address = customer.Address;
                City = customer.City;
                PostalCode = customer.PostalCode;
                Country = customer.Country;
                Notes = customer.Notes;
                IsActive = customer.IsActive;
            }
        }
        catch (Exception)
        {
            ShowToast("Failed to load customer data", "error");
        }
    }

    public async Task SaveCustomer()
    {
        // Use Guardian for comprehensive input validation
        try
        {
            ValidateInput(FirstName, nameof(FirstName));
            ValidateInput(LastName, nameof(LastName));
            ValidateEmail(Email, nameof(Email));
            
            if (!string.IsNullOrWhiteSpace(Phone))
            {
                ValidateInput(Phone, nameof(Phone));
            }
        }
        catch (ArgumentException)
        {
            ShowToast("Please correct the validation errors", "error");
            return;
        }

        if (!Validate())
        {
            ShowToast("Please correct the errors below", "error");
            return;
        }

        IsLoading = true;

        try
        {
            var customer = new Customer
            {
                Id = CustomerId ?? 0,
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                Phone = Phone,
                Address = Address,
                City = City,
                PostalCode = PostalCode,
                Country = Country,
                Notes = Notes,
                IsActive = IsActive
            };

            if (IsEditMode && CustomerId.HasValue)
            {
                await _customerService.UpdateCustomerAsync(customer);
                ShowToast($"Customer {customer.FullName} updated successfully!", "success");
                
                DispatchNoundryEvent(new CustomerUpdated 
                { 
                    CustomerId = customer.Id, 
                    CustomerName = customer.FullName 
                }, Scope.Global);
            }
            else
            {
                var createdCustomer = await _customerService.CreateCustomerAsync(customer);
                ShowToast($"Customer {createdCustomer.FullName} created successfully!", "success");
                
                DispatchNoundryEvent(new CustomerCreated 
                { 
                    CustomerId = createdCustomer.Id, 
                    CustomerName = createdCustomer.FullName 
                }, Scope.Global);

                ClearForm();
            }

            ComponentContext.TrackComponentUsage("CustomerForm", IsEditMode ? "Update" : "Create");
        }
        catch (Exception ex)
        {
            ShowToast($"Failed to save customer: {ex.Message}", "error");
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
        Phone = "";
        Address = "";
        City = "";
        PostalCode = "";
        Country = "USA";
        Notes = "";
        IsActive = true;
        CustomerId = null;
        IsEditMode = false;
        
        ShowToast("Form cleared", "info");
        ComponentContext.TrackComponentUsage("CustomerForm", "Clear");
    }

    public async Task CheckEmailAvailability()
    {
        if (string.IsNullOrWhiteSpace(Email) || !new EmailAddressAttribute().IsValid(Email))
            return;

        // In a real app, this would check if email exists
        // For demo, simulate availability check
        await Task.Delay(500);
        
        var isAvailable = !Email.Contains("test@taken.com"); // Simulate taken email
        if (!isAvailable)
        {
            ModelState.AddModelError(nameof(Email), "This email address is already in use.");
            ShowToast("Email already exists", "warning");
        }
        else
        {
            ShowToast("Email available", "success");
        }

        ComponentContext.TrackComponentUsage("CustomerForm", "EmailCheck");
    }

    public void FillSampleData()
    {
        var samples = new[]
        {
            new { FirstName = "Alice", LastName = "Johnson", Email = "alice.johnson@example.com", Phone = "+1-555-0101", City = "San Francisco", Country = "USA" },
            new { FirstName = "Bob", LastName = "Williams", Email = "bob.williams@example.com", Phone = "+1-555-0102", City = "Seattle", Country = "USA" },
            new { FirstName = "Carol", LastName = "Brown", Email = "carol.brown@example.com", Phone = "+1-555-0103", City = "Denver", Country = "USA" },
            new { FirstName = "David", LastName = "Wilson", Email = "david.wilson@example.com", Phone = "+1-555-0104", City = "Austin", Country = "USA" }
        };

        var sample = samples[new Random().Next(samples.Length)];
        
        FirstName = sample.FirstName;
        LastName = sample.LastName;
        Email = sample.Email;
        Phone = sample.Phone;
        City = sample.City;
        Country = sample.Country;
        Address = $"{new Random().Next(100, 9999)} {new[] { "Main St", "Oak Ave", "Pine Rd", "Elm Dr", "Maple Ln" }[new Random().Next(5)]}";
        PostalCode = $"{new Random().Next(10000, 99999)}";
        
        ShowToast($"Sample data filled for {sample.FirstName}", "info");
        ComponentContext.TrackComponentUsage("CustomerForm", "FillSample");
    }
}