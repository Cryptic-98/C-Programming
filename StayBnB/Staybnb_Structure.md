# Staybnb: Models, Controllers and Validation Design

Structure only (ASP.NET Core MVC + EF Core + SQL Server). Controller actions are signatures and responsibilities, not full implementations.

---

## 0. Design decisions and brief gaps filled

The brief's entity list is missing fields that the requirements depend on. These are added and flagged `// ADDED`.

| Gap in brief | Resolution |
|---|---|
| Booking price formula needs `CleaningFee`, `ServiceFee` | Fees stored on `HostProperty` (cleaning) and snapshotted on `Booking` so later price changes don't alter old bookings |
| "Filter by city or property type" | `City`, `PropertyType` added to `HostProperty` |
| "Toggle IsActive" | `IsActive` added to `HostProperty` |
| "Number of travelers" | `NumberOfGuests` added to `Booking`, `MaxGuests` to `HostProperty` |
| Read/Unread for messages and notifications (rubric: 10 marks) | `IsRead` added to `Message` and `Notification` |
| Notification has only a Title | `Message` and `LinkUrl` added |
| `GuestDocument` has no storage path or upload date | `FilePath`, `UploadedAt` added |
| `HostApplication` has no review data | `ReviewedById`, `ReviewedAt`, `RejectionReason` added |
| `RecieverId` typo in brief | Spelled `ReceiverId` in code |
| Brief says "StayHub" once | Treated as a typo for Staybnb |

**Role rules**
- Roles: `Guest`, `Host`, `Admin`, `SuperAdmin`.
- Registration always assigns `Guest`.
- `RoleSeeder` creates all four roles and one SuperAdmin user. The SuperAdmin is placed in both `SuperAdmin` and `Admin` so shared admin pages need only `Roles = "Admin,SuperAdmin"`.
- Guest and Host are mutually exclusive. Approval removes `Guest` and adds `Host` in one operation.

---

## 1. Project structure

```
Staybnb/
├── Controllers/
│   ├── AccountController.cs
│   ├── HomeController.cs
│   ├── PropertiesController.cs          (public discovery)
│   ├── WishlistController.cs            (Guest)
│   ├── BookingsController.cs            (Guest)
│   ├── GuestCheckInController.cs        (Guest)
│   ├── ReviewsController.cs             (Guest)
│   ├── HostApplicationController.cs     (Guest)
│   ├── Host/
│   │   ├── HostDashboardController.cs
│   │   ├── HostPropertiesController.cs
│   │   ├── HostBookingsController.cs
│   │   └── HostCheckInController.cs
│   ├── Admin/
│   │   ├── AdminDashboardController.cs
│   │   ├── ApplicationReviewController.cs
│   │   ├── UserManagementController.cs  (SuperAdmin)
│   │   └── ActivityLogController.cs
│   ├── MessagesController.cs
│   └── NotificationsController.cs
├── Models/
│   ├── Entities/
│   ├── Enums/
│   └── ViewModels/
├── Validation/                          (custom attributes)
├── Services/                            (logging, notifications, pricing, files, roles)
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── RoleSeeder.cs
│   └── Migrations/
└── Program.cs
```

---

## 2. Enums

```csharp
namespace Staybnb.Models.Enums;

public enum PropertyType      { Apartment, House, Villa, Cabin, Studio, Hotel }
public enum BookingStatus     { Pending, Approved, Rejected, Cancelled, CheckedIn, Completed }
public enum PaymentStatus     { Pending, Paid, Failed, Refunded }
public enum ApplicationStatus { Pending, Approved, Rejected }
public enum CheckInStatus     { InProgress, Submitted, Verified, Rejected }
public enum DocumentStatus    { Pending, Verified, Rejected }

public enum ActivityType
{
    Login, Logout, Registration, RoleChange,
    ApplicationReview, PropertyChange,
    BookingStatusChange, CheckIn, Message, Security
}

public enum NotificationType
{
    BookingRequested, BookingApproved, BookingRejected, BookingCancelled,
    ApplicationApproved, ApplicationRejected,
    CheckInSubmitted, CheckInVerified, CheckInRejected,
    NewMessage, System
}
```

**Booking state machine** (enforced in the controller/service, never from the client):

