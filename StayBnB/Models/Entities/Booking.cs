using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StayBnB.Models.Enums;

namespace StayBnB.Models.Entities;

/// <summary>A booking request. Maps to the <c>Bookings</c> table.</summary>
public class Booking
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    [Required]
    public string GuestId { get; set; } = string.Empty;
    public ApplicationUser Guest { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateTime CheckInDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime CheckOutDate { get; set; }

    [Range(1, 50)]
    public int NumberOfGuests { get; set; } = 1;

    /// <summary>Snapshot of the nightly rate at the time of booking.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerNight { get; set; }

    /// <summary>Snapshot of the property cleaning fee at the time of booking.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CleaningFee { get; set; }

    /// <summary>Snapshot of the service fee at the time of booking.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ServiceFee { get; set; }

    /// <summary>(PricePerNight x Nights) + CleaningFee + ServiceFee.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Payment? Payment { get; set; }
    public GuestCheckIn? GuestCheckIn { get; set; }

    [NotMapped]
    public int Nights => Math.Max(0, (CheckOutDate.Date - CheckInDate.Date).Days);

    [NotMapped]
    public decimal NightlySubtotal => PricePerNight * Nights;
}

/// <summary>Payment attached to a booking. Maps to the <c>Payments</c> table (1:1).</summary>
public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A guest review of a property. Maps to the <c>Reviews</c> table.</summary>
public class Review
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    [Required]
    public string ReviewerId { get; set; } = string.Empty;
    public ApplicationUser Reviewer { get; set; } = null!;

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required, StringLength(1000, MinimumLength = 10)]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A saved property. Maps to the <c>WishlistItems</c> table.</summary>
public class WishlistItem
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
