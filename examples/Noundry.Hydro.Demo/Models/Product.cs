using System.ComponentModel.DataAnnotations;
using Tuxedo.Attributes;

namespace Noundry.Hydro.Demo.Models;

[Table("Products")]
public class Product
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = "";

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = "";

    [Display(Name = "Unit Price")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Display(Name = "Cost Price")]
    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    [Display(Name = "Stock Quantity")]
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Display(Name = "Minimum Stock Level")]
    [Range(0, int.MaxValue)]
    public int MinimumStock { get; set; } = 0;

    [Required]
    public ProductCategory Category { get; set; } = ProductCategory.General;

    [MaxLength(500)]
    [Display(Name = "Image URL")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Is Featured")]
    public bool IsFeatured { get; set; } = false;

    [Display(Name = "Created Date")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Updated Date")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed properties (populated by Tuxedo queries when needed)
    [Computed]
    [Display(Name = "Profit Margin")]
    public decimal ProfitMargin => Price > 0 ? ((Price - CostPrice) / Price) * 100 : 0;

    [Computed]
    [Display(Name = "Is Low Stock")]
    public bool IsLowStock => StockQuantity <= MinimumStock;

    [Computed]
    [Display(Name = "Stock Status")]
    public string StockStatus => StockQuantity <= 0 ? "Out of Stock" : 
        IsLowStock ? "Low Stock" : "In Stock";

    [Computed]
    [Display(Name = "Total Sold")]
    public int TotalSold { get; set; }
}

public enum ProductCategory
{
    [Display(Name = "General")]
    General = 0,
    
    [Display(Name = "Electronics")]
    Electronics = 1,
    
    [Display(Name = "Clothing")]
    Clothing = 2,
    
    [Display(Name = "Home & Garden")]
    HomeGarden = 3,
    
    [Display(Name = "Books")]
    Books = 4,
    
    [Display(Name = "Sports")]
    Sports = 5,
    
    [Display(Name = "Health & Beauty")]
    HealthBeauty = 6,
    
    [Display(Name = "Food & Beverages")]
    FoodBeverages = 7
}