```
Pending ──host approve──► Approved ──host verifies check-in docs──► CheckedIn ──after check-out date──► Completed
   │                          │
   ├─host reject─► Rejected   └─guest cancel─► Cancelled
   └─guest cancel─► Cancelled
```

---

## 3. Entity models

### 3.1 ApplicationUser

```csharp
public class ApplicationUser : IdentityUser
{
    [Required, StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[\p{L}\s'\-]+$", ErrorMessage = "Letters, spaces, hyphens and apostrophes only.")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[\p{L}\s'\-]+$")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped] public string FullName => $"{FirstName} {LastName}";

    // Navigation
    public ICollection<HostProperty> Properties { get; set; } = new List<HostProperty>();   // 1:N (as host)
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();               // 1:N (as guest)
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    public ICollection<HostApplication> HostApplications { get; set; } = new List<HostApplication>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
}
```

### 3.2 HostProperty

```csharp
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

    [Range(0, 10000), Column(TypeName = "decimal(18,2)")]          // ADDED
    public decimal CleaningFee { get; set; }

    [Required, StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]                                   // ADDED (filter)
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]                                   // ADDED
    public string Country { get; set; } = string.Empty;

    public PropertyType PropertyType { get; set; }                  // ADDED (filter)

    [Range(1, 50)] public int MaxGuests { get; set; } = 1;         // ADDED
    [Range(0, 50)] public int Bedrooms { get; set; }               // ADDED

    public bool IsActive { get; set; }                              // ADDED (host toggle)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;      // ADDED

    [Required] public string HostId { get; set; } = string.Empty;
    public ApplicationUser Host { get; set; } = null!;

    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();      // M:N
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public CheckInProcess? CheckInProcess { get; set; }                              // 1:1
    public HostApplication? HostApplication { get; set; }
}
```

### 3.3 PropertyImage and Amenity

```csharp
public class PropertyImage
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    [Required, StringLength(500)] public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }                              // ADDED (gallery cover)
}

public class Amenity
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string Name { get; set; } = string.Empty;
    [StringLength(50)] public string? IconClass { get; set; }        // e.g. "bi bi-wifi"
    public ICollection<HostProperty> Properties { get; set; } = new List<HostProperty>();
}
```

### 3.4 Booking and Payment

```csharp
public class Booking
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    [Required] public string GuestId { get; set; } = string.Empty;
    public ApplicationUser Guest { get; set; } = null!;

    [DataType(DataType.Date)] public DateTime CheckInDate { get; set; }
    [DataType(DataType.Date)] public DateTime CheckOutDate { get; set; }

    [Range(1, 50)] public int NumberOfGuests { get; set; }           // ADDED

    [Column(TypeName = "decimal(18,2)")] public decimal CleaningFee { get; set; }   // ADDED snapshot
    [Column(TypeName = "decimal(18,2)")] public decimal ServiceFee  { get; set; }   // ADDED snapshot
    [Column(TypeName = "decimal(18,2)")] public decimal TotalPrice  { get; set; }   // (PPN × Nights) + Cleaning + Service

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;       // ADDED

    [NotMapped] public int Nights => (CheckOutDate.Date - CheckInDate.Date).Days;

    public Payment? Payment { get; set; }                            // 1:1
    public GuestCheckIn? GuestCheckIn { get; set; }                  // 1:1
}

public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### 3.5 Review and WishlistItem

```csharp
public class Review
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;
    [Required] public string ReviewerId { get; set; } = string.Empty;
    public ApplicationUser Reviewer { get; set; } = null!;

    [Range(1, 5)] public int Rating { get; set; }
    [Required, StringLength(1000, MinimumLength = 10)] public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class WishlistItem
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;       // unique index (UserId, PropertyId)
}
```

### 3.6 Messaging, notifications, logging

```csharp
public class Message
{
    public int Id { get; set; }
    [Required] public string SenderId { get; set; } = string.Empty;
    public ApplicationUser Sender { get; set; } = null!;
    [Required] public string ReceiverId { get; set; } = string.Empty;
    public ApplicationUser Receiver { get; set; } = null!;

    [Required, StringLength(2000, MinimumLength = 1)] public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }                                 // ADDED
    public int? PropertyId { get; set; }                             // ADDED optional context
}

