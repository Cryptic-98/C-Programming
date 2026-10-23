using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace StayBnB.Models.Entities;

/// <summary>
/// ASP.NET Core Identity user extended with the StayBnB profile fields.
/// Matches the <c>AspNetUsers</c> table in <c>database.sql</c>.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Required, StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[\p{L}\s'\-]+$", ErrorMessage = "Letters, spaces, hyphens and apostrophes only.")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[\p{L}\s'\-]+$", ErrorMessage = "Letters, spaces, hyphens and apostrophes only.")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    // ----- Navigation -----
    public ICollection<HostProperty> Properties { get; set; } = new List<HostProperty>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    public ICollection<HostApplication> HostApplications { get; set; } = new List<HostApplication>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
}
