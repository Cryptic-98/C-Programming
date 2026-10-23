using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StayBnB.Models.Enums;

namespace StayBnB.Models.Entities;

/// <summary>A property listed by a host. Maps to the <c>HostProperties</c> table.</summary>
public class HostProperty
{
    public int Id { get; set; }

    [Required, StringLength(100, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 30)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 100000), Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Price per night")]
    public decimal PricePerNight { get; set; }

    [Range(0, 10000), Column(TypeName = "decimal(18,2)")]
    public decimal CleaningFee { get; set; }

    [Range(0, 10000), Column(TypeName = "decimal(18,2)")]
    public decimal ServiceFee { get; set; }

    [Required, StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = string.Empty;

    public PropertyType PropertyType { get; set; } = PropertyType.Apartment;

    [Range(1, 50)]
    public int MaxGuests { get; set; } = 1;

    [Range(0, 50)]
    public int Bedrooms { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string HostId { get; set; } = string.Empty;

    public ApplicationUser Host { get; set; } = null!;

    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public HostApplication? HostApplication { get; set; }
    public CheckInProcess? CheckInProcess { get; set; }

    [NotMapped]
    public string DisplayLocation => string.IsNullOrWhiteSpace(Country) ? City : $"{City}, {Country}";
}

/// <summary>An image belonging to a property. Maps to the <c>PropertyImages</c> table.</summary>
public class PropertyImage
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    [Required, StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Gallery cover (gallery first image).</summary>
    public bool IsPrimary { get; set; }
}

/// <summary>A feature offered by a property. Maps to the <c>Amenities</c> table.</summary>
public class Amenity
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? IconClass { get; set; }

    public ICollection<HostProperty> Properties { get; set; } = new List<HostProperty>();
}
