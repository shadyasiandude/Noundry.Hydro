using Tuxedo;
using Bowtie;
using Noundry.Hydro.Demo.Models;
using Guardian;

namespace Noundry.Hydro.Demo.Data;

/// <summary>
/// Tuxedo-based data context replacing Entity Framework
/// </summary>
public class TuxedoDataContext : ITuxedoContext
{
    private readonly ITuxedoConnection _connection;
    private readonly IGuardian _guard;
    private readonly ILogger<TuxedoDataContext> _logger;

    public TuxedoDataContext(
        ITuxedoConnection connection, 
        IGuardian guard,
        ILogger<TuxedoDataContext> logger)
    {
        _connection = connection;
        _guard = guard;
        _logger = logger;
    }

    // Customer operations
    public async Task<List<Customer>> GetCustomersAsync(string? searchTerm = null, int page = 1, int pageSize = 10)
    {
        var sql = @"
            SELECT * FROM Customers 
            WHERE (@SearchTerm IS NULL OR 
                   FirstName LIKE '%' + @SearchTerm + '%' OR 
                   LastName LIKE '%' + @SearchTerm + '%' OR 
                   Email LIKE '%' + @SearchTerm + '%')
            ORDER BY DateJoined DESC 
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        return (await _connection.QueryAsync<Customer>(sql, new 
        { 
            SearchTerm = searchTerm, 
            Offset = (page - 1) * pageSize, 
            PageSize = pageSize 
        })).ToList();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        _guard.Against.LessThanOrEqualTo(id, 0, nameof(id));

        var sql = "SELECT * FROM Customers WHERE Id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<Customer>(sql, new { Id = id });
    }

    public async Task<int> CreateCustomerAsync(Customer customer)
    {
        _guard.Against.Null(customer, nameof(customer));
        _guard.Against.NullOrWhiteSpace(customer.FirstName, nameof(customer.FirstName));
        _guard.Against.NullOrWhiteSpace(customer.LastName, nameof(customer.LastName));
        _guard.Against.NullOrWhiteSpace(customer.Email, nameof(customer.Email));

        var sql = @"
            INSERT INTO Customers (FirstName, LastName, Email, Phone, Address, City, PostalCode, Country, DateJoined, IsActive, Notes)
            VALUES (@FirstName, @LastName, @Email, @Phone, @Address, @City, @PostalCode, @Country, @DateJoined, @IsActive, @Notes);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        return await _connection.QuerySingleAsync<int>(sql, customer);
    }

    public async Task<int> UpdateCustomerAsync(Customer customer)
    {
        _guard.Against.Null(customer, nameof(customer));
        _guard.Against.LessThanOrEqualTo(customer.Id, 0, nameof(customer.Id));

        var sql = @"
            UPDATE Customers 
            SET FirstName = @FirstName, LastName = @LastName, Email = @Email, 
                Phone = @Phone, Address = @Address, City = @City, PostalCode = @PostalCode, 
                Country = @Country, IsActive = @IsActive, Notes = @Notes
            WHERE Id = @Id";

        return await _connection.ExecuteAsync(sql, customer);
    }

    public async Task<int> DeleteCustomerAsync(int id)
    {
        _guard.Against.LessThanOrEqualTo(id, 0, nameof(id));

        // Check for related records first
        var hasOrders = await _connection.QuerySingleAsync<bool>(
            "SELECT CASE WHEN EXISTS(SELECT 1 FROM Orders WHERE CustomerId = @Id) THEN 1 ELSE 0 END", 
            new { Id = id });

        if (hasOrders)
        {
            // Soft delete by deactivating
            var sql = "UPDATE Customers SET IsActive = 0 WHERE Id = @Id";
            return await _connection.ExecuteAsync(sql, new { Id = id });
        }
        else
        {
            // Hard delete if no related data
            var sql = "DELETE FROM Customers WHERE Id = @Id";
            return await _connection.ExecuteAsync(sql, new { Id = id });
        }
    }

