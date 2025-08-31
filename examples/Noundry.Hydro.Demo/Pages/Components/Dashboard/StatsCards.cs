using Noundry.Hydro.Components;
using Noundry.Hydro.Demo.Services;

namespace Noundry.Hydro.Demo.Pages.Components.Dashboard;

public class StatsCards : NoundryHydroComponent
{
    private readonly IDashboardService _dashboardService;

    public StatsCards(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public DashboardStats Stats { get; set; } = new();
    public bool IsLoading { get; set; } = true;
    public DateTime LastRefresh { get; set; }

    protected override async Task MountAsync()
    {
        await LoadStats();
        
        // Subscribe to data refresh events
        Subscribe<RefreshDashboard>(async _ => await LoadStats());
        
        // Auto-refresh every 5 minutes
        Client.ExecuteScript(@"
            setInterval(async () => {
                await this.invoke(null, { name: 'LoadStats' });
            }, 300000);
        ");
    }

    [Poll(Interval = 60000)] // Refresh every minute for real-time feel
    public async Task LoadStats()
    {
        IsLoading = true;
        
        try
        {
            Stats = await _dashboardService.GetDashboardDataAsync()
                .ContinueWith(t => t.Result.Stats);
            LastRefresh = DateTime.UtcNow;
            
            // Dispatch stats updated event for other components
            DispatchNoundryEvent(new StatsUpdated 
            { 
                Stats = Stats, 
                UpdatedAt = LastRefresh 
            }, Scope.Global);
        }
        catch (Exception ex)
        {
            ShowToast("Failed to load dashboard stats", "error");
            ComponentContext.TrackComponentUsage("StatsCards", "LoadError");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshStats()
    {
        ShowToast("Refreshing dashboard stats...", "info");
        await LoadStats();
        ShowToast("Dashboard stats updated!", "success");
        ComponentContext.TrackComponentUsage("StatsCards", "ManualRefresh");
    }

    public void ViewDetails(string statType)
    {
        var url = statType.ToLowerInvariant() switch
        {
            "customers" => "/Customers",
            "products" => "/Products", 
            "orders" => "/Orders",
            "invoices" => "/Invoices",
            _ => "/"
        };

        Location(url);
        ComponentContext.TrackComponentUsage("StatsCards", $"ViewDetails-{statType}");
    }
}

public record RefreshDashboard();
public record StatsUpdated(DashboardStats Stats, DateTime UpdatedAt);