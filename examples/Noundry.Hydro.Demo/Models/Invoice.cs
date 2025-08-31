using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Noundry.Hydro.Demo.Models;

[Table("Invoices")]
public class Invoice
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "Invoice Number")]
    public string InvoiceNumber { get; set; } = "";

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Order")]
    public int? OrderId { get; set; }

    [Column(TypeName = "datetime2")]
    [Display(Name = "Invoice Date")]
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; }

    [Required]
    [Display(Name = "Invoice Status")]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Subtotal")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Tax Amount")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Discount Amount")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Total Amount")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Amount Paid")]
    public decimal AmountPaid { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Invoice Notes")]
    public string? Notes { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Terms & Conditions")]
    public string? TermsAndConditions { get; set; }

    [Column(TypeName = "datetime2")]
    [Display(Name = "Paid Date")]
    public DateTime? PaidDate { get; set; }

    [MaxLength(100)]
    [Display(Name = "Payment Method")]
    public string? PaymentMethod { get; set; }

    [MaxLength(200)]
    [Display(Name = "Payment Reference")]
    public string? PaymentReference { get; set; }

    [Column(TypeName = "datetime2")]
    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    [Display(Name = "Updated At")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } = null!;
    
    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }
    
    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    // Computed properties
    [NotMapped]
    [Display(Name = "Amount Due")]
    public decimal AmountDue => TotalAmount - AmountPaid;

    [NotMapped]
    [Display(Name = "Is Overdue")]
    public bool IsOverdue => Status == InvoiceStatus.Sent && DueDate < DateTime.UtcNow;

    [NotMapped]
    [Display(Name = "Days Overdue")]
    public int DaysOverdue => IsOverdue ? (DateTime.UtcNow - DueDate).Days : 0;

    [NotMapped]
    [Display(Name = "Is Partially Paid")]
    public bool IsPartiallyPaid => AmountPaid > 0 && AmountPaid < TotalAmount;

    [NotMapped]
    [Display(Name = "Payment Progress")]
    public decimal PaymentProgress => TotalAmount > 0 ? (AmountPaid / TotalAmount) * 100 : 0;

    [NotMapped]
    [Display(Name = "Status Badge Class")]
    public string StatusBadgeClass => Status switch
    {
        InvoiceStatus.Draft => "bg-gray-100 text-gray-800",
        InvoiceStatus.Sent => "bg-blue-100 text-blue-800",
        InvoiceStatus.PartiallyPaid => "bg-yellow-100 text-yellow-800",
        InvoiceStatus.Paid => "bg-green-100 text-green-800",
        InvoiceStatus.Overdue => "bg-red-100 text-red-800",
        InvoiceStatus.Cancelled => "bg-red-100 text-red-800",
        _ => "bg-gray-100 text-gray-800"
    };
}

[Table("InvoiceItems")]
public class InvoiceItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Invoice")]
    public int InvoiceId { get; set; }

    [Required]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required, MaxLength(200)]
    [Display(Name = "Description")]
    public string Description { get; set; } = "";

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Line Total")]
    public decimal LineTotal { get; set; }

    // Navigation properties
    [ForeignKey("InvoiceId")]
    public virtual Invoice Invoice { get; set; } = null!;
    
    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;
}

[Table("Payments")]
public class Payment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Invoice")]
    public int InvoiceId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Amount")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Column(TypeName = "datetime2")]
    [Display(Name = "Payment Date")]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(100)]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "";

    [MaxLength(200)]
    [Display(Name = "Reference Number")]
    public string? ReferenceNumber { get; set; }

    [MaxLength(500)]
    [Display(Name = "Payment Notes")]
    public string? Notes { get; set; }

    [Column(TypeName = "datetime2")]
    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("InvoiceId")]
    public virtual Invoice Invoice { get; set; } = null!;
}

public enum InvoiceStatus
{
    [Display(Name = "Draft")]
    Draft = 0,
    
    [Display(Name = "Sent")]
    Sent = 1,
    
    [Display(Name = "Partially Paid")]
    PartiallyPaid = 2,
    
    [Display(Name = "Paid")]
    Paid = 3,
    
    [Display(Name = "Overdue")]
    Overdue = 4,
    
    [Display(Name = "Cancelled")]
    Cancelled = 5
}