public class Notification
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, StringLength(100)] public string Title { get; set; } = string.Empty;
    [StringLength(500)] public string? Message { get; set; }         // ADDED
    [StringLength(300)] public string? LinkUrl { get; set; }         // ADDED
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }                                 // ADDED
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ActivityLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }                              // nullable: failed logins, system events
    public ApplicationUser? User { get; set; }

    [Required, StringLength(100)] public string Action { get; set; } = string.Empty;
    public ActivityType ActivityType { get; set; }
    [StringLength(45)] public string? IpAddress { get; set; }        // ADDED
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### 3.7 Host application and check-in

```csharp
public class HostApplication
{
    public int Id { get; set; }
    [Required] public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;
    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public string? ReviewedById { get; set; }                        // ADDED
    public DateTime? ReviewedAt { get; set; }                        // ADDED
    [StringLength(500)] public string? RejectionReason { get; set; } // ADDED
}

public class CheckInProcess
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;

    // JSON list of steps, e.g. [{"order":1,"type":"Text","title":"House rules","instructions":"..."},
    //                           {"order":2,"type":"Document","title":"Upload passport","documentType":"Passport"}]
    [Required] public string StepsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<GuestCheckIn> GuestCheckIns { get; set; } = new List<GuestCheckIn>();
}

public class GuestCheckIn
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int CheckInProcessId { get; set; }
    public CheckInProcess CheckInProcess { get; set; } = null!;

    public CheckInStatus Status { get; set; } = CheckInStatus.InProgress;
    public int CurrentStep { get; set; } = 1;                        // ADDED (multi-step progress)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GuestDocument> Documents { get; set; } = new List<GuestDocument>();
}

public class GuestDocument
{
    public int Id { get; set; }
    public int GuestCheckInId { get; set; }
    public GuestCheckIn GuestCheckIn { get; set; } = null!;

    [Required, StringLength(100)] public string DocumentType { get; set; } = string.Empty;   // "ID", "Passport"
    [Required, StringLength(200)] public string FileName { get; set; } = string.Empty;       // stored (GUID) name
    [StringLength(260)] public string? OriginalFileName { get; set; }                         // ADDED
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;                               // ADDED
}
```

### 3.8 Step DTO for `StepsJson`

```csharp
public class CheckInStepDto
{
    [Range(1, 20)] public int Order { get; set; }
    [Required, RegularExpression("Text|Document|Acknowledge")] public string Type { get; set; } = "Text";
    [Required, StringLength(100)] public string Title { get; set; } = string.Empty;
    [StringLength(1000)] public string? Instructions { get; set; }
    [StringLength(100)] public string? DocumentType { get; set; }    // required when Type == "Document"
}
```

---

## 4. Relationships (Fluent API in `ApplicationDbContext`)

| Relationship | Configuration |
|---|---|
| User → HostProperty (1:N) | `HasOne(p => p.Host).WithMany(u => u.Properties).HasForeignKey(p => p.HostId).OnDelete(Restrict)` |
| HostProperty → Booking (1:N) | `HasMany(p => p.Bookings).WithOne(b => b.Property).OnDelete(Restrict)` |
| User → Booking (1:N, guest) | `HasOne(b => b.Guest).WithMany(u => u.Bookings).OnDelete(Restrict)` |
| HostProperty ↔ Amenity (M:N) | `HasMany(p => p.Amenities).WithMany(a => a.Properties)` (EF creates the join table) |
| Booking → GuestCheckIn (1:1) | `HasOne(b => b.GuestCheckIn).WithOne(c => c.Booking).HasForeignKey<GuestCheckIn>(c => c.BookingId)` |
| Booking → Payment (1:1) | `HasOne(b => b.Payment).WithOne(p => p.Booking).HasForeignKey<Payment>(p => p.BookingId)` |
| HostProperty → CheckInProcess (1:1) | `HasOne(p => p.CheckInProcess).WithOne(c => c.Property).HasForeignKey<CheckInProcess>(c => c.PropertyId)` |
| HostProperty → PropertyImage (1:N) | cascade delete |
| HostProperty → Review (1:N), User → Review (1:N) | reviewer FK `Restrict` |
| GuestCheckIn → GuestDocument (1:N) | cascade delete |
| Message | two FKs to `ApplicationUser` (`Sender`, `Receiver`), both `Restrict` to avoid multiple cascade paths |

