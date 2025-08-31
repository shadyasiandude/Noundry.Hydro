using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Noundry.Hydro.Demo.Models;

namespace Noundry.Hydro.Demo.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for our domain entities
    public DbSet<Customer> Customers { get; set; } = default!;
    public DbSet<Product> Products { get; set; } = default!;
    public DbSet<Order> Orders { get; set; } = default!;
    public DbSet<OrderItem> OrderItems { get; set; } = default!;
    public DbSet<Invoice> Invoices { get; set; } = default!;
    public DbSet<InvoiceItem> InvoiceItems { get; set; } = default!;
    public DbSet<Payment> Payments { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure entity relationships and constraints
        ConfigureCustomer(builder);
        ConfigureProduct(builder);
        ConfigureOrder(builder);
        ConfigureInvoice(builder);
        
        // Seed initial data
        SeedData(builder);
    }

    private void ConfigureCustomer(ModelBuilder builder)
    {
        builder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_Customers_Email");

            entity.Property(e => e.FirstName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.LastName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Email)
                  .IsRequired()
                  .HasMaxLength(200);

            // Configure relationships
            entity.HasMany(c => c.Orders)
                  .WithOne(o => o.Customer)
                  .HasForeignKey(o => o.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.Invoices)
                  .WithOne(i => i.Customer)
                  .HasForeignKey(i => i.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureProduct(ModelBuilder builder)
    {
        builder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.Sku)
                  .IsUnique()
                  .HasDatabaseName("IX_Products_Sku");

            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Sku)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(e => e.Price)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.CostPrice)
                  .HasColumnType("decimal(18,2)");
        });
    }

    private void ConfigureOrder(ModelBuilder builder)
    {
        builder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.OrderNumber)
                  .IsUnique()
                  .HasDatabaseName("IX_Orders_OrderNumber");

            entity.Property(e => e.OrderNumber)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(e => e.Subtotal)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TaxAmount)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.ShippingCost)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.DiscountAmount)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TotalAmount)
                  .HasColumnType("decimal(18,2)");

            // Configure relationships
            entity.HasMany(o => o.OrderItems)
                  .WithOne(oi => oi.Order)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(o => o.Invoices)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.UnitPrice)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.LineTotal)
                  .HasColumnType("decimal(18,2)");

            // Configure relationship with Product
            entity.HasOne(oi => oi.Product)
                  .WithMany(p => p.OrderItems)
                  .HasForeignKey(oi => oi.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureInvoice(ModelBuilder builder)
    {
        builder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(e => e.InvoiceNumber)
                  .IsUnique()
                  .HasDatabaseName("IX_Invoices_InvoiceNumber");

            entity.Property(e => e.InvoiceNumber)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(e => e.Subtotal)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TaxAmount)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.DiscountAmount)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TotalAmount)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.AmountPaid)
                  .HasColumnType("decimal(18,2)");

            // Configure relationships
            entity.HasMany(i => i.InvoiceItems)
                  .WithOne(ii => ii.Invoice)
                  .HasForeignKey(ii => ii.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(i => i.Payments)
                  .WithOne(p => p.Invoice)
                  .HasForeignKey(p => p.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InvoiceItem>(entity =>
        {
            entity.Property(e => e.Description)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.UnitPrice)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.LineTotal)
                  .HasColumnType("decimal(18,2)");

            // Configure relationship with Product
            entity.HasOne(ii => ii.Product)
                  .WithMany(p => p.InvoiceItems)
                  .HasForeignKey(ii => ii.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.Property(e => e.Amount)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.PaymentMethod)
                  .IsRequired()
                  .HasMaxLength(100);
        });
    }

    private void SeedData(ModelBuilder builder)
    {
        // Seed sample customers
        builder.Entity<Customer>().HasData(
            new Customer
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "+1-555-0123",
                Address = "123 Main St",
                City = "New York",
                PostalCode = "10001",
                Country = "USA",
                DateJoined = DateTime.UtcNow.AddMonths(-6)
            },
            new Customer
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Phone = "+1-555-0124",
                Address = "456 Oak Ave",
                City = "Los Angeles",
                PostalCode = "90001",
                Country = "USA",
                DateJoined = DateTime.UtcNow.AddMonths(-4)
            },
            new Customer
            {
                Id = 3,
                FirstName = "Bob",
                LastName = "Johnson",
                Email = "bob.johnson@example.com",
                Phone = "+1-555-0125",
                Address = "789 Pine Rd",
                City = "Chicago",
                PostalCode = "60601",
                Country = "USA",
                DateJoined = DateTime.UtcNow.AddMonths(-2)
            }
        );

        // Seed sample products
        builder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Wireless Headphones",
                Description = "High-quality wireless headphones with noise cancellation",
                Sku = "WH-001",
                Price = 199.99m,
                CostPrice = 120.00m,
                StockQuantity = 50,
                MinimumStock = 10,
                Category = ProductCategory.Electronics,
                ImageUrl = "https://via.placeholder.com/300x300/007bff/ffffff?text=Headphones",
                IsFeatured = true
            },
            new Product
            {
                Id = 2,
                Name = "Cotton T-Shirt",
                Description = "Comfortable 100% cotton t-shirt in various colors",
                Sku = "TS-001",
                Price = 24.99m,
                CostPrice = 12.00m,
                StockQuantity = 100,
                MinimumStock = 20,
                Category = ProductCategory.Clothing,
                ImageUrl = "https://via.placeholder.com/300x300/28a745/ffffff?text=T-Shirt"
            },
            new Product
            {
                Id = 3,
                Name = "Coffee Maker",
                Description = "Automatic drip coffee maker with programmable timer",
                Sku = "CM-001",
                Price = 89.99m,
                CostPrice = 55.00m,
                StockQuantity = 25,
                MinimumStock = 5,
                Category = ProductCategory.HomeGarden,
                ImageUrl = "https://via.placeholder.com/300x300/ffc107/ffffff?text=Coffee",
                IsFeatured = true
            },
            new Product
            {
                Id = 4,
                Name = "Running Shoes",
                Description = "Lightweight running shoes with advanced cushioning",
                Sku = "RS-001",
                Price = 129.99m,
                CostPrice = 75.00m,
                StockQuantity = 30,
                MinimumStock = 8,
                Category = ProductCategory.Sports,
                ImageUrl = "https://via.placeholder.com/300x300/dc3545/ffffff?text=Shoes"
            },
            new Product
            {
                Id = 5,
                Name = "Smartphone Case",
                Description = "Protective case for smartphones with shock absorption",
                Sku = "SC-001",
                Price = 19.99m,
                CostPrice = 8.00m,
                StockQuantity = 75,
                MinimumStock = 15,
                Category = ProductCategory.Electronics,
                ImageUrl = "https://via.placeholder.com/300x300/17a2b8/ffffff?text=Case"
            }
        );
    }
}