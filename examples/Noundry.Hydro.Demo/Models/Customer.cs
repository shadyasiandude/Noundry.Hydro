using System.ComponentModel.DataAnnotations;
using Tuxedo.Attributes;

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

    [Display(Name = "Date Joined")]
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Computed properties (no navigation properties needed with Tuxedo)
    [Computed]
    [Display(Name = "Full Name")]
    public string FullName => $"{FirstName} {LastName}";

    // These will be populated by Tuxedo queries when needed
    [Computed]
    [Display(Name = "Total Orders")]
    public int TotalOrders { get; set; }

    [Computed]
    [Display(Name = "Total Spent")]
    public decimal TotalSpent { get; set; }
}