Indexes and constraints:
- Unique `(UserId, PropertyId)` on `WishlistItem`.
- Unique `(ReviewerId, PropertyId)` on `Review` (one review per guest per property).
- Index on `(PropertyId, CheckInDate, CheckOutDate)` on `Booking` for overlap checks.
- Index on `(UserId, IsRead)` on `Notification` and `(ReceiverId, IsRead)` on `Message`.
- Check constraint `CheckOutDate > CheckInDate`.

---

## 5. Custom validation attributes (`/Validation`)

```csharp
// Date must be today or later
public class FutureDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
        => value is DateTime d && d.Date >= DateTime.Today
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? "Date cannot be in the past.");
}

// Date must be after another property (check-out > check-in)
public class DateAfterAttribute : ValidationAttribute
{
    public DateAfterAttribute(string otherProperty) { /* store name; compare via reflection */ }
}

// Minimum number of uploaded files (application needs ≥ 1 image)
public class MinFilesAttribute : ValidationAttribute
{
    public MinFilesAttribute(int min) { /* IsValid: value is IEnumerable<IFormFile> with Count >= min */ }
}

// Extension whitelist
public class AllowedExtensionsAttribute : ValidationAttribute
{
    public AllowedExtensionsAttribute(params string[] extensions) { /* e.g. ".jpg", ".jpeg", ".png", ".webp" */ }
}

// Max file size in MB, applied per file
public class MaxFileSizeAttribute : ValidationAttribute
{
    public MaxFileSizeAttribute(int megabytes) { /* IFormFile or IEnumerable<IFormFile> */ }
}
```

Server-side file checks beyond attributes: verify the file signature (magic bytes), not just the extension. Save with a generated GUID filename. Store documents outside `wwwroot` (private folder) and serve them only through an authorized action.

---

## 6. ViewModels with validation

### 6.1 Account

```csharp
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
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
```

### 6.2 Host application (property + images, submitted together)

```csharp
public class HostApplicationViewModel
{
    [Required, StringLength(100, MinimumLength = 5)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(4000, MinimumLength = 30)] public string Description { get; set; } = string.Empty;

    [Required, Range(1, 100000)] [Display(Name = "Price per night")] public decimal? PricePerNight { get; set; }
    [Range(0, 10000)] public decimal CleaningFee { get; set; }

    [Required, StringLength(250)] public string Address { get; set; } = string.Empty;
    [Required, StringLength(100)] public string City { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Country { get; set; } = string.Empty;

    [Required] public PropertyType? PropertyType { get; set; }
    [Required, Range(1, 50)] public int? MaxGuests { get; set; }
    [Range(0, 50)] public int Bedrooms { get; set; }

    public List<int> SelectedAmenityIds { get; set; } = new();

    [MinFiles(1, ErrorMessage = "At least one property image is required.")]
    [AllowedExtensions(".jpg", ".jpeg", ".png", ".webp")]
    [MaxFileSize(5)]
    public List<IFormFile> Images { get; set; } = new();

    [Range(0, 20)] public int PrimaryImageIndex { get; set; }
}
```

### 6.3 Properties and bookings

```csharp
public class PropertyFilterViewModel
{
    [StringLength(100)] public string? City { get; set; }
    public PropertyType? PropertyType { get; set; }
    [Range(0, 100000)] public decimal? MinPrice { get; set; }
    [Range(0, 100000)] public decimal? MaxPrice { get; set; }
    [Range(1, 50)] public int? Guests { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;

    public IEnumerable<PropertyCardViewModel> Results { get; set; } = new List<PropertyCardViewModel>();
    public int TotalPages { get; set; }
}

public class PropertyCardViewModel   // output only
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string City { get; set; } = "";
    public decimal PricePerNight { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public double AverageRating { get; set; }
    public bool IsWishlisted { get; set; }
}

public class BookingRequestViewModel
{
    [Required] public int PropertyId { get; set; }

    [Required, DataType(DataType.Date), FutureDate, Display(Name = "Check-in")]
    public DateTime? CheckInDate { get; set; }

    [Required, DataType(DataType.Date), DateAfter(nameof(CheckInDate)), Display(Name = "Check-out")]
    public DateTime? CheckOutDate { get; set; }

    [Required, Range(1, 50, ErrorMessage = "Enter between 1 and 50 travelers.")]
    public int? NumberOfGuests { get; set; }

    // Output only: recalculated on the server. Never trust values posted from the form.
    public decimal PricePerNight { get; set; }
    public int Nights { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal TotalPrice { get; set; }
}
```

