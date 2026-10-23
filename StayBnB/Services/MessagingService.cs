using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;

namespace StayBnB.Services;

/// <summary>
/// Sends database-backed (non real-time) messages. Each message is persisted,
/// audit-logged and turned into a notification for the receiver, so the messaging
/// rule is enforced in one place instead of being duplicated in controllers.
/// </summary>
public interface IMessagingService
{
    Task<Message> SendAsync(string senderId, string receiverId, string content, int? propertyId = null);
}

public class MessagingService : IMessagingService
{
    private readonly ApplicationDbContext _db;
    private readonly IActivityLogger _logger;
    private readonly INotificationService _notifications;

    public MessagingService(ApplicationDbContext db, IActivityLogger logger, INotificationService notifications)
    {
        _db = db;
        _logger = logger;
        _notifications = notifications;
    }

    public async Task<Message> SendAsync(string senderId, string receiverId, string content, int? propertyId = null)
    {
        if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(receiverId))
            throw new InvalidOperationException("A message needs both a sender and a receiver.");

        if (senderId == receiverId)
            throw new InvalidOperationException("A user cannot message themselves.");

        var trimmed = (content ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new InvalidOperationException("A message cannot be empty.");

        var receiver = await _db.Users.FirstOrDefaultAsync(u => u.Id == receiverId)
                       ?? throw new InvalidOperationException("The message receiver could not be found.");

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = trimmed.Length > 2000 ? trimmed[..2000] : trimmed,
            Timestamp = DateTime.UtcNow,
            IsRead = false,
            PropertyId = propertyId
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        await _logger.LogAsync(senderId, $"Sent a message to {receiver.FullName}", ActivityType.Messaging);
        await _notifications.NotifyAsync(receiverId, NotificationType.Message, "New message",
            "You have received a new message.", "/Messages/Inbox");

        return message;
    }
}
