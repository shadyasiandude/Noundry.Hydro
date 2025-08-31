using Noundry.Hydro.Components;
using Noundry.Hydro.Demo.Models;
using Noundry.Hydro.Demo.Services;

namespace Noundry.Hydro.Demo.Pages.Components.Customers;

public class CustomersList : NoundryHydroComponent
{
    private readonly ICustomerService _customerService;

    public CustomersList(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public List<Customer> Customers { get; set; } = new();
    public string SearchTerm { get; set; } = "";
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool IsLoading { get; set; } = true;
    public bool ShowCreateForm { get; set; } = false;

    protected override async Task MountAsync()
    {
        await LoadCustomers();
        
        // Subscribe to customer events
        Subscribe<CustomerCreated>(async _ => await LoadCustomers());
        Subscribe<CustomerUpdated>(async _ => await LoadCustomers());
        Subscribe<CustomerDeleted>(async _ => await LoadCustomers());
    }

    public async Task LoadCustomers()
    {
        IsLoading = true;
        
        try
        {
            var customersTask = _customerService.GetCustomersAsync(SearchTerm, CurrentPage, PageSize);
            var countTask = _customerService.GetCustomerCountAsync(SearchTerm);
            
            await Task.WhenAll(customersTask, countTask);
            
            Customers = await customersTask;
            TotalCount = await countTask;
            
            ComponentContext.TrackComponentUsage("CustomersList", "DataLoaded");
        }
        catch (Exception ex)
        {
            ShowToast("Failed to load customers", "error");
            ComponentContext.TrackComponentUsage("CustomersList", "LoadError");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task Search()
    {
        CurrentPage = 1;
        ShowToast($"Searching for: {SearchTerm}", "info");
        await LoadCustomers();
        ComponentContext.TrackComponentUsage("CustomersList", "Search");
    }

    public async Task ClearSearch()
    {
        SearchTerm = "";
        CurrentPage = 1;
        await LoadCustomers();
        ShowToast("Search cleared", "info");
        ComponentContext.TrackComponentUsage("CustomersList", "ClearSearch");
    }

    public async Task GoToPage(int page)
    {
        if (page < 1 || page > TotalPages)
            return;

        CurrentPage = page;
        await LoadCustomers();
        ComponentContext.TrackComponentUsage("CustomersList", $"PageNavigation-{page}");
    }

    public async Task NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            await GoToPage(CurrentPage + 1);
        }
    }

    public async Task PreviousPage()
    {
        if (CurrentPage > 1)
        {
            await GoToPage(CurrentPage - 1);
        }
    }

    public void ViewCustomer(int customerId)
    {
        Location($"/Customers/{customerId}");
        ComponentContext.TrackComponentUsage("CustomersList", $"ViewCustomer-{customerId}");
    }

    public void EditCustomer(int customerId)
    {
        Location($"/Customers/Edit/{customerId}");
        ComponentContext.TrackComponentUsage("CustomersList", $"EditCustomer-{customerId}");
    }

    public async Task DeleteCustomer(int customerId)
    {
        var customer = Customers.FirstOrDefault(c => c.Id == customerId);
        if (customer == null)
            return;

        // In a real app, you'd show a confirmation dialog
        var confirmed = true; // Simplified for demo

        if (confirmed)
        {
            try
            {
                var result = await _customerService.DeleteCustomerAsync(customerId);
                if (result)
                {
                    ShowToast($"Customer {customer.FullName} deleted successfully", "success");
                    
                    // Dispatch event to notify other components
                    DispatchNoundryEvent(new CustomerDeleted 
                    { 
                        CustomerId = customerId, 
                        CustomerName = customer.FullName 
                    }, Scope.Global);
                    
                    await LoadCustomers();
                    ComponentContext.TrackComponentUsage("CustomersList", $"DeleteCustomer-{customerId}");
                }
                else
                {
                    ShowToast("Failed to delete customer", "error");
                }
            }
            catch (Exception)
            {
                ShowToast("Error occurred while deleting customer", "error");
            }
        }
    }

    public void ToggleCreateForm()
    {
        ShowCreateForm = !ShowCreateForm;
        ComponentContext.TrackComponentUsage("CustomersList", "ToggleCreateForm");
    }

    public void ExportCustomers()
    {
        ShowToast("Exporting customers... (Demo feature)", "info");
        
        // In a real app, this would trigger CSV/Excel export
        Client.ExecuteScript(@"
            setTimeout(() => {
                alert('Export feature would download customer data as CSV/Excel file');
            }, 1000);
        ");
        
        ComponentContext.TrackComponentUsage("CustomersList", "Export");
    }

    public async Task BulkAction(string action)
    {
        ShowToast($"Executing bulk action: {action} (Demo feature)", "info");
        
        // In a real app, this would handle bulk operations
        await LoadCustomers();
        
        ComponentContext.TrackComponentUsage("CustomersList", $"BulkAction-{action}");
    }
}

public record CustomerCreated(int CustomerId, string CustomerName);
public record CustomerUpdated(int CustomerId, string CustomerName);
public record CustomerDeleted(int CustomerId, string CustomerName);