Server-side booking checks (beyond annotations):
1. The property exists, `IsActive`, and its host is not the current user.
2. `NumberOfGuests <= MaxGuests`.
3. No overlap with Approved, CheckedIn or Pending bookings for the same property.
4. Maximum stay (e.g. 90 nights).
5. Recompute `TotalPrice = PricePerNight × Nights + CleaningFee + ServiceFee` in `IPricingService`. Service fee is a configured percentage, for example 12% of the nightly subtotal.

### 6.4 Check-in

```csharp
public class CheckInStepViewModel          // guest side, one step per request
{
    [Required] public int GuestCheckInId { get; set; }
    [Range(1, 20)] public int StepOrder { get; set; }
    public bool Acknowledged { get; set; }                    // for "Acknowledge" steps; validated when step requires it
}

public class GuestDocumentUploadViewModel
{
    [Required] public int GuestCheckInId { get; set; }

    [Required, RegularExpression("ID|Passport", ErrorMessage = "Choose ID or Passport.")]
    public string DocumentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a file.")]
    [AllowedExtensions(".jpg", ".jpeg", ".png", ".pdf")]
    [MaxFileSize(5)]
    public IFormFile File { get; set; } = null!;
}

public class CheckInProcessViewModel       // host configures rules and required documents
{
    public int PropertyId { get; set; }
    [Required, StringLength(200, MinimumLength = 3)] public string Title { get; set; } = string.Empty;
    [Required, MinLength(1, ErrorMessage = "Add at least one step.")]
    public List<CheckInStepDto> Steps { get; set; } = new();
}

public class DocumentVerificationViewModel
{
    [Required] public int DocumentId { get; set; }
    [Required] public bool Approve { get; set; }
    [StringLength(500)] public string? Reason { get; set; }   // required when Approve == false (checked in controller)
}
```

### 6.5 Host, admin, messaging, reviews

```csharp
public class HostPropertyEditViewModel   // same rules as HostApplicationViewModel; images optional here
{
    public int Id { get; set; }
    [Required, StringLength(100, MinimumLength = 5)] public string Title { get; set; } = "";
    [Required, StringLength(4000, MinimumLength = 30)] public string Description { get; set; } = "";
    [Required, Range(1, 100000)] public decimal? PricePerNight { get; set; }
    [Range(0, 10000)] public decimal CleaningFee { get; set; }
    [Required, StringLength(250)] public string Address { get; set; } = "";
    [Required, StringLength(100)] public string City { get; set; } = "";
    public PropertyType PropertyType { get; set; }
    [Range(1, 50)] public int MaxGuests { get; set; }
    public List<int> SelectedAmenityIds { get; set; } = new();
    [AllowedExtensions(".jpg", ".jpeg", ".png", ".webp"), MaxFileSize(5)]
    public List<IFormFile> NewImages { get; set; } = new();
    public List<int> RemoveImageIds { get; set; } = new();   // controller blocks removal of the last image
}

public class ApplicationReviewViewModel
{
    [Required] public int ApplicationId { get; set; }
    [Required] public bool Approve { get; set; }
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Give a reason of at least 10 characters.")]
    public string? RejectionReason { get; set; }   // enforced when Approve == false
}

public class PromoteUserViewModel
{
    [Required] public string UserId { get; set; } = string.Empty;
    [Required] public string Confirm { get; set; } = string.Empty;    // optional "type PROMOTE"
}

public class SendMessageViewModel
{
    [Required] public string ReceiverId { get; set; } = string.Empty;
    [Required, StringLength(2000, MinimumLength = 1)] public string Content { get; set; } = string.Empty;
    public int? PropertyId { get; set; }
}

public class ReviewViewModel
{
    [Required] public int PropertyId { get; set; }
    [Required, Range(1, 5)] public int? Rating { get; set; }
    [Required, StringLength(1000, MinimumLength = 10)] public string Comment { get; set; } = string.Empty;
}

public class ActivityLogFilterViewModel
{
    public ActivityType? Type { get; set; }
    public string? UserSearch { get; set; }
    [DataType(DataType.Date)] public DateTime? From { get; set; }
    [DataType(DataType.Date), DateAfter(nameof(From))] public DateTime? To { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
}
```

