using Microsoft.EntityFrameworkCore;
using Noundry.Hydro.Demo.Data;
using Noundry.Hydro.Demo.Models;

namespace Noundry.Hydro.Demo.Services;

public interface IInvoiceService
{
    Task<List<Invoice>> GetInvoicesAsync(string? searchTerm = null, InvoiceStatus? status = null, int page = 1, int pageSize = 10);
    Task<Invoice?> GetInvoiceByIdAsync(int id);
    Task<Invoice> CreateInvoiceAsync(Invoice invoice);
    Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
    Task<bool> DeleteInvoiceAsync(int id);
    Task<int> GetInvoiceCountAsync(string? searchTerm = null, InvoiceStatus? status = null);
    Task<List<Invoice>> GetOverdueInvoicesAsync();
    Task<InvoiceStats> GetInvoiceStatsAsync();
    Task<Invoice> CreateInvoiceFromOrderAsync(int orderId);
    Task<Payment> AddPaymentAsync(Payment payment);
}

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(ApplicationDbContext context, ILogger<InvoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Invoice>> GetInvoicesAsync(string? searchTerm = null, InvoiceStatus? status = null, int page = 1, int pageSize = 10)
    {
        var query = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Order)
            .Include(i => i.InvoiceItems)
            .ThenInclude(ii => ii.Product)
            .Include(i => i.Payments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(i => 
                i.InvoiceNumber.Contains(searchTerm) ||
                i.Customer.FirstName.Contains(searchTerm) ||
                i.Customer.LastName.Contains(searchTerm) ||
                i.Customer.Email.Contains(searchTerm));
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status);
        }

        return await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(int id)
    {
        return await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Order)
            .Include(i => i.InvoiceItems)
            .ThenInclude(ii => ii.Product)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
    {
        try
        {
            // Generate invoice number if not provided
            if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            {
                invoice.InvoiceNumber = await GenerateInvoiceNumberAsync();
            }

            invoice.InvoiceDate = DateTime.UtcNow;
            invoice.CreatedAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            // Set due date if not provided (30 days from invoice date)
            if (invoice.DueDate == default)
            {
                invoice.DueDate = invoice.InvoiceDate.AddDays(30);
            }

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new invoice: {InvoiceNumber} for customer {CustomerId}", 
                invoice.InvoiceNumber, invoice.CustomerId);

            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice: {InvoiceNumber}", invoice.InvoiceNumber);
            throw;
        }
    }

    public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
    {
        try
        {
            invoice.UpdatedAt = DateTime.UtcNow;
            _context.Entry(invoice).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated invoice: {InvoiceNumber} ({Id})", 
                invoice.InvoiceNumber, invoice.Id);

            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice: {InvoiceId}", invoice.Id);
            throw;
        }
    }

    public async Task<bool> DeleteInvoiceAsync(int id)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return false;

            // Check if invoice has payments
            if (invoice.Payments.Any())
            {
                // Can't delete invoices with payments - set to cancelled instead
                invoice.Status = InvoiceStatus.Cancelled;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cancelled invoice with payments: {InvoiceId}", id);
            }
            else
            {
                // Remove invoice items and payments first
                _context.InvoiceItems.RemoveRange(invoice.InvoiceItems);
                _context.Payments.RemoveRange(invoice.Payments);
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted invoice: {InvoiceId}", id);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice: {InvoiceId}", id);
            throw;
        }
    }

    public async Task<int> GetInvoiceCountAsync(string? searchTerm = null, InvoiceStatus? status = null)
    {
        var query = _context.Invoices.Include(i => i.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(i => 
                i.InvoiceNumber.Contains(searchTerm) ||
                i.Customer.FirstName.Contains(searchTerm) ||
                i.Customer.LastName.Contains(searchTerm) ||
                i.Customer.Email.Contains(searchTerm));
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status);
        }

        return await query.CountAsync();
    }

    public async Task<List<Invoice>> GetOverdueInvoicesAsync()
    {
        return await _context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.Status == InvoiceStatus.Sent && i.DueDate < DateTime.UtcNow)
            .OrderBy(i => i.DueDate)
            .ToListAsync();
    }

    public async Task<InvoiceStats> GetInvoiceStatsAsync()
    {
        var totalInvoices = await _context.Invoices.CountAsync();
        var totalAmount = await _context.Invoices.SumAsync(i => i.TotalAmount);
        var totalPaid = await _context.Invoices.SumAsync(i => i.AmountPaid);
        var totalOutstanding = totalAmount - totalPaid;

        var statusCounts = await _context.Invoices
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var overdueCount = await _context.Invoices
            .CountAsync(i => i.Status == InvoiceStatus.Sent && i.DueDate < DateTime.UtcNow);

        var overdueAmount = await _context.Invoices
            .Where(i => i.Status == InvoiceStatus.Sent && i.DueDate < DateTime.UtcNow)
            .SumAsync(i => i.AmountDue);

        var monthlyInvoiced = await _context.Invoices
            .Where(i => i.InvoiceDate >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(i => i.TotalAmount);

        var averageInvoiceValue = totalInvoices > 0 ? totalAmount / totalInvoices : 0;

        return new InvoiceStats
        {
            TotalInvoices = totalInvoices,
            TotalAmount = totalAmount,
            TotalPaid = totalPaid,
            TotalOutstanding = totalOutstanding,
            OverdueCount = overdueCount,
            OverdueAmount = overdueAmount,
            MonthlyInvoiced = monthlyInvoiced,
            AverageInvoiceValue = averageInvoiceValue,
            StatusDistribution = statusCounts.ToDictionary(sc => sc.Status, sc => sc.Count),
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<Invoice> CreateInvoiceFromOrderAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new ArgumentException("Order not found", nameof(orderId));

        var invoice = new Invoice
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            CustomerId = order.CustomerId,
            OrderId = order.Id,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = InvoiceStatus.Draft,
            Subtotal = order.Subtotal,
            TaxAmount = order.TaxAmount,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            PaymentMethod = order.PaymentMethod,
            TermsAndConditions = "Payment due within 30 days. Late fees may apply after due date.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Create invoice items from order items
        var invoiceItems = order.OrderItems.Select(oi => new InvoiceItem
        {
            InvoiceId = invoice.Id,
            ProductId = oi.ProductId,
            Description = oi.Product.Name,
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            LineTotal = oi.LineTotal
        }).ToList();

        _context.InvoiceItems.AddRange(invoiceItems);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created invoice {InvoiceNumber} from order {OrderNumber}", 
            invoice.InvoiceNumber, order.OrderNumber);

        return invoice;
    }

    public async Task<Payment> AddPaymentAsync(Payment payment)
    {
        try
        {
            payment.CreatedAt = DateTime.UtcNow;
            _context.Payments.Add(payment);

            // Update invoice paid amount and status
            var invoice = await _context.Invoices.FindAsync(payment.InvoiceId);
            if (invoice != null)
            {
                invoice.AmountPaid += payment.Amount;
                
                if (invoice.AmountPaid >= invoice.TotalAmount)
                {
                    invoice.Status = InvoiceStatus.Paid;
                    invoice.PaidDate = payment.PaymentDate;
                }
                else if (invoice.AmountPaid > 0)
                {
                    invoice.Status = InvoiceStatus.PartiallyPaid;
                }

                invoice.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Added payment {Amount:C} to invoice {InvoiceId}", 
                payment.Amount, payment.InvoiceId);

            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment to invoice: {InvoiceId}", payment.InvoiceId);
            throw;
        }
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var today = DateTime.UtcNow;
        var prefix = $"INV-{today:yyyyMM}";
        
        var lastInvoiceNumber = await _context.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        if (lastInvoiceNumber == null)
        {
            return $"{prefix}-001";
        }

        var lastSequence = int.Parse(lastInvoiceNumber.Substring(lastInvoiceNumber.LastIndexOf('-') + 1));
        return $"{prefix}-{(lastSequence + 1):D3}";
    }
}

public class InvoiceStats
{
    public int TotalInvoices { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int OverdueCount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal MonthlyInvoiced { get; set; }
    public decimal AverageInvoiceValue { get; set; }
    public Dictionary<InvoiceStatus, int> StatusDistribution { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}