using System.ComponentModel.DataAnnotations;
using StayBnB.Models.Enums;

namespace StayBnB.Models.Entities;

/// <summary>
/// A guest's request to become a host. Owns exactly one property, which is
/// created (inactive) at the same time the application is submitted.
/// Maps to the <c>HostApplications</c> table.
/// </summary>
public class HostApplication
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public string? ReviewedById { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }
}

/// <summary>
/// The host-configured check-in checklist for a property. The steps are stored as
/// a JSON array so a host can add, remove or reorder steps without a schema change.
/// Maps to the <c>CheckInProcesses</c> table (1:1 with a property).
/// </summary>
public class CheckInProcess
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>JSON list of <see cref="CheckInStepDto"/>.</summary>
    [Required]
    public string StepsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GuestCheckIn> GuestCheckIns { get; set; } = new List<GuestCheckIn>();
}

/// <summary>
/// One guest's progress through a property's check-in checklist.
/// Maps to the <c>GuestCheckIns</c> table (1:1 with a booking).
/// </summary>
public class GuestCheckIn
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int CheckInProcessId { get; set; }
    public CheckInProcess CheckInProcess { get; set; } = null!;

    public CheckInStatus Status { get; set; } = CheckInStatus.Pending;

    /// <summary>The step the guest is currently working on (1 based).</summary>
    public int CurrentStep { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GuestDocument> Documents { get; set; } = new List<GuestDocument>();
}

/// <summary>
/// An uploaded identity document belonging to a guest check-in.
/// Maps to the <c>GuestDocuments</c> table.
/// </summary>
public class GuestDocument
{
    public int Id { get; set; }

    public int GuestCheckInId { get; set; }
    public GuestCheckIn GuestCheckIn { get; set; } = null!;

    [Required, StringLength(100)]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>The generated file name the document is stored under.</summary>
    [Required, StringLength(200)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(260)]
    public string? OriginalFileName { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A single step of a check-in checklist. Persisted inside
/// <see cref="CheckInProcess.StepsJson"/>.
/// </summary>
public class CheckInStepDto
{
    [Range(1, 20)]
    public int Order { get; set; }

    [Required, RegularExpression("Text|Document|Acknowledge")]
    public string Type { get; set; } = "Text";

    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Instructions { get; set; }

    /// <summary>Required when <see cref="Type"/> is <c>Document</c> (for example "Passport").</summary>
    [StringLength(100)]
    public string? DocumentType { get; set; }
}