---

## 7. Services (keep controllers thin)

```csharp
public interface IActivityLogger        { Task LogAsync(string? userId, string action, ActivityType type); }
public interface INotificationService   { Task NotifyAsync(string userId, NotificationType type, string title, string? message = null, string? link = null); }
public interface IPricingService        { BookingPrice Calculate(HostProperty p, DateTime checkIn, DateTime checkOut); }
public interface IFileStorageService    { Task<string> SaveImageAsync(IFormFile f); Task<string> SavePrivateDocumentAsync(IFormFile f); Task<Stream> OpenPrivateAsync(string name); }
public interface IBookingService        { Task<bool> HasOverlapAsync(int propertyId, DateTime from, DateTime to); Task ChangeStatusAsync(int bookingId, BookingStatus to, string actorId); }
public interface IHostApplicationService{ Task ApproveAsync(int applicationId, string adminId); Task RejectAsync(int applicationId, string adminId, string reason); }
```

Registered in `Program.cs` as scoped services. `RoleSeeder.SeedAsync(IServiceProvider)` is called at startup and reads SuperAdmin credentials from configuration or user-secrets, never hard-coded.

---

## 8. Controllers

Conventions:
- All POST actions use `[HttpPost, ValidateAntiForgeryToken]`.
- Every POST begins with `if (!ModelState.IsValid) return View(vm);`.
- Ownership is checked on the server: a host can only touch their own properties, bookings and documents.
- Every state change calls `IActivityLogger`, and where relevant `INotificationService`.

### 8.1 AccountController (public)

```csharp
public class AccountController : Controller
{
    [HttpGet, AllowAnonymous]  public IActionResult Register();
    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public Task<IActionResult> Register(RegisterViewModel vm);
        // CreateAsync -> AddToRoleAsync("Guest") -> log Registration -> sign in

    [HttpGet, AllowAnonymous]  public IActionResult Login(string? returnUrl);
    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public Task<IActionResult> Login(LoginViewModel vm);
        // log Login (success) or Security (failed); use Url.IsLocalUrl(returnUrl); redirect by role:
        // Guest -> Properties/Index, Host -> HostDashboard, Admin/SuperAdmin -> AdminDashboard

    [HttpPost, Authorize, ValidateAntiForgeryToken] public Task<IActionResult> Logout();     // log Logout
    [HttpGet, Authorize]  public Task<IActionResult> Profile();
    [HttpPost, Authorize, ValidateAntiForgeryToken] public Task<IActionResult> Profile(ProfileViewModel vm);
    [AllowAnonymous] public IActionResult AccessDenied();
}
```

### 8.2 PropertiesController (public discovery)

```csharp
[AllowAnonymous]
public class PropertiesController : Controller
{
    public Task<IActionResult> Index(PropertyFilterViewModel filter);  // only IsActive == true; filter city/type/price/guests; paged
    public Task<IActionResult> Details(int id);                        // gallery, amenities, reviews, booking form (Guests only)
}
```

### 8.3 Guest-only controllers

```csharp
[Authorize(Roles = "Guest")]
public class HostApplicationController : Controller
{
    [HttpGet]  public Task<IActionResult> Create();                    // blocks if a Pending application exists
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(HostApplicationViewModel vm);
        // validate >=1 image -> in ONE transaction: save HostProperty (IsActive=false), PropertyImages,
        // Amenities, HostApplication(Pending) -> notify admins -> log. Roll back and delete files on failure.
    public Task<IActionResult> Status();                               // current/previous applications and rejection reasons
}

[Authorize(Roles = "Guest")]
public class BookingsController : Controller
{
    public Task<IActionResult> Index();                                // "My trips"
    [HttpGet]  public Task<IActionResult> Create(int propertyId, DateTime? checkIn, DateTime? checkOut);   // preview price
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(BookingRequestViewModel vm);     // overlap + capacity checks, recompute price, Pending + Payment(Pending), notify host
    public Task<IActionResult> Details(int id);                        // owner only
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Cancel(int id);   // only Pending/Approved, owner only
}

[Authorize(Roles = "Guest")]
public class GuestCheckInController : Controller
{
    public Task<IActionResult> Start(int bookingId);                   // booking must be Approved and belong to the user; creates GuestCheckIn
    [HttpGet]  public Task<IActionResult> Step(int guestCheckInId, int order);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Step(CheckInStepViewModel vm);   // cannot skip ahead of CurrentStep
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> UploadDocument(GuestDocumentUploadViewModel vm);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Submit(int guestCheckInId);       // all required documents present -> Submitted, notify host
}

[Authorize(Roles = "Guest")]
public class WishlistController : Controller
{
    public Task<IActionResult> Index();
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Toggle(int propertyId);   // add/remove, idempotent
}

[Authorize(Roles = "Guest")]
public class ReviewsController : Controller
{
    [HttpGet]  public Task<IActionResult> Create(int propertyId);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(ReviewViewModel vm);             // only if guest has a CheckedIn/Completed booking, one review per property
}
```

