using Bogus;
using Noundry.Hydro.Demo.Data;
using Noundry.Hydro.Demo.Models;
using Tuxedo;
using Guardian;

namespace Noundry.Hydro.Demo.Services;

public interface IDemoDataService
{
    Task GenerateSampleDataAsync();
    Task<DashboardStats> GetDashboardStatsAsync();
}

public class DemoDataService : IDemoDataService
{
    private readonly TuxedoDataContext _tuxedoContext;
    private readonly ILogger<DemoDataService> _logger;

    public DemoDataService(TuxedoDataContext tuxedoContext, ILogger<DemoDataService> logger)
    {
        _tuxedoContext = tuxedoContext;
        _logger = logger;
    }

    public async Task GenerateSampleDataAsync()
    {
        try
        {
            // Check if data already exists using Tuxedo
            var existingOrders = await _tuxedoContext.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Orders");
            if (existingOrders > 0)
            {
                _logger.LogInformation("Sample data already exists, skipping generation.");
                return;
            }

            _logger.LogInformation("Generating sample data with Tuxedo ORM...");

            // Generate additional customers using Bogus
            var customerFaker = new Faker<Customer>()
                .RuleFor(c => c.FirstName, f => f.Name.FirstName())
                .RuleFor(c => c.LastName, f => f.Name.LastName())
                .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.FirstName, c.LastName))
                .RuleFor(c => c.Phone, f => f.Phone.PhoneNumber())
                .RuleFor(c => c.Address, f => f.Address.StreetAddress())
                .RuleFor(c => c.City, f => f.Address.City())
                .RuleFor(c => c.PostalCode, f => f.Address.ZipCode())
                .RuleFor(c => c.Country, f => f.Address.Country())
                .RuleFor(c => c.DateJoined, f => f.Date.Between(DateTime.UtcNow.AddYears(-2), DateTime.UtcNow))
                .RuleFor(c => c.IsActive, f => f.Random.Bool(0.9f))
                .RuleFor(c => c.Notes, f => f.Lorem.Sentence());

            var additionalCustomers = customerFaker.Generate(20);
            
            // Set IDs starting from 4 (after seed data)
            for (int i = 0; i < additionalCustomers.Count; i++)
            {
                additionalCustomers[i].Id = i + 4;
            }

            await _context.Customers.AddRangeAsync(additionalCustomers);

            // Generate additional products
            var productFaker = new Faker<Product>()
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
                .RuleFor(p => p.Sku, f => f.Random.AlphaNumeric(8).ToUpperInvariant())
                .RuleFor(p => p.Price, f => f.Random.Decimal(10, 500))
                .RuleFor(p => p.CostPrice, (f, p) => p.Price * f.Random.Decimal(0.4m, 0.8m))
                .RuleFor(p => p.StockQuantity, f => f.Random.Number(0, 200))
                .RuleFor(p => p.MinimumStock, f => f.Random.Number(5, 20))
                .RuleFor(p => p.Category, f => f.Random.Enum<ProductCategory>())
                .RuleFor(p => p.ImageUrl, f => f.Image.PicsumUrl(300, 300))
                .RuleFor(p => p.IsActive, f => f.Random.Bool(0.95f))
                .RuleFor(p => p.IsFeatured, f => f.Random.Bool(0.2f))
                .RuleFor(p => p.CreatedAt, f => f.Date.Between(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow))
                .RuleFor(p => p.UpdatedAt, (f, p) => f.Date.Between(p.CreatedAt, DateTime.UtcNow));

            var additionalProducts = productFaker.Generate(50);
            
            // Set IDs starting from 6 (after seed data)
            for (int i = 0; i < additionalProducts.Count; i++)
            {
                additionalProducts[i].Id = i + 6;
            }

            await _context.Products.AddRangeAsync(additionalProducts);
            await _context.SaveChangesAsync();

            // Generate orders with realistic data
            await GenerateOrdersAsync();

            // Generate invoices based on orders
            await GenerateInvoicesAsync();

