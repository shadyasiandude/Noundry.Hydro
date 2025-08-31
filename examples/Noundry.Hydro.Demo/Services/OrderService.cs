using Microsoft.EntityFrameworkCore;
using Noundry.Hydro.Demo.Data;
using Noundry.Hydro.Demo.Models;

namespace Noundry.Hydro.Demo.Services;

public interface IOrderService
{
    Task<List<Order>> GetOrdersAsync(string? searchTerm = null, OrderStatus? status = null, int page = 1, int pageSize = 10);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<Order> CreateOrderAsync(Order order);
    Task<Order> UpdateOrderAsync(Order order);
    Task<bool> DeleteOrderAsync(int id);
    Task<int> GetOrderCountAsync(string? searchTerm = null, OrderStatus? status = null);
    Task<List<Order>> GetRecentOrdersAsync(int count = 5);
    Task<OrderStats> GetOrderStatsAsync();
    Task<Order> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
}

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ApplicationDbContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Order>> GetOrdersAsync(string? searchTerm = null, OrderStatus? status = null, int page = 1, int pageSize = 10)
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(o => 
                o.OrderNumber.Contains(searchTerm) ||
                o.Customer.FirstName.Contains(searchTerm) ||
                o.Customer.LastName.Contains(searchTerm) ||
                o.Customer.Email.Contains(searchTerm));
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status);
        }

        return await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Invoices)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        try
        {
            // Generate order number if not provided
            if (string.IsNullOrWhiteSpace(order.OrderNumber))
            {
                order.OrderNumber = await GenerateOrderNumberAsync();
            }

            order.OrderDate = DateTime.UtcNow;
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new order: {OrderNumber} for customer {CustomerId}", 
                order.OrderNumber, order.CustomerId);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order: {OrderNumber}", order.OrderNumber);
            throw;
        }
    }

    public async Task<Order> UpdateOrderAsync(Order order)
    {
        try
        {
            order.UpdatedAt = DateTime.UtcNow;
            _context.Entry(order).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated order: {OrderNumber} ({Id})", 
                order.OrderNumber, order.Id);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order: {OrderId}", order.Id);
            throw;
        }
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Invoices)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return false;

            // Check if order has invoices
            if (order.Invoices.Any())
            {
                // Can't delete orders with invoices - set to cancelled instead
                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cancelled order with invoices: {OrderId}", id);
            }
            else
            {
                // Remove order items first
                _context.OrderItems.RemoveRange(order.OrderItems);
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted order: {OrderId}", id);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order: {OrderId}", id);
            throw;
        }
    }

    public async Task<int> GetOrderCountAsync(string? searchTerm = null, OrderStatus? status = null)
    {
        var query = _context.Orders.Include(o => o.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(o => 
                o.OrderNumber.Contains(searchTerm) ||
                o.Customer.FirstName.Contains(searchTerm) ||
                o.Customer.LastName.Contains(searchTerm) ||
                o.Customer.Email.Contains(searchTerm));
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status);
        }

        return await query.CountAsync();
    }

    public async Task<List<Order>> GetRecentOrdersAsync(int count = 5)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<OrderStats> GetOrderStatsAsync()
    {
        var totalOrders = await _context.Orders.CountAsync();
        var totalRevenue = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .SumAsync(o => o.TotalAmount);

        var statusCounts = await _context.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var averageOrderValue = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .AverageAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var monthlyOrders = await _context.Orders
            .Where(o => o.OrderDate >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();

        var monthlyRevenue = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed && 
                       o.OrderDate >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(o => o.TotalAmount);

        return new OrderStats
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            AverageOrderValue = averageOrderValue,
            MonthlyOrders = monthlyOrders,
            MonthlyRevenue = monthlyRevenue,
            StatusDistribution = statusCounts.ToDictionary(sc => sc.Status, sc => sc.Count),
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<Order> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        try
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new ArgumentException("Order not found", nameof(orderId));

            var oldStatus = order.Status;
            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            // Update delivery dates based on status
            if (newStatus == OrderStatus.Shipped && order.ExpectedDeliveryDate == null)
            {
                order.ExpectedDeliveryDate = DateTime.UtcNow.AddDays(3); // 3 days for delivery
            }
            else if (newStatus == OrderStatus.Delivered && order.ActualDeliveryDate == null)
            {
                order.ActualDeliveryDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated order {OrderId} status from {OldStatus} to {NewStatus}", 
                orderId, oldStatus, newStatus);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status: {OrderId}", orderId);
            throw;
        }
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        var today = DateTime.UtcNow;
        var prefix = $"ORD-{today:yyyyMM}";
        
        var lastOrderNumber = await _context.Orders
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        if (lastOrderNumber == null)
        {
            return $"{prefix}-001";
        }

        var lastSequence = int.Parse(lastOrderNumber.Substring(lastOrderNumber.LastIndexOf('-') + 1));
        return $"{prefix}-{(lastSequence + 1):D3}";
    }
}

public class OrderStats
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int MonthlyOrders { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public Dictionary<OrderStatus, int> StatusDistribution { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}