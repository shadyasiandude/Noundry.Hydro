using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Noundry.Hydro.Demo.Models;

[Table("Orders")]
public class Order
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "Order Number")]
    public string OrderNumber { get; set; } = "";

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Column(TypeName = "datetime2")]
    [Display(Name = "Order Date")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Required]
    [Display(Name = "Order Status")]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Subtotal")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Tax Amount")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Shipping Cost")]
    public decimal ShippingCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Discount Amount")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Total Amount")]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    [Display(Name = "Shipping Address")]
    public string? ShippingAddress { get; set; }

    [MaxLength(500)]
    [Display(Name = "Billing Address")]
    public string? BillingAddress { get; set; }

    [MaxLength(100)]
    [Display(Name = "Payment Method")]
    public string? PaymentMethod { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Order Notes")]
    public string? Notes { get; set; }

    [Column(TypeName = "datetime2")]
    [Display(Name = "Expected Delivery")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [Column(TypeName = "datetime2")]
    [Display(Name = "Actual Delivery")]
    public DateTime? ActualDeliveryDate { get; set; }

    [Column(TypeName = "datetime2")]
    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    [Display(Name = "Updated At")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } = null!;
    
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    // Computed properties
    [NotMapped]
    [Display(Name = "Item Count")]
    public int ItemCount => OrderItems?.Sum(oi => oi.Quantity) ?? 0;

    [NotMapped]
    [Display(Name = "Is Overdue")]
    public bool IsOverdue => Status == OrderStatus.Processing && 
        ExpectedDeliveryDate.HasValue && 
        ExpectedDeliveryDate < DateTime.UtcNow;

    [NotMapped]
    [Display(Name = "Days Since Order")]
    public int DaysSinceOrder => (DateTime.UtcNow - OrderDate).Days;

    [NotMapped]
    [Display(Name = "Status Badge Class")]
    public string StatusBadgeClass => Status switch
    {
        OrderStatus.Pending => "bg-yellow-100 text-yellow-800",
        OrderStatus.Processing => "bg-blue-100 text-blue-800",
        OrderStatus.Shipped => "bg-purple-100 text-purple-800",
        OrderStatus.Delivered => "bg-green-100 text-green-800",
        OrderStatus.Completed => "bg-green-100 text-green-800",
        OrderStatus.Cancelled => "bg-red-100 text-red-800",
        OrderStatus.Refunded => "bg-gray-100 text-gray-800",
        _ => "bg-gray-100 text-gray-800"
    };
}

[Table("OrderItems")]
public class OrderItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Order")]
    public int OrderId { get; set; }

    [Required]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Line Total")]
    public decimal LineTotal { get; set; }

    [MaxLength(500)]
    [Display(Name = "Item Notes")]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;
    
    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;
}

public enum OrderStatus
{
    [Display(Name = "Pending")]
    Pending = 0,
    
    [Display(Name = "Processing")]
    Processing = 1,
    
    [Display(Name = "Shipped")]
    Shipped = 2,
    
    [Display(Name = "Delivered")]
    Delivered = 3,
    
    [Display(Name = "Completed")]
    Completed = 4,
    
    [Display(Name = "Cancelled")]
    Cancelled = 5,
    
    [Display(Name = "Refunded")]
    Refunded = 6
}