### 8.4 Host controllers (`Roles = "Host"`)

```csharp
[Authorize(Roles = "Host")]
public class HostDashboardController : Controller
{
    public Task<IActionResult> Index();    // counts: active listings, pending bookings, check-ins awaiting verification, unread items
}

[Authorize(Roles = "Host")]
public class HostPropertiesController : Controller
{
    public Task<IActionResult> Index();                                // own listings only
    [HttpGet]  public Task<IActionResult> Create();
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Create(HostApplicationViewModel vm);   // additional listings; >=1 image required
    [HttpGet]  public Task<IActionResult> Edit(int id);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Edit(HostPropertyEditViewModel vm);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> ToggleActive(int id);   // block activation if no images; log PropertyChange
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Delete(int id);         // block when future Approved bookings exist
}

[Authorize(Roles = "Host")]
public class HostBookingsController : Controller
{
    public Task<IActionResult> Index(BookingStatus? status);           // bookings on this host's properties
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Approve(int id);   // Pending -> Approved, notify guest, log
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Reject(int id, string? reason);   // Pending -> Rejected
}

[Authorize(Roles = "Host")]
public class HostCheckInController : Controller
{
    [HttpGet]  public Task<IActionResult> Configure(int propertyId);   // create/edit CheckInProcess (rules and required documents)
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Configure(CheckInProcessViewModel vm);   // serialize Steps -> StepsJson
    public Task<IActionResult> Submissions();                          // check-ins in Submitted status
    public Task<IActionResult> ViewDocument(int documentId);           // streams private file; owner-host only
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Verify(DocumentVerificationViewModel vm);
        // all docs Verified -> GuestCheckIn.Verified and Booking Approved -> CheckedIn; any rejected -> Rejected + notify guest
}
```

### 8.5 Admin controllers (`Roles = "Admin,SuperAdmin"`)

```csharp
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminDashboardController : Controller
{
    public Task<IActionResult> Index();    // pending applications, user counts, recent logs
}

[Authorize(Roles = "Admin,SuperAdmin")]
public class ApplicationReviewController : Controller
{
    public Task<IActionResult> Index(ApplicationStatus? status);
    public Task<IActionResult> Details(int id);                        // property, images, applicant
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Review(ApplicationReviewViewModel vm);
        // Approve -> IHostApplicationService.ApproveAsync (see 8.7); Reject -> reason required
}

[Authorize(Roles = "SuperAdmin")]                                      // strictly SuperAdmin
public class UserManagementController : Controller
{
    public Task<IActionResult> Index(string? search);                  // find users currently in role Guest
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> PromoteToAdmin(PromoteUserViewModel vm);
        // target must be Guest; add Admin, remove Guest; cannot target self/SuperAdmin; log RoleChange; notify user
}

[Authorize(Roles = "Admin,SuperAdmin")]
public class ActivityLogController : Controller
{
    public Task<IActionResult> Index(ActivityLogFilterViewModel filter);   // read-only, paged, no edit/delete actions exist
}
```

### 8.6 Shared (any authenticated role)

