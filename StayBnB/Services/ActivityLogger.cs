using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;

namespace StayBnB.Services;

/// <summary>Writes an audit trail entry for a key action.</summary>
public interface IActivityLogger
{
    Task LogAsync(string? userId, string action, ActivityType type, string? ipAddress = null);
}

public class ActivityLogger : IActivityLogger
{
    private readonly ApplicationDbContext _db;

    public ActivityLogger(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(string? userId, string action, ActivityType type, string? ipAddress = null)
    {
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId,
            Action = action,
            ActivityType = type,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}

/// <summary>Creates the notifications shown in the bell menu.</summary>
public interface INotificationService
{
    Task NotifyAsync(string userId, NotificationType type, string title, string? message = null, string? link = null);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db) => _db = db;

    public async Task NotifyAsync(string userId, NotificationType type, string title, string? message = null, string? link = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            LinkUrl = link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
