using Microsoft.EntityFrameworkCore;
using Noundry.Hydro.Demo.Data;
using Noundry.Hydro.Demo.Models;

namespace Noundry.Hydro.Demo.Services;

public interface IProductService
{
    Task<List<Product>> GetProductsAsync(string? searchTerm = null, ProductCategory? category = null, int page = 1, int pageSize = 10);
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product> CreateProductAsync(Product product);
    Task<Product> UpdateProductAsync(Product product);
    Task<bool> DeleteProductAsync(int id);
    Task<int> GetProductCountAsync(string? searchTerm = null, ProductCategory? category = null);
    Task<List<Product>> GetFeaturedProductsAsync(int count = 6);
    Task<List<Product>> GetLowStockProductsAsync();
    Task<ProductStats> GetProductStatsAsync();
    Task<bool> UpdateStockAsync(int productId, int quantity);
}

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ApplicationDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Product>> GetProductsAsync(string? searchTerm = null, ProductCategory? category = null, int page = 1, int pageSize = 10)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => 
                p.Name.Contains(searchTerm) ||
                p.Description!.Contains(searchTerm) ||
                p.Sku.Contains(searchTerm));
        }

        if (category.HasValue)
        {
            query = query.Where(p => p.Category == category);
        }

        return await query
            .Where(p => p.IsActive)
            .Include(p => p.OrderItems)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.OrderItems)
            .ThenInclude(oi => oi.Order)
            .ThenInclude(o => o.Customer)
            .Include(p => p.InvoiceItems)
            .ThenInclude(ii => ii.Invoice)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        try
        {
            // Generate SKU if not provided
            if (string.IsNullOrWhiteSpace(product.Sku))
            {
                product.Sku = await GenerateSkuAsync(product.Name);
            }

            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new product: {ProductName} (SKU: {Sku})", 
                product.Name, product.Sku);

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {ProductName}", product.Name);
            throw;
        }
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        try
        {
            product.UpdatedAt = DateTime.UtcNow;
            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated product: {ProductName} ({Id})", 
                product.Name, product.Id);

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product: {ProductId}", product.Id);
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return false;

            // Check if product has order items or invoice items
            var hasOrderItems = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            var hasInvoiceItems = await _context.InvoiceItems.AnyAsync(ii => ii.ProductId == id);

            if (hasOrderItems || hasInvoiceItems)
            {
                // Soft delete by deactivating
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deactivated product with existing data: {ProductId}", id);
            }
            else
            {
                // Hard delete if no related data
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted product: {ProductId}", id);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product: {ProductId}", id);
            throw;
        }
    }

    public async Task<int> GetProductCountAsync(string? searchTerm = null, ProductCategory? category = null)
    {
        var query = _context.Products.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => 
                p.Name.Contains(searchTerm) ||
                p.Description!.Contains(searchTerm) ||
                p.Sku.Contains(searchTerm));
        }

        if (category.HasValue)
        {
            query = query.Where(p => p.Category == category);
        }

        return await query.CountAsync();
    }

    public async Task<List<Product>> GetFeaturedProductsAsync(int count = 6)
    {
        return await _context.Products
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive && p.StockQuantity <= p.MinimumStock)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();
    }

    public async Task<ProductStats> GetProductStatsAsync()
    {
        var totalProducts = await _context.Products.CountAsync(p => p.IsActive);
        var totalValue = await _context.Products
            .Where(p => p.IsActive)
            .SumAsync(p => p.Price * p.StockQuantity);

        var lowStockCount = await _context.Products
            .CountAsync(p => p.IsActive && p.StockQuantity <= p.MinimumStock);

        var outOfStockCount = await _context.Products
            .CountAsync(p => p.IsActive && p.StockQuantity == 0);

        var featuredCount = await _context.Products
            .CountAsync(p => p.IsActive && p.IsFeatured);

        var categoryCounts = await _context.Products
            .Where(p => p.IsActive)
            .GroupBy(p => p.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync();

        var topSellingProducts = await _context.Products
            .Where(p => p.IsActive)
            .Include(p => p.OrderItems)
            .OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity))
            .Take(5)
            .Select(p => new { p.Id, p.Name, TotalSold = p.OrderItems.Sum(oi => oi.Quantity) })
            .ToListAsync();

        return new ProductStats
        {
            TotalProducts = totalProducts,
            TotalInventoryValue = totalValue,
            LowStockCount = lowStockCount,
            OutOfStockCount = outOfStockCount,
            FeaturedCount = featuredCount,
            CategoryDistribution = categoryCounts.ToDictionary(cc => cc.Category.ToString(), cc => cc.Count),
            TopSellingProducts = topSellingProducts.ToDictionary(tsp => tsp.Name, tsp => tsp.TotalSold),
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<bool> UpdateStockAsync(int productId, int quantity)
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return false;

            product.StockQuantity = Math.Max(0, quantity);
            product.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated stock for product {ProductId}: {Quantity}", 
                productId, quantity);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for product: {ProductId}", productId);
            throw;
        }
    }

    private async Task<string> GenerateSkuAsync(string productName)
    {
        // Generate SKU from product name
        var prefix = string.Concat(productName.Split(' ').Take(2).Select(w => w.ToUpperInvariant().FirstOrDefault()));
        if (prefix.Length < 2)
            prefix = productName.Substring(0, Math.Min(3, productName.Length)).ToUpperInvariant();

        var counter = 1;
        string sku;
        
        do
        {
            sku = $"{prefix}-{counter:D3}";
            counter++;
        } 
        while (await _context.Products.AnyAsync(p => p.Sku == sku));

        return sku;
    }
}

public class ProductStats
{
    public int TotalProducts { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int FeaturedCount { get; set; }
    public Dictionary<string, int> CategoryDistribution { get; set; } = new();
    public Dictionary<string, int> TopSellingProducts { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}