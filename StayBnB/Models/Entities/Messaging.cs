using System.ComponentModel.DataAnnotations;
using StayBnB.Models.Enums;

namespace StayBnB.Models.Entities;

/// <summary>A direct message between two users. Maps to the <c>Messages</c> table.</summary>
public class Message
{
    public int Id { get; set; }

    [Required]
    public string SenderId { get; set; } = string.Empty;
    public ApplicationUser Sender { get; set; } = null!;

    [Required]
    public string ReceiverId { get; set; } = string.Empty;
    public ApplicationUser Receiver { get; set; } = null!;

    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Read / unread state, persisted so the bell and inbox badge survive a reload.</summary>
    public bool IsRead { get; set; }

    /// <summary>Optional context: the property the conversation is about.</summary>
    public int? PropertyId { get; set; }
}

/// <summary>A notification shown in the bell menu. Maps to the <c>Notifications</c> table.</summary>
public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Message { get; set; }

    [StringLength(300)]
    public string? LinkUrl { get; set; }

    public NotificationType Type { get; set; } = NotificationType.System;

    /// <summary>Read / unread state, persisted in the database.</summary>
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>An audit trail entry. Maps to the <c>ActivityLogs</c> table.</summary>
public class ActivityLog
{
    public int Id { get; set; }

    /// <summary>Nullable: failed logins and system events are not tied to a user.</summary>
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required, StringLength(100)]
    public string Action { get; set; } = string.Empty;

    public ActivityType ActivityType { get; set; } = ActivityType.System;

    [StringLength(45)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
