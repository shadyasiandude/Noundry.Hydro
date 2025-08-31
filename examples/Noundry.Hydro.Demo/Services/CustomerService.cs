using Noundry.Hydro.Demo.Data;
using Noundry.Hydro.Demo.Models;
using Tuxedo;
using Guardian;

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
    private readonly TuxedoDataContext _tuxedoContext;
    private readonly IGuardian _guard;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        TuxedoDataContext tuxedoContext,
        IGuardian guard,
        ILogger<CustomerService> logger)
    {
        _tuxedoContext = tuxedoContext;
        _guard = guard;
        _logger = logger;
    }

    public async Task<List<Customer>> GetCustomersAsync(string? searchTerm = null, int page = 1, int pageSize = 10)
    {
        // Use pure Tuxedo ORM instead of Entity Framework
        return await _tuxedoContext.GetCustomersAsync(searchTerm, page, pageSize);
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        // Use Tuxedo ORM for high-performance query
        return await _tuxedoContext.GetCustomerByIdAsync(id);
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        try
        {
            // Use pure Tuxedo ORM for creation
            customer.DateJoined = DateTime.UtcNow;
            var customerId = await _tuxedoContext.CreateCustomerAsync(customer);
            customer.Id = customerId;

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
            // Use pure Tuxedo ORM for updates
            await _tuxedoContext.UpdateCustomerAsync(customer);

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
            // Use pure Tuxedo ORM for deletion
            var result = await _tuxedoContext.DeleteCustomerAsync(id);
            
            if (result > 0)
            {
                _logger.LogInformation("Customer deleted/deactivated: {CustomerId}", id);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer: {CustomerId}", id);
            throw;
        }
    }

    public async Task<int> GetCustomerCountAsync(string? searchTerm = null)
    {
        var sql = @"
            SELECT COUNT(*) FROM Customers 
            WHERE (@SearchTerm IS NULL OR 
                   FirstName LIKE '%' + @SearchTerm + '%' OR 
                   LastName LIKE '%' + @SearchTerm + '%' OR 
                   Email LIKE '%' + @SearchTerm + '%' OR
                   Phone LIKE '%' + @SearchTerm + '%')";

        return await _tuxedoContext.QueryFirstOrDefaultAsync<int>(sql, new { SearchTerm = searchTerm });
    }

    public async Task<List<Customer>> GetRecentCustomersAsync(int count = 5)
    {
        var sql = @"
            SELECT TOP(@Count) * FROM Customers 
            WHERE IsActive = 1 
            ORDER BY DateJoined DESC";

        return (await _tuxedoContext.QueryAsync<Customer>(sql, new { Count = count })).ToList();
    }

    public async Task<CustomerStats> GetCustomerStatsAsync(int customerId)
    {
        var customer = await _tuxedoContext.GetCustomerByIdAsync(customerId);
        if (customer == null)
            throw new ArgumentException("Customer not found", nameof(customerId));

        var sql = @"
            SELECT 
                @CustomerId as CustomerId,
                (SELECT COUNT(*) FROM Orders WHERE CustomerId = @CustomerId) as TotalOrders,
                (SELECT COUNT(*) FROM Orders WHERE CustomerId = @CustomerId AND Status = @CompletedStatus) as CompletedOrders,
                (SELECT ISNULL(SUM(TotalAmount), 0) FROM Orders WHERE CustomerId = @CustomerId AND Status = @CompletedStatus) as TotalSpent,
                (SELECT MAX(OrderDate) FROM Orders WHERE CustomerId = @CustomerId) as LastOrderDate,
                (SELECT COUNT(*) FROM Invoices WHERE CustomerId = @CustomerId AND Status IN (@SentStatus, @PartiallyPaidStatus)) as PendingInvoices,
                (SELECT COUNT(*) FROM Invoices WHERE CustomerId = @CustomerId AND Status = @SentStatus AND DueDate < @Now) as OverdueInvoices";

        var stats = await _tuxedoContext.QueryFirstOrDefaultAsync<CustomerStats>(sql, new 
        {
            CustomerId = customerId,
            CompletedStatus = OrderStatus.Completed,
            SentStatus = InvoiceStatus.Sent,
            PartiallyPaidStatus = InvoiceStatus.PartiallyPaid,
            Now = DateTime.UtcNow
        });

        if (stats != null)
        {
            stats.CustomerSince = customer.DateJoined;
            stats.IsActive = customer.IsActive;
            stats.AverageOrderValue = stats.CompletedOrders > 0 ? stats.TotalSpent / stats.CompletedOrders : 0;
        }

        return stats ?? new CustomerStats { CustomerId = customerId };
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