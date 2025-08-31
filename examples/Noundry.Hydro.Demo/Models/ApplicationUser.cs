using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Noundry.Hydro.Demo.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Display(Name = "Date Joined")]
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;

    [Display(Name = "Last Login")]
    public DateTime? LastLoginDate { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    [Display(Name = "Profile Picture URL")]
    public string? ProfilePictureUrl { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Bio")]
    public string? Bio { get; set; }

    // Navigation properties for customer relationship (if user is also a customer)
    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }
    
    public virtual Customer? Customer { get; set; }

    // Computed properties
    [Display(Name = "Full Name")]
    public string FullName => 
        !string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName) 
            ? $"{FirstName} {LastName}" 
            : Email ?? UserName ?? "Unknown User";

    [Display(Name = "Display Name")]
    public string DisplayName => 
        !string.IsNullOrEmpty(FirstName) 
            ? FirstName 
            : Email?.Split('@')[0] ?? UserName ?? "User";

    [Display(Name = "Member Since")]
    public string MemberSince => DateJoined.ToString("MMMM yyyy");

    [Display(Name = "Days Since Last Login")]
    public int? DaysSinceLastLogin => 
        LastLoginDate.HasValue 
            ? (DateTime.UtcNow - LastLoginDate.Value).Days 
            : null;
}