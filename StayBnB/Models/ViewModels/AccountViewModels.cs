using System.ComponentModel.DataAnnotations;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;

namespace StayBnB.Models.ViewModels;

// ---------------------------------------------------------------------------
// Account
// ---------------------------------------------------------------------------

public class RegisterViewModel
{
    [Required, StringLength(50, MinimumLength = 2), Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 2), Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Must contain upper, lower case and a digit.")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ProfileViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 2), Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 2), Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(500), Display(Name = "Profile picture URL")]
    public string? ProfilePictureUrl { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

// ---------------------------------------------------------------------------
// Property discovery
// ---------------------------------------------------------------------------

public class PropertyFilterViewModel
{
    [StringLength(100)]
    public string? City { get; set; }

    public PropertyType? PropertyType { get; set; }

    [Range(0, 100000)]
    public decimal? MinPrice { get; set; }

    [Range(0, 100000)]
    public decimal? MaxPrice { get; set; }

    [Range(1, 50)]
    public int? Guests { get; set; }

    [DataType(DataType.Date)]
    public DateTime? CheckIn { get; set; }

    [DataType(DataType.Date)]
    public DateTime? CheckOut { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    public IEnumerable<PropertyCardViewModel> Results { get; set; } = new List<PropertyCardViewModel>();

    public int TotalPages { get; set; }

    public int TotalResults { get; set; }
}

public class ExploreViewModel
{
    public int TotalStays { get; set; }
    public int TotalCities { get; set; }
    public List<ExploreCityViewModel> Cities { get; set; } = new();
    public List<ExploreTypeViewModel> Types { get; set; } = new();
    public List<PropertyCardViewModel> TopRated { get; set; } = new();
    public List<PropertyCardViewModel> Newest { get; set; } = new();
}

public class ExploreCityViewModel
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int Count { get; set; }
    public string? ImageUrl { get; set; }
    public decimal FromPrice { get; set; }
}

public class ExploreTypeViewModel
{
    public PropertyType PropertyType { get; set; }
    public int Count { get; set; }
    public string? ImageUrl { get; set; }
}

public class PropertyCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsWishlisted { get; set; }
    public PropertyType PropertyType { get; set; }
    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public bool IsActive { get; set; }
    public string HostName { get; set; } = string.Empty;
    public decimal CleaningFee { get; set; }
    public decimal ServiceFee { get; set; }
}

public class PropertyDetailsViewModel
{
    public HostPropertyDetail Property { get; set; } = new();

    public List<PropertyCardViewModel> Similar { get; set; } = new();

    public bool IsOwner { get; set; }

    public bool IsWishlisted { get; set; }

    public bool CanReview { get; set; }

    public BookingRequestViewModel Booking { get; set; } = new();
}

public class HostPropertyDetail
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal ServiceFee { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public bool IsActive { get; set; }
    public string HostId { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string HostAvatarUrl { get; set; } = string.Empty;
    public DateTime HostMemberSince { get; set; }
    public List<PropertyImageViewModel> Images { get; set; } = new();
    public List<AmenityViewModel> Amenities { get; set; } = new();
    public List<ReviewViewModel> Reviews { get; set; } = new();
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public CheckInInfo? CheckIn { get; set; }
}

public class PropertyImageViewModel
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class AmenityViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
}

public class ReviewViewModel
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string ReviewerAvatarUrl { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string StayPeriod { get; set; } = string.Empty;
}

public class CheckInInfo
{
    public string Title { get; set; } = string.Empty;
    public List<CheckInStepDto> Steps { get; set; } = new();
}
