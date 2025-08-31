using Microsoft.EntityFrameworkCore;
using Noundry.Hydro.Demo.Data;
using Noundry.Hydro.Demo.Models;

namespace Noundry.Hydro.Demo.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetCustomersAsync(string? searchTerm = null, int page = 1, int pageSize = 10);
    Task<Customer?> GetCustomerByIdAsync(int id);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer> UpdateCustomerAsync(Customer customer);
    Task<bool> DeleteCustomerAsync(int id);
    Task<int> GetCustomerCountAsync(string? searchTerm = null);
    Task<List<Customer>> GetRecentCustomersAsync(int count = 5);
    Task<CustomerStats> GetCustomerStatsAsync(int customerId);
}

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ApplicationDbContext context, ILogger<CustomerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Customer>> GetCustomersAsync(string? searchTerm = null, int page = 1, int pageSize = 10)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(c => 
                c.FirstName.Contains(searchTerm) ||
                c.LastName.Contains(searchTerm) ||
                c.Email.Contains(searchTerm) ||
                (c.Phone != null && c.Phone.Contains(searchTerm)));
        }

        return await query
            .Include(c => c.Orders)
            .OrderByDescending(c => c.DateJoined)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _context.Customers
            .Include(c => c.Orders)
            .ThenInclude(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(c => c.Invoices)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        try
        {
            customer.DateJoined = DateTime.UtcNow;
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new customer: {CustomerName} ({Email})", 
                customer.FullName, customer.Email);

            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer: {Email}", customer.Email);
            throw;
        }
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        try
        {
            _context.Entry(customer).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated customer: {CustomerName} ({Id})", 
                customer.FullName, customer.Id);

            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer: {CustomerId}", customer.Id);
            throw;
        }
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        try
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return false;

            // Check if customer has orders or invoices
            var hasOrders = await _context.Orders.AnyAsync(o => o.CustomerId == id);
            var hasInvoices = await _context.Invoices.AnyAsync(i => i.CustomerId == id);

            if (hasOrders || hasInvoices)
            {
                // Soft delete by deactivating
                customer.IsActive = false;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deactivated customer with existing data: {CustomerId}", id);
            }
            else
            {
                // Hard delete if no related data
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted customer: {CustomerId}", id);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer: {CustomerId}", id);
            throw;
        }
    }

    public async Task<int> GetCustomerCountAsync(string? searchTerm = null)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(c => 
                c.FirstName.Contains(searchTerm) ||
                c.LastName.Contains(searchTerm) ||
                c.Email.Contains(searchTerm) ||
                (c.Phone != null && c.Phone.Contains(searchTerm)));
        }

        return await query.CountAsync();
    }

    public async Task<List<Customer>> GetRecentCustomersAsync(int count = 5)
    {
        return await _context.Customers
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.DateJoined)
            .Take(count)
            .ToListAsync();
    }

    public async Task<CustomerStats> GetCustomerStatsAsync(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            throw new ArgumentException("Customer not found", nameof(customerId));

        var totalOrders = await _context.Orders.CountAsync(o => o.CustomerId == customerId);
        var completedOrders = await _context.Orders.CountAsync(o => o.CustomerId == customerId && o.Status == OrderStatus.Completed);
        var totalSpent = await _context.Orders
            .Where(o => o.CustomerId == customerId && o.Status == OrderStatus.Completed)
            .SumAsync(o => o.TotalAmount);

        var lastOrderDate = await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .MaxAsync(o => (DateTime?)o.OrderDate);

        var averageOrderValue = completedOrders > 0 ? totalSpent / completedOrders : 0;

        var pendingInvoices = await _context.Invoices.CountAsync(i => 
            i.CustomerId == customerId && 
            (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.PartiallyPaid));

        var overdueInvoices = await _context.Invoices.CountAsync(i => 
            i.CustomerId == customerId && 
            i.Status == InvoiceStatus.Sent && 
            i.DueDate < DateTime.UtcNow);

        return new CustomerStats
        {
            CustomerId = customerId,
            TotalOrders = totalOrders,
            CompletedOrders = completedOrders,
            TotalSpent = totalSpent,
            AverageOrderValue = averageOrderValue,
            LastOrderDate = lastOrderDate,
            PendingInvoices = pendingInvoices,
            OverdueInvoices = overdueInvoices,
            CustomerSince = customer.DateJoined,
            IsActive = customer.IsActive
        };
    }
}

public class CustomerStats
{
    public int CustomerId { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public int PendingInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public DateTime CustomerSince { get; set; }
    public bool IsActive { get; set; }
}