            _logger.LogInformation("Sample data generation completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating sample data.");
            throw;
        }
    }

    private async Task GenerateOrdersAsync()
    {
        var customers = await _context.Customers.ToListAsync();
        var products = await _context.Products.Where(p => p.IsActive).ToListAsync();

        var orderFaker = new Faker<Order>()
            .RuleFor(o => o.OrderNumber, f => $"ORD-{f.Random.Number(100000, 999999)}")
            .RuleFor(o => o.CustomerId, f => f.PickRandom(customers).Id)
            .RuleFor(o => o.OrderDate, f => f.Date.Between(DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow))
            .RuleFor(o => o.Status, f => f.Random.Enum<OrderStatus>())
            .RuleFor(o => o.ShippingAddress, f => f.Address.FullAddress())
            .RuleFor(o => o.BillingAddress, f => f.Address.FullAddress())
            .RuleFor(o => o.PaymentMethod, f => f.PickRandom("Credit Card", "PayPal", "Bank Transfer", "Cash"))
            .RuleFor(o => o.Notes, f => f.Lorem.Sentence())
            .RuleFor(o => o.ExpectedDeliveryDate, (f, o) => o.OrderDate.AddDays(f.Random.Number(3, 14)))
            .RuleFor(o => o.ActualDeliveryDate, (f, o) => 
                o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed 
                    ? o.ExpectedDeliveryDate?.AddDays(f.Random.Number(-2, 3))
                    : null)
            .RuleFor(o => o.CreatedAt, (f, o) => o.OrderDate)
            .RuleFor(o => o.UpdatedAt, (f, o) => f.Date.Between(o.CreatedAt, DateTime.UtcNow));

        var orders = orderFaker.Generate(100);

        // Set IDs
        for (int i = 0; i < orders.Count; i++)
        {
            orders[i].Id = i + 1;
        }

        await _context.Orders.AddRangeAsync(orders);
        await _context.SaveChangesAsync();

        // Generate order items
        var orderItemsList = new List<OrderItem>();
        int orderItemId = 1;

        foreach (var order in orders)
        {
            var itemCount = new Random().Next(1, 6); // 1-5 items per order
            var selectedProducts = products.OrderBy(x => Guid.NewGuid()).Take(itemCount).ToList();

            foreach (var product in selectedProducts)
            {
                var quantity = new Random().Next(1, 4);
                var unitPrice = product.Price * (decimal)new Random().NextDouble() * 0.2m + product.Price * 0.9m; // ±10% variance

                var orderItem = new OrderItem
                {
                    Id = orderItemId++,
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = Math.Round(unitPrice, 2),
                    LineTotal = Math.Round(unitPrice * quantity, 2),
                    Notes = new Random().Next(1, 5) == 1 ? "Special instructions: Handle with care" : null
                };

                orderItemsList.Add(orderItem);
            }

            // Update order totals
            order.Subtotal = orderItemsList.Where(oi => oi.OrderId == order.Id).Sum(oi => oi.LineTotal);
            order.TaxAmount = Math.Round(order.Subtotal * 0.08m, 2); // 8% tax
            order.ShippingCost = order.Subtotal > 100 ? 0 : 15.99m; // Free shipping over $100
            order.DiscountAmount = new Random().Next(1, 10) == 1 ? Math.Round(order.Subtotal * 0.1m, 2) : 0; // 10% discount sometimes
            order.TotalAmount = order.Subtotal + order.TaxAmount + order.ShippingCost - order.DiscountAmount;
        }

        await _context.OrderItems.AddRangeAsync(orderItemsList);
        await _context.SaveChangesAsync();
    }

    private async Task GenerateInvoicesAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.Status >= OrderStatus.Processing)
            .ToListAsync();

        var invoices = new List<Invoice>();
        var invoiceItems = new List<InvoiceItem>();
        var payments = new List<Payment>();

        int invoiceId = 1;
        int invoiceItemId = 1;
        int paymentId = 1;

        foreach (var order in orders.Take(75)) // Create invoices for 75% of eligible orders
        {
            var invoice = new Invoice
            {
                Id = invoiceId++,
                InvoiceNumber = $"INV-{new Random().Next(100000, 999999)}",
                CustomerId = order.CustomerId,
                OrderId = order.Id,
                InvoiceDate = order.OrderDate.AddDays(new Random().Next(0, 3)),
                DueDate = order.OrderDate.AddDays(30),
                Status = DetermineInvoiceStatus(order),
                Subtotal = order.Subtotal,
                TaxAmount = order.TaxAmount,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                TermsAndConditions = "Payment due within 30 days. Late fees may apply.",
                CreatedAt = order.OrderDate.AddDays(new Random().Next(0, 2)),
                UpdatedAt = DateTime.UtcNow
            };

            // Create invoice items from order items
            foreach (var orderItem in order.OrderItems)
            {
                var invoiceItem = new InvoiceItem
                {
                    Id = invoiceItemId++,
                    InvoiceId = invoice.Id,
                    ProductId = orderItem.ProductId,
                    Description = orderItem.Product.Name,
                    Quantity = orderItem.Quantity,
                    UnitPrice = orderItem.UnitPrice,
                    LineTotal = orderItem.LineTotal
                };

                invoiceItems.Add(invoiceItem);
            }

            // Generate payments for some invoices
            if (invoice.Status == InvoiceStatus.Paid || invoice.Status == InvoiceStatus.PartiallyPaid)
            {
                if (invoice.Status == InvoiceStatus.Paid)
                {
                    // Full payment
                    var payment = new Payment
                    {
                        Id = paymentId++,
                        InvoiceId = invoice.Id,
                        Amount = invoice.TotalAmount,
                        PaymentDate = invoice.InvoiceDate.AddDays(new Random().Next(1, 20)),
                        PaymentMethod = invoice.PaymentMethod ?? "Credit Card",
                        ReferenceNumber = $"PAY-{new Random().Next(100000, 999999)}",
                        CreatedAt = DateTime.UtcNow
                    };

                    invoice.AmountPaid = payment.Amount;
                    invoice.PaidDate = payment.PaymentDate;
                    payments.Add(payment);
                }
                else
                {
                    // Partial payment
                    var partialAmount = Math.Round(invoice.TotalAmount * (decimal)new Random().NextDouble() * 0.7m + invoice.TotalAmount * 0.3m, 2);
                    var payment = new Payment
                    {
                        Id = paymentId++,
                        InvoiceId = invoice.Id,
                        Amount = partialAmount,
                        PaymentDate = invoice.InvoiceDate.AddDays(new Random().Next(1, 15)),
                        PaymentMethod = invoice.PaymentMethod ?? "Credit Card",
                        ReferenceNumber = $"PAY-{new Random().Next(100000, 999999)}",
                        Notes = "Partial payment - balance pending",
                        CreatedAt = DateTime.UtcNow
                    };

                    invoice.AmountPaid = payment.Amount;
                    payments.Add(payment);
                }
            }

            invoices.Add(invoice);
        }

        await _context.Invoices.AddRangeAsync(invoices);
        await _context.InvoiceItems.AddRangeAsync(invoiceItems);
        await _context.Payments.AddRangeAsync(payments);
        await _context.SaveChangesAsync();
    }

    private static InvoiceStatus DetermineInvoiceStatus(Order order)
    {
        var random = new Random();
        
        return order.Status switch
        {
            OrderStatus.Completed => random.Next(1, 5) switch
            {
                1 => InvoiceStatus.PartiallyPaid,
                2 or 3 => InvoiceStatus.Paid,
                _ => InvoiceStatus.Sent
            },
            OrderStatus.Delivered => random.Next(1, 4) switch
            {
                1 => InvoiceStatus.PartiallyPaid,
                2 => InvoiceStatus.Paid,
                _ => InvoiceStatus.Sent
            },
            OrderStatus.Shipped => random.Next(1, 6) switch
            {
                1 => InvoiceStatus.PartiallyPaid,
                _ => InvoiceStatus.Sent
            },
            OrderStatus.Cancelled => InvoiceStatus.Cancelled,
            _ => InvoiceStatus.Draft
        };
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        // Use pure Tuxedo ORM for high-performance dashboard queries
        return await _tuxedoContext.GetDashboardStatsAsync();
    }
}

public class DashboardStats
{
    public int TotalCustomers { get; set; }
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public int TotalInvoices { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int OverdueInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public int LowStockProducts { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public DateTime LastUpdated { get; set; }
}