    // Product operations
    public async Task<List<Product>> GetProductsAsync(string? searchTerm = null, ProductCategory? category = null, int page = 1, int pageSize = 10)
    {
        var sql = @"
            SELECT * FROM Products 
            WHERE IsActive = 1 
            AND (@SearchTerm IS NULL OR Name LIKE '%' + @SearchTerm + '%' OR Sku LIKE '%' + @SearchTerm + '%')
            AND (@Category IS NULL OR Category = @Category)
            ORDER BY CreatedAt DESC 
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        return (await _connection.QueryAsync<Product>(sql, new 
        { 
            SearchTerm = searchTerm, 
            Category = category,
            Offset = (page - 1) * pageSize, 
            PageSize = pageSize 
        })).ToList();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        _guard.Against.LessThanOrEqualTo(id, 0, nameof(id));

        var sql = "SELECT * FROM Products WHERE Id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task<int> CreateProductAsync(Product product)
    {
        _guard.Against.Null(product, nameof(product));
        _guard.Against.NullOrWhiteSpace(product.Name, nameof(product.Name));
        _guard.Against.NullOrWhiteSpace(product.Sku, nameof(product.Sku));

        var sql = @"
            INSERT INTO Products (Name, Description, Sku, Price, CostPrice, StockQuantity, MinimumStock, Category, ImageUrl, IsActive, IsFeatured, CreatedAt, UpdatedAt)
            VALUES (@Name, @Description, @Sku, @Price, @CostPrice, @StockQuantity, @MinimumStock, @Category, @ImageUrl, @IsActive, @IsFeatured, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        return await _connection.QuerySingleAsync<int>(sql, product);
    }

    // Order operations
    public async Task<List<Order>> GetOrdersAsync(string? searchTerm = null, OrderStatus? status = null, int page = 1, int pageSize = 10)
    {
        var sql = @"
            SELECT o.*, c.FirstName, c.LastName, c.Email 
            FROM Orders o
            INNER JOIN Customers c ON o.CustomerId = c.Id
            WHERE (@SearchTerm IS NULL OR o.OrderNumber LIKE '%' + @SearchTerm + '%' OR c.Email LIKE '%' + @SearchTerm + '%')
            AND (@Status IS NULL OR o.Status = @Status)
            ORDER BY o.OrderDate DESC 
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        return (await _connection.QueryAsync<Order, Customer, Order>(sql, 
            (order, customer) => 
            {
                order.Customer = customer;
                return order;
            },
            new { SearchTerm = searchTerm, Status = status, Offset = (page - 1) * pageSize, PageSize = pageSize },
            splitOn: "FirstName")).ToList();
    }

    // Invoice operations
    public async Task<List<Invoice>> GetInvoicesAsync(string? searchTerm = null, InvoiceStatus? status = null, int page = 1, int pageSize = 10)
    {
        var sql = @"
            SELECT i.*, c.FirstName, c.LastName, c.Email 
            FROM Invoices i
            INNER JOIN Customers c ON i.CustomerId = c.Id
            WHERE (@SearchTerm IS NULL OR i.InvoiceNumber LIKE '%' + @SearchTerm + '%' OR c.Email LIKE '%' + @SearchTerm + '%')
            AND (@Status IS NULL OR i.Status = @Status)
            ORDER BY i.InvoiceDate DESC 
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        return (await _connection.QueryAsync<Invoice, Customer, Invoice>(sql, 
            (invoice, customer) => 
            {
                invoice.Customer = customer;
                return invoice;
            },
            new { SearchTerm = searchTerm, Status = status, Offset = (page - 1) * pageSize, PageSize = pageSize },
            splitOn: "FirstName")).ToList();
    }

    // Dashboard statistics
    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var sql = @"
            SELECT 
                (SELECT COUNT(*) FROM Customers WHERE IsActive = 1) as TotalCustomers,
                (SELECT COUNT(*) FROM Products WHERE IsActive = 1) as TotalProducts,
                (SELECT COUNT(*) FROM Orders) as TotalOrders,
                (SELECT COUNT(*) FROM Invoices) as TotalInvoices,
                (SELECT ISNULL(SUM(TotalAmount), 0) FROM Orders WHERE Status = @CompletedStatus) as TotalRevenue,
                (SELECT COUNT(*) FROM Orders WHERE Status = @PendingStatus) as PendingOrders,
                (SELECT COUNT(*) FROM Orders WHERE Status = @ProcessingStatus) as ProcessingOrders,
                (SELECT COUNT(*) FROM Orders WHERE Status = @ShippedStatus) as ShippedOrders,
                (SELECT COUNT(*) FROM Invoices WHERE Status = @SentStatus AND DueDate < GETDATE()) as OverdueInvoices,
                (SELECT COUNT(*) FROM Invoices WHERE Status = @PaidStatus) as PaidInvoices,
                (SELECT COUNT(*) FROM Products WHERE IsActive = 1 AND StockQuantity <= MinimumStock) as LowStockProducts,
                (SELECT ISNULL(SUM(TotalAmount), 0) FROM Orders WHERE Status = @CompletedStatus AND OrderDate >= DATEADD(day, -30, GETDATE())) as MonthlyRevenue,
                (SELECT COUNT(*) FROM Customers WHERE DateJoined >= DATEADD(day, -30, GETDATE())) as NewCustomersThisMonth";

        var stats = await _connection.QuerySingleAsync<DashboardStats>(sql, new 
        {
            CompletedStatus = OrderStatus.Completed,
            PendingStatus = OrderStatus.Pending,
            ProcessingStatus = OrderStatus.Processing,
            ShippedStatus = OrderStatus.Shipped,
            SentStatus = InvoiceStatus.Sent,
            PaidStatus = InvoiceStatus.Paid
        });

        stats.LastUpdated = DateTime.UtcNow;
        return stats;
    }

    // Initialize database schema using Bowtie
    public async Task InitializeDatabaseAsync()
    {
        // Create tables using Bowtie migration system
        await CreateTablesAsync();
        await SeedInitialDataAsync();
    }

    private async Task CreateTablesAsync()
    {
        // Customers table
        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Customers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FirstName NVARCHAR(100) NOT NULL,
                LastName NVARCHAR(100) NOT NULL,
                Email NVARCHAR(200) NOT NULL UNIQUE,
                Phone NVARCHAR(20),
                Address NVARCHAR(500),
                City NVARCHAR(100),
                PostalCode NVARCHAR(20),
                Country NVARCHAR(100),
                DateJoined DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                IsActive BOOLEAN NOT NULL DEFAULT 1,
                Notes NVARCHAR(1000)
            )");

        // Products table
        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name NVARCHAR(200) NOT NULL,
                Description NVARCHAR(1000),
                Sku NVARCHAR(50) NOT NULL UNIQUE,
                Price DECIMAL(18,2) NOT NULL,
                CostPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                StockQuantity INTEGER NOT NULL DEFAULT 0,
                MinimumStock INTEGER NOT NULL DEFAULT 0,
                Category INTEGER NOT NULL DEFAULT 0,
                ImageUrl NVARCHAR(500),
                IsActive BOOLEAN NOT NULL DEFAULT 1,
                IsFeatured BOOLEAN NOT NULL DEFAULT 0,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )");

        // Orders table
        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
                CustomerId INTEGER NOT NULL,
                OrderDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                Status INTEGER NOT NULL DEFAULT 0,
                Subtotal DECIMAL(18,2) NOT NULL DEFAULT 0,
                TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                ShippingCost DECIMAL(18,2) NOT NULL DEFAULT 0,
                DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                ShippingAddress NVARCHAR(500),
                BillingAddress NVARCHAR(500),
                PaymentMethod NVARCHAR(100),
                Notes NVARCHAR(1000),
                ExpectedDeliveryDate DATETIME,
                ActualDeliveryDate DATETIME,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
            )");

        // OrderItems table
        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS OrderItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId INTEGER NOT NULL,
                ProductId INTEGER NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitPrice DECIMAL(18,2) NOT NULL,
                LineTotal DECIMAL(18,2) NOT NULL,
                Notes NVARCHAR(500),
                FOREIGN KEY (OrderId) REFERENCES Orders(Id),
                FOREIGN KEY (ProductId) REFERENCES Products(Id)
            )");

        // Invoices table
        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Invoices (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
                CustomerId INTEGER NOT NULL,
                OrderId INTEGER,
                InvoiceDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                DueDate DATETIME NOT NULL,
                Status INTEGER NOT NULL DEFAULT 0,
                Subtotal DECIMAL(18,2) NOT NULL DEFAULT 0,
                TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                AmountPaid DECIMAL(18,2) NOT NULL DEFAULT 0,
                Notes NVARCHAR(1000),
                TermsAndConditions NVARCHAR(1000),
                PaidDate DATETIME,
                PaymentMethod NVARCHAR(100),
                PaymentReference NVARCHAR(200),
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
                FOREIGN KEY (OrderId) REFERENCES Orders(Id)
            )");

        // InvoiceItems table
        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS InvoiceItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                ProductId INTEGER NOT NULL,
                Description NVARCHAR(200) NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitPrice DECIMAL(18,2) NOT NULL,
                LineTotal DECIMAL(18,2) NOT NULL,
                FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id),
                FOREIGN KEY (ProductId) REFERENCES Products(Id)
            )");

        // Payments table
        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Payments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                Amount DECIMAL(18,2) NOT NULL,
                PaymentDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PaymentMethod NVARCHAR(100) NOT NULL,
                ReferenceNumber NVARCHAR(200),
                Notes NVARCHAR(500),
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id)
            )");

        // Identity tables (simplified for Tuxedo)
        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS AspNetUsers (
                Id NVARCHAR(450) PRIMARY KEY,
                UserName NVARCHAR(256),
                NormalizedUserName NVARCHAR(256),
                Email NVARCHAR(256),
                NormalizedEmail NVARCHAR(256),
                EmailConfirmed BOOLEAN NOT NULL DEFAULT 0,
                PasswordHash NVARCHAR(MAX),
                SecurityStamp NVARCHAR(MAX),
                ConcurrencyStamp NVARCHAR(MAX),
                PhoneNumber NVARCHAR(MAX),
                PhoneNumberConfirmed BOOLEAN NOT NULL DEFAULT 0,
                TwoFactorEnabled BOOLEAN NOT NULL DEFAULT 0,
                LockoutEnd DATETIME,
                LockoutEnabled BOOLEAN NOT NULL DEFAULT 0,
                AccessFailedCount INTEGER NOT NULL DEFAULT 0,
                FirstName NVARCHAR(100),
                LastName NVARCHAR(100),
                DateJoined DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                LastLoginDate DATETIME,
                IsActive BOOLEAN NOT NULL DEFAULT 1,
                ProfilePictureUrl NVARCHAR(500),
                Bio NVARCHAR(1000),
                CustomerId INTEGER,
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
            )");

        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS AspNetRoles (
                Id NVARCHAR(450) PRIMARY KEY,
                Name NVARCHAR(256),
                NormalizedName NVARCHAR(256),
                ConcurrencyStamp NVARCHAR(MAX)
            )");

        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS AspNetUserRoles (
                UserId NVARCHAR(450) NOT NULL,
                RoleId NVARCHAR(450) NOT NULL,
                PRIMARY KEY (UserId, RoleId),
                FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id),
                FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id)
            )");
    }

    private async Task SeedInitialDataAsync()
    {
        // Check if data already exists
        var customerCount = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM Customers");
        if (customerCount > 0) return;

        // Seed initial customers using Tuxedo
        var customers = new[]
        {
            new Customer { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Phone = "+1-555-0123", City = "New York", Country = "USA", DateJoined = DateTime.UtcNow.AddMonths(-6) },
            new Customer { FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", Phone = "+1-555-0124", City = "Los Angeles", Country = "USA", DateJoined = DateTime.UtcNow.AddMonths(-4) },
            new Customer { FirstName = "Bob", LastName = "Johnson", Email = "bob.johnson@example.com", Phone = "+1-555-0125", City = "Chicago", Country = "USA", DateJoined = DateTime.UtcNow.AddMonths(-2) }
        };

        foreach (var customer in customers)
        {
            await CreateCustomerAsync(customer);
        }

        // Seed initial products
        var products = new[]
        {
            new Product { Name = "Wireless Headphones", Description = "High-quality wireless headphones", Sku = "WH-001", Price = 199.99m, CostPrice = 120.00m, StockQuantity = 50, Category = ProductCategory.Electronics, IsFeatured = true },
            new Product { Name = "Cotton T-Shirt", Description = "Comfortable 100% cotton t-shirt", Sku = "TS-001", Price = 24.99m, CostPrice = 12.00m, StockQuantity = 100, Category = ProductCategory.Clothing },
            new Product { Name = "Coffee Maker", Description = "Automatic drip coffee maker", Sku = "CM-001", Price = 89.99m, CostPrice = 55.00m, StockQuantity = 25, Category = ProductCategory.HomeGarden, IsFeatured = true }
        };

        foreach (var product in products)
        {
            await CreateProductAsync(product);
        }

        _logger.LogInformation("Database initialized with Tuxedo ORM and sample data seeded");
    }

    // Generic Tuxedo operations
    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        _guard.Against.NullOrWhiteSpace(sql, nameof(sql));
        return await _connection.QueryAsync<T>(sql, parameters);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null)
    {
        _guard.Against.NullOrWhiteSpace(sql, nameof(sql));
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        _guard.Against.NullOrWhiteSpace(sql, nameof(sql));
        return await _connection.ExecuteAsync(sql, parameters);
    }

    public async Task<bool> ValidateAsync<T>(T entity)
    {
        // Use Guardian for entity validation
        _guard.Against.Null(entity, nameof(entity));
        return true; // Simplified for demo
    }
}