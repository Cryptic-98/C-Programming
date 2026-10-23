using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;
using StayBnB.Validation;

namespace StayBnB.Models.ViewModels;

// ---------------------------------------------------------------------------
// Bookings
// ---------------------------------------------------------------------------

public class BookingRequestViewModel
{
    [Required]
    public int PropertyId { get; set; }

    [Required, DataType(DataType.Date), FutureDate, Display(Name = "Check-in")]
    public DateTime? CheckInDate { get; set; }

    [Required, DataType(DataType.Date), DateAfter(nameof(CheckInDate)), Display(Name = "Check-out")]
    public DateTime? CheckOutDate { get; set; }

    [Required, Range(1, 50, ErrorMessage = "Enter between 1 and 50 travellers.")]
    public int? NumberOfGuests { get; set; }

    // Output only - always recalculated on the server, never trusted from the form.
    public decimal PricePerNight { get; set; }
    public int Nights { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal TotalPrice { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string PropertyCity { get; set; } = string.Empty;
    public string? PropertyImageUrl { get; set; }
    public string HostName { get; set; } = string.Empty;
}

public class BookingListItemViewModel
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string PropertyCity { get; set; } = string.Empty;
    public string? PropertyImageUrl { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string HostId { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Nights { get; set; }
    public bool HasCheckIn { get; set; }
    public int? GuestCheckInId { get; set; }
    public CheckInStatus? CheckInStatus { get; set; }
}

public class BookingDetailsViewModel
{
    public BookingListItemViewModel Booking { get; set; } = new();
    public string PropertyAddress { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal ServiceFee { get; set; }
    public List<BookingEventViewModel> Timeline { get; set; } = new();
}

public class BookingEventViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BookingStatus? Status { get; set; }
    public bool Done { get; set; }
}

// ---------------------------------------------------------------------------
// Host application
// ---------------------------------------------------------------------------

public class HostApplicationViewModel
{
    [Required, StringLength(100, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 30)]
    public string Description { get; set; } = string.Empty;

    [Required, Range(1, 100000), Display(Name = "Price per night")]
    public decimal? PricePerNight { get; set; }

    [Range(0, 10000), Display(Name = "Cleaning fee")]
    public decimal CleaningFee { get; set; }

    [Range(0, 10000), Display(Name = "Service fee")]
    public decimal ServiceFee { get; set; }

    [Required, StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [Required]
    public PropertyType? PropertyType { get; set; }

    [Required, Range(1, 50), Display(Name = "Maximum guests")]
    public int? MaxGuests { get; set; }

    [Range(0, 50), Display(Name = "Bedrooms")]
    public int Bedrooms { get; set; }

    public List<int> SelectedAmenityIds { get; set; } = new();

    [MinFiles(1, ErrorMessage = "At least one property image is required.")]
    [AllowedExtensions(".jpg", ".jpeg", ".png", ".webp")]
    [MaxFileSize(5)]
    public List<IFormFile> Images { get; set; } = new();

    [Range(0, 20)]
    public int PrimaryImageIndex { get; set; }

    // Only used by the host "add listing" flow.
    public bool SkipApplicationReview { get; set; }
}

public class HostApplicationStatusViewModel
{
    public int ApplicationId { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string PropertyCity { get; set; } = string.Empty;
    public string? PropertyImageUrl { get; set; }
    public int PropertyId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public List<PropertyImageViewModel> Images { get; set; } = new();
}

public class HostApplicationReviewViewModel
{
    public int Id { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string PropertyTitle { get; set; } = string.Empty;
    public string PropertyDescription { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal ServiceFee { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public List<PropertyImageViewModel> Images { get; set; } = new();
    public List<AmenityViewModel> Amenities { get; set; } = new();
    public int ImageCount { get; set; }
    public string ApplicantAvatarUrl { get; set; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Host management
// ---------------------------------------------------------------------------

public class HostPropertyEditViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 30)]
    public string Description { get; set; } = string.Empty;

    [Required, Range(1, 100000), Display(Name = "Price per night")]
    public decimal? PricePerNight { get; set; }

    [Range(0, 10000), Display(Name = "Cleaning fee")]
    public decimal CleaningFee { get; set; }

    [Range(0, 10000), Display(Name = "Service fee")]
    public decimal ServiceFee { get; set; }

    [Required, StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [Required]
    public PropertyType PropertyType { get; set; }

    [Required, Range(1, 50), Display(Name = "Maximum guests")]
    public int MaxGuests { get; set; }

    [Range(0, 50), Display(Name = "Bedrooms")]
    public int Bedrooms { get; set; }

    public List<int> SelectedAmenityIds { get; set; } = new();

    [AllowedExtensions(".jpg", ".jpeg", ".png", ".webp")]
    [MaxFileSize(5)]
    public List<IFormFile> NewImages { get; set; } = new();

    public List<int> RemoveImageIds { get; set; } = new();

    public List<PropertyImageViewModel> ExistingImages { get; set; } = new();
}

public class HostPropertyListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public decimal PricePerNight { get; set; }
    public bool IsActive { get; set; }
    public int ImageCount { get; set; }
    public string? ImageUrl { get; set; }
    public int BookingCount { get; set; }
    public int PendingBookingCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasCheckInProcess { get; set; }
}

public class HostDashboardViewModel
{
    public string HostName { get; set; } = string.Empty;
    public int ActiveListings { get; set; }
    public int TotalListings { get; set; }
    public int PendingBookings { get; set; }
    public int CheckInsToVerify { get; set; }
    public decimal Earnings { get; set; }
    public int UnreadMessages { get; set; }
    public int UnreadNotifications { get; set; }
    public List<HostPropertyListItemViewModel> Properties { get; set; } = new();
    public List<BookingListItemViewModel> RecentBookings { get; set; } = new();
}

public class HostBookingListItemViewModel
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string? PropertyImageUrl { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string GuestAvatarUrl { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; }
    public bool HasCheckInSubmission { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ---------------------------------------------------------------------------
// Check-in
// ---------------------------------------------------------------------------

public class CheckInStepViewModel
{
    [Required]
    public int GuestCheckInId { get; set; }

    [Range(1, 20)]
    public int StepOrder { get; set; }

    public bool Acknowledged { get; set; }
}

public class GuestDocumentUploadViewModel
{
    [Required]
    public int GuestCheckInId { get; set; }

    [Required, RegularExpression("ID|Passport", ErrorMessage = "Choose ID or Passport.")]
    public string DocumentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a file.")]
    [AllowedExtensions(".jpg", ".jpeg", ".png", ".pdf")]
    [MaxFileSize(5)]
    public IFormFile File { get; set; } = null!;
}

public class CheckInProcessViewModel
{
    public int PropertyId { get; set; }

    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required, MinLength(1, ErrorMessage = "Add at least one step.")]
    public List<CheckInStepDto> Steps { get; set; } = new();

    public string PropertyTitle { get; set; } = string.Empty;
}

public class DocumentVerificationViewModel
{
    [Required]
    public int DocumentId { get; set; }

    public bool Approve { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}

public class GuestCheckInViewModel
{
    public int GuestCheckInId { get; set; }
    public int BookingId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public string? PropertyImageUrl { get; set; }
    public string CheckInTitle { get; set; } = string.Empty;
    public CheckInStatus Status { get; set; }
    public int CurrentStep { get; set; }
    public List<CheckInStepDto> Steps { get; set; } = new();
    public List<GuestDocumentViewModel> Documents { get; set; } = new();
    public int TotalSteps { get; set; }
}

public class GuestDocumentViewModel
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class HostCheckInSubmissionViewModel
{
    public int GuestCheckInId { get; set; }
    public int BookingId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public CheckInStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<GuestDocumentViewModel> Documents { get; set; } = new();
    public List<CheckInStepDto> Steps { get; set; } = new();
}

// ---------------------------------------------------------------------------
// Admin
// ---------------------------------------------------------------------------

public class ApplicationReviewViewModel
{
    [Required]
    public int ApplicationId { get; set; }

    public bool Approve { get; set; }

    [StringLength(500, MinimumLength = 10, ErrorMessage = "Give a reason of at least 10 characters.")]
    public string? RejectionReason { get; set; }
}

public class AdminDashboardViewModel
{
    public int PendingApplications { get; set; }
    public int TotalApplications { get; set; }
    public int ActiveHosts { get; set; }
    public int TotalGuests { get; set; }
    public int TotalUsers { get; set; }
    public int BookingsToday { get; set; }
    public int PendingBookings { get; set; }
    public int CheckInsAwaitingVerification { get; set; }
    public List<HostApplicationReviewViewModel> RecentApplications { get; set; } = new();
    public List<ActivityLogViewModel> RecentActivity { get; set; } = new();
}

public class PromoteUserViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public string Confirm { get; set; } = string.Empty;
}

public class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int PropertyCount { get; set; }
}

public class ActivityLogViewModel
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public ActivityType ActivityType { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ActivityLogFilterViewModel
{
    public ActivityType? Type { get; set; }
    public string? UserSearch { get; set; }

    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [DataType(DataType.Date), DateAfter(nameof(From))]
    public DateTime? To { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    public IEnumerable<ActivityLogViewModel> Results { get; set; } = new List<ActivityLogViewModel>();
    public int TotalPages { get; set; }
    public int TotalResults { get; set; }
}

// ---------------------------------------------------------------------------
// Messaging and notifications
// ---------------------------------------------------------------------------

public class SendMessageViewModel
{
    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    public int? PropertyId { get; set; }
}

public class ConversationViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

public class MessageThreadViewModel
{
    public string OtherUserId { get; set; } = string.Empty;
    public string OtherDisplayName { get; set; } = string.Empty;
    public string? OtherAvatarUrl { get; set; }
    public List<MessageViewModel> Messages { get; set; } = new();
    public string? PropertyTitle { get; set; }
    public int? PropertyId { get; set; }
}

public class MessageViewModel
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsMine { get; set; }
}

public class NotificationListViewModel
{
    public List<NotificationItemViewModel> Items { get; set; } = new();
    public int UnreadCount { get; set; }
}

public class NotificationItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? LinkUrl { get; set; }
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ---------------------------------------------------------------------------
// Wishlist
// ---------------------------------------------------------------------------

public class WishlistViewModel
{
    public List<PropertyCardViewModel> Items { get; set; } = new();
}
