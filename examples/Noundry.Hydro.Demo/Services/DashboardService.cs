using Microsoft.EntityFrameworkCore;
using Noundry.Hydro.Demo.Data;
using Noundry.Hydro.Demo.Models;

namespace Noundry.Hydro.Demo.Services;

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync();
    Task<List<RecentActivity>> GetRecentActivitiesAsync(int count = 10);
    Task<ChartData> GetRevenueChartDataAsync(int days = 30);
    Task<ChartData> GetOrderStatusChartDataAsync();
    Task<List<TopCustomer>> GetTopCustomersAsync(int count = 5);
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        var stats = await GetBasicStatsAsync();
        var recentActivities = await GetRecentActivitiesAsync(5);
        var revenueChart = await GetRevenueChartDataAsync(30);
        var orderStatusChart = await GetOrderStatusChartDataAsync();
        var topCustomers = await GetTopCustomersAsync(5);

        return new DashboardData
        {
            Stats = stats,
            RecentActivities = recentActivities,
            RevenueChart = revenueChart,
            OrderStatusChart = orderStatusChart,
            TopCustomers = topCustomers,
            LastUpdated = DateTime.UtcNow
        };
    }

    private async Task<DashboardStats> GetBasicStatsAsync()
    {
        var totalCustomers = await _context.Customers.CountAsync(c => c.IsActive);
        var totalProducts = await _context.Products.CountAsync(p => p.IsActive);
        var totalOrders = await _context.Orders.CountAsync();
        var totalInvoices = await _context.Invoices.CountAsync();

        var totalRevenue = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .SumAsync(o => o.TotalAmount);

        var monthlyRevenue = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed && 
                       o.OrderDate >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(o => o.TotalAmount);

        var weeklyRevenue = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed && 
                       o.OrderDate >= DateTime.UtcNow.AddDays(-7))
            .SumAsync(o => o.TotalAmount);

        var todayRevenue = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed && 
                       o.OrderDate.Date == DateTime.UtcNow.Date)
            .SumAsync(o => o.TotalAmount);

        var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        var processingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Processing);
        var shippedOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Shipped);

        var overdueInvoices = await _context.Invoices
            .CountAsync(i => i.Status == InvoiceStatus.Sent && i.DueDate < DateTime.UtcNow);

        var paidInvoices = await _context.Invoices.CountAsync(i => i.Status == InvoiceStatus.Paid);

        var lowStockProducts = await _context.Products
            .CountAsync(p => p.IsActive && p.StockQuantity <= p.MinimumStock);

        var newCustomersThisMonth = await _context.Customers
            .CountAsync(c => c.DateJoined >= DateTime.UtcNow.AddDays(-30));

        return new DashboardStats
        {
            TotalCustomers = totalCustomers,
            TotalProducts = totalProducts,
            TotalOrders = totalOrders,
            TotalInvoices = totalInvoices,
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            WeeklyRevenue = weeklyRevenue,
            TodayRevenue = todayRevenue,
            PendingOrders = pendingOrders,
            ProcessingOrders = processingOrders,
            ShippedOrders = shippedOrders,
            OverdueInvoices = overdueInvoices,
            PaidInvoices = paidInvoices,
            LowStockProducts = lowStockProducts,
            NewCustomersThisMonth = newCustomersThisMonth,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<List<RecentActivity>> GetRecentActivitiesAsync(int count = 10)
    {
        var activities = new List<RecentActivity>();

        // Recent orders
        var recentOrders = await _context.Orders
            .Include(o => o.Customer)
            .OrderByDescending(o => o.CreatedAt)
            .Take(count / 2)
            .ToListAsync();

        activities.AddRange(recentOrders.Select(o => new RecentActivity
        {
            Id = o.Id,
            Type = "order",
            Title = $"New Order #{o.OrderNumber}",
            Description = $"{o.Customer.FullName} placed an order for {o.TotalAmount:C}",
            Timestamp = o.CreatedAt,
            Icon = "🛒",
            Color = "blue"
        }));

        // Recent customers
        var recentCustomers = await _context.Customers
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.DateJoined)
            .Take(count / 2)
            .ToListAsync();

        activities.AddRange(recentCustomers.Select(c => new RecentActivity
        {
            Id = c.Id,
            Type = "customer",
            Title = "New Customer Registration",
            Description = $"{c.FullName} joined the platform",
            Timestamp = c.DateJoined,
            Icon = "👤",
            Color = "green"
        }));

        return activities
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToList();
    }

    public async Task<ChartData> GetRevenueChartDataAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        
        var revenueData = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.OrderDate >= startDate)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
            .OrderBy(g => g.Date)
            .ToListAsync();

        var labels = new List<string>();
        var values = new List<decimal>();
        
        for (var date = startDate.Date; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
        {
            labels.Add(date.ToString("MMM dd"));
            var dayRevenue = revenueData.FirstOrDefault(r => r.Date == date)?.Revenue ?? 0;
            values.Add(dayRevenue);
        }

        return new ChartData
        {
            Labels = labels,
            Values = values.Select(v => (double)v).ToList(),
            Title = $"Revenue - Last {days} Days",
            Type = "line"
        };
    }

    public async Task<ChartData> GetOrderStatusChartDataAsync()
    {
        var statusCounts = await _context.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var labels = statusCounts.Select(sc => sc.Status.ToString()).ToList();
        var values = statusCounts.Select(sc => (double)sc.Count).ToList();

        return new ChartData
        {
            Labels = labels,
            Values = values,
            Title = "Orders by Status",
            Type = "doughnut"
        };
    }

    public async Task<List<TopCustomer>> GetTopCustomersAsync(int count = 5)
    {
        return await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .GroupBy(o => o.Customer)
            .Select(g => new TopCustomer
            {
                CustomerId = g.Key.Id,
                CustomerName = g.Key.FullName,
                Email = g.Key.Email,
                TotalOrders = g.Count(),
                TotalSpent = g.Sum(o => o.TotalAmount),
                LastOrderDate = g.Max(o => o.OrderDate)
            })
            .OrderByDescending(tc => tc.TotalSpent)
            .Take(count)
            .ToListAsync();
    }
}

public class DashboardData
{
    public DashboardStats Stats { get; set; } = new();
    public List<RecentActivity> RecentActivities { get; set; } = new();
    public ChartData RevenueChart { get; set; } = new();
    public ChartData OrderStatusChart { get; set; } = new();
    public List<TopCustomer> TopCustomers { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class RecentActivity
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
    
    public string TimeAgo
    {
        get
        {
            var timeSpan = DateTime.UtcNow - Timestamp;
            return timeSpan.TotalMinutes switch
            {
                < 1 => "Just now",
                < 60 => $"{(int)timeSpan.TotalMinutes} minutes ago",
                < 1440 => $"{(int)timeSpan.TotalHours} hours ago",
                _ => $"{(int)timeSpan.TotalDays} days ago"
            };
        }
    }
}

public class ChartData
{
    public List<string> Labels { get; set; } = new();
    public List<double> Values { get; set; } = new();
    public string Title { get; set; } = "";
    public string Type { get; set; } = "line";
}

public class TopCustomer
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string Email { get; set; } = "";
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime LastOrderDate { get; set; }
}