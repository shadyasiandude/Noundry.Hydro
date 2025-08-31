using Noundry.Hydro.Components;
using Noundry.Hydro.Demo.Services;

namespace Noundry.Hydro.Demo.Pages.Components.Dashboard;

public class DashboardOverview : NoundryHydroComponent
{
    private readonly IDashboardService _dashboardService;

    public DashboardOverview(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public DashboardData DashboardData { get; set; } = new();
    public bool IsLoading { get; set; } = true;
    public string SelectedTimeRange { get; set; } = "30";
    public DateTime LastRefresh { get; set; }

    protected override async Task MountAsync()
    {
        await LoadDashboardData();
        
        // Subscribe to various events that should trigger dashboard refresh
        Subscribe<UserLoggedIn>(async _ => await LoadDashboardData());
        Subscribe<OrderCreated>(async _ => await LoadDashboardData());
        Subscribe<CustomerCreated>(async _ => await LoadDashboardData());
        Subscribe<InvoicePaid>(async _ => await LoadDashboardData());
    }

    public async Task LoadDashboardData()
    {
        IsLoading = true;
        
        try
        {
            DashboardData = await _dashboardService.GetDashboardDataAsync();
            LastRefresh = DateTime.UtcNow;
            
            // Dispatch dashboard loaded event
            DispatchNoundryEvent(new DashboardLoaded 
            { 
                Data = DashboardData, 
                LoadedAt = LastRefresh 
            }, Scope.Global);

            ComponentContext.TrackComponentUsage("DashboardOverview", "DataLoaded");
        }
        catch (Exception ex)
        {
            ShowToast("Failed to load dashboard data", "error");
            ComponentContext.TrackComponentUsage("DashboardOverview", "LoadError");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshAll()
    {
        ShowToast("Refreshing all dashboard data...", "info");
        await LoadDashboardData();
        
        // Dispatch refresh event to all other components
        DispatchNoundryEvent(new RefreshDashboard(), Scope.Global);
        
        ShowToast("Dashboard refreshed successfully!", "success");
        ComponentContext.TrackComponentUsage("DashboardOverview", "FullRefresh");
    }

    public async Task ChangeTimeRange(string timeRange)
    {
        SelectedTimeRange = timeRange;
        ShowToast($"Updating view for last {timeRange} days...", "info");
        
        // In a real app, this would filter the data by time range
        await LoadDashboardData();
        
        ComponentContext.TrackComponentUsage("DashboardOverview", $"TimeRangeChanged-{timeRange}");
    }

    public void NavigateToSection(string section)
    {
        var url = section.ToLowerInvariant() switch
        {
            "customers" => "/Customers",
            "products" => "/Products",
            "orders" => "/Orders", 
            "invoices" => "/Invoices",
            "analytics" => "/Analytics",
            _ => "/"
        };

        Location(url);
        ComponentContext.TrackComponentUsage("DashboardOverview", $"Navigation-{section}");
    }

    public void ExportData(string dataType)
    {
        ShowToast($"Exporting {dataType} data... (Demo feature)", "info");
        
        // In a real app, this would trigger data export
        Client.ExecuteScript($@"
            setTimeout(() => {{
                alert('Export feature would download {dataType} data as CSV/Excel file');
            }}, 1000);
        ");
        
        ComponentContext.TrackComponentUsage("DashboardOverview", $"Export-{dataType}");
    }
}

public record DashboardLoaded(DashboardData Data, DateTime LoadedAt);
public record OrderCreated(int OrderId, string CustomerName, decimal Amount);
public record CustomerCreated(int CustomerId, string CustomerName);
public record InvoicePaid(int InvoiceId, decimal Amount);