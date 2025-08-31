using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Noundry.Hydro.Demo.Models;

[Table("Customers")]
public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, MaxLength(200)]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = "";

    [Phone, MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(20)]
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [Column(TypeName = "datetime2")]
    [Display(Name = "Date Joined")]
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    // Computed properties
    [NotMapped]
    [Display(Name = "Full Name")]
    public string FullName => $"{FirstName} {LastName}";

    [NotMapped]
    [Display(Name = "Total Orders")]
    public int TotalOrders => Orders?.Count ?? 0;

    [NotMapped]
    [Display(Name = "Total Spent")]
    public decimal TotalSpent => Orders?.Where(o => o.Status == OrderStatus.Completed)
        .Sum(o => o.TotalAmount) ?? 0;
}