```csharp
[Authorize(Roles = "Guest,Host,Admin,SuperAdmin")]
public class MessagesController : Controller
{
    public Task<IActionResult> Inbox();                                // conversations, unread first
    public Task<IActionResult> Thread(string withUserId);              // marks incoming messages IsRead = true
    [HttpGet]  public Task<IActionResult> Compose(string receiverId, int? propertyId);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Send(SendMessageViewModel vm);
        // receiver must exist and differ from sender; creates NewMessage notification
}

[Authorize(Roles = "Guest,Host,Admin,SuperAdmin")]
public class NotificationsController : Controller
{
    public Task<IActionResult> Index();
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> MarkRead(int id);      // owner only
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> MarkAllRead();
    public Task<IActionResult> UnreadCount();                          // JSON for the bell badge
}
```

### 8.7 The key rubric logic: role swap on approval

```csharp
public async Task ApproveAsync(int applicationId, string adminId)
{
    // 1. Load application with Property and ApplicationUser; must be Pending.
    // 2. Begin DB transaction.
    // 3. application.Status = Approved; ReviewedById = adminId; ReviewedAt = UtcNow
    // 4. application.Property.IsActive = true
    // 5. await _userManager.AddToRoleAsync(user, "Host");       // 1) add Host
    // 6. await _userManager.RemoveFromRoleAsync(user, "Guest"); // 2) remove Guest
    //    -> if either IdentityResult.Succeeded is false: rollback and return error
    // 7. await _userManager.UpdateSecurityStampAsync(user);     // forces claims refresh so new role applies
    // 8. await _logger.LogAsync(adminId, $"Approved host application #{id}", ActivityType.ApplicationReview);
    //    await _logger.LogAsync(user.Id, "Role changed Guest -> Host", ActivityType.RoleChange);
    // 9. await _notifications.NotifyAsync(user.Id, NotificationType.ApplicationApproved, "You're now a host!", ...);
    // 10. Commit.
}
```

---

## 9. Access control matrix

| Area | Anonymous | Guest | Host | Admin | SuperAdmin |
|---|:-:|:-:|:-:|:-:|:-:|
| Browse / property details | ✔ | ✔ | ✔ | ✔ | ✔ |
| Become-a-host application | ✘ | ✔ | ✘ | ✘ | ✘ |
| Bookings, guest check-in, wishlist, reviews | ✘ | ✔ | ✘ | ✘ | ✘ |
| Host dashboard, listings, booking approvals, check-in config/verification | ✘ | ✘ | ✔ | ✘ | ✘ |
| Application review, activity log | ✘ | ✘ | ✘ | ✔ | ✔ |
| Promote Guest → Admin | ✘ | ✘ | ✘ | ✘ | ✔ |
| Messages, notifications | ✘ | ✔ | ✔ | ✔ | ✔ |

Because roles are exclusive, `[Authorize(Roles = "Guest")]` automatically locks hosts out of guest pages, and `[Authorize(Roles = "Host")]` locks guests out of host pages. `AccessDeniedPath` is configured in `Program.cs`.

---

## 10. Where each activity is logged

| Event | ActivityType |
|---|---|
| Register | Registration |
| Login success / failure | Login / Security |
| Logout | Logout |
| Application approved / rejected | ApplicationReview |
| Guest → Host, Guest → Admin | RoleChange |
| Booking created / approved / rejected / cancelled / CheckedIn / Completed | BookingStatusChange |
| Listing created / edited / activated / deactivated | PropertyChange |
| Document uploaded / verified / rejected | CheckIn |
| Message sent | Message |

---

## 11. Rubric mapping

| Rubric item | Covered by |
|---|---|
| Auth and role logic (20) | `AccountController`, `RoleSeeder`, `HostApplicationService.ApproveAsync` (add Host, remove Guest), role-based `[Authorize]` |
| Admin dashboard (15) | `ApplicationReviewController`, `UserManagementController` (SuperAdmin only), `ActivityLogController` |
| Host dashboard (15) | `HostPropertiesController`, `HostBookingsController`, `HostCheckInController` |
| Application process (15) | `HostApplicationViewModel` with `MinFiles(1)` and the transactional property + images + application save |
| Booking and check-in (15) | `BookingsController`, `GuestCheckInController`, booking state machine and `IPricingService` |
| Messaging and notifications (10) | `MessagesController`, `NotificationsController`, `IsRead` flags persisted in the DB |
| Data and architecture (10) | Fluent API relationships, indexes, migrations, services layer, `IActivityLogger` on every key action |
