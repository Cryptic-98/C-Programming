using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;

namespace StayBnB.Services;

public interface IBookingService
{
    /// <summary>True when the property already has a booking that overlaps the requested window.</summary>
    Task<bool> HasOverlapAsync(int propertyId, DateTime from, DateTime to, int? ignoreBookingId = null);

    /// <summary>
    /// Moves a booking through the state machine. The transition is validated on the
    /// server so a tampered form cannot skip a step.
    /// </summary>
    Task<(bool Success, string Message)> ChangeStatusAsync(int bookingId, BookingStatus to, string actorId, string? reason = null);
}

public class BookingService : IBookingService
{
    private static readonly BookingStatus[] BlockingStatuses =
    {
        BookingStatus.Pending,
        BookingStatus.Approved,
        BookingStatus.CheckedIn
    };

    private readonly ApplicationDbContext _db;
    private readonly IActivityLogger _logger;
    private readonly INotificationService _notifications;
    private readonly ILogger<BookingService> _applicationLogger;

    public BookingService(
        ApplicationDbContext db,
        IActivityLogger logger,
        INotificationService notifications,
        ILogger<BookingService> applicationLogger)
    {
        _db = db;
        _logger = logger;
        _notifications = notifications;
        _applicationLogger = applicationLogger;
    }

    public async Task<bool> HasOverlapAsync(int propertyId, DateTime from, DateTime to, int? ignoreBookingId = null)
    {
        var query = _db.Bookings.Where(b => b.PropertyId == propertyId && BlockingStatuses.Contains(b.Status));

        if (ignoreBookingId.HasValue)
            query = query.Where(b => b.Id != ignoreBookingId.Value);

        // Two ranges overlap when each starts before the other ends.
        return await query.AnyAsync(b => b.CheckInDate < to && b.CheckOutDate > from);
    }

    public async Task<(bool Success, string Message)> ChangeStatusAsync(int bookingId, BookingStatus to, string actorId, string? reason = null)
    {
        var booking = await _db.Bookings
            .Include(b => b.Property)
            .Include(b => b.GuestCheckIn)
                .ThenInclude(c => c!.Documents)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return (false, "Booking not found.");

        var authorized = to switch
        {
            BookingStatus.Approved or BookingStatus.Rejected =>
                booking.Property.HostId == actorId,
            BookingStatus.Cancelled =>
                booking.GuestId == actorId,
            BookingStatus.CheckedIn =>
                booking.Property.HostId == actorId &&
                booking.GuestCheckIn is not null &&
                booking.GuestCheckIn.Status == CheckInStatus.Completed &&
                booking.GuestCheckIn.Documents.Count > 0 &&
                booking.GuestCheckIn.Documents.All(d => d.Status == DocumentStatus.Approved),
            BookingStatus.Completed =>
                booking.Property.HostId == actorId &&
                booking.CheckOutDate.Date <= DateTime.Today,
            _ => false
        };

        if (!authorized)
            return (false, "You are not permitted to make this booking status change.");

        if (!IsValidTransition(booking.Status, to))
            return (false, $"A {booking.Status} booking cannot become {to}.");

        var transaction = _db.Database.IsRelational() && _db.Database.CurrentTransaction is null
            ? await _db.Database.BeginTransactionAsync()
            : null;

        try
        {
            booking.Status = to;
            await _db.SaveChangesAsync();

            await _logger.LogAsync(
                actorId,
                string.IsNullOrWhiteSpace(reason)
                    ? $"Booking #{booking.Id} status changed to {to}"
                    : $"Booking #{booking.Id} status changed to {to}: {reason}",
                ActivityType.Booking);

            var recipientId = to == BookingStatus.Cancelled ? booking.Property.HostId : booking.GuestId;
            var notification = to switch
            {
                BookingStatus.Approved => (NotificationType.Booking, "Booking approved", $"Your booking of {booking.Property.Title} was approved."),
                BookingStatus.Rejected => (NotificationType.Booking, "Booking declined", string.IsNullOrWhiteSpace(reason)
                    ? $"Your booking of {booking.Property.Title} was declined."
                    : $"Your booking of {booking.Property.Title} was declined: {reason}"),
                BookingStatus.Cancelled => (NotificationType.Booking, "Booking cancelled", $"The booking for {booking.Property.Title} was cancelled."),
                _ => (NotificationType.System, $"Booking {to}", $"Your booking of {booking.Property.Title} is now {to}.")
            };

            var link = to == BookingStatus.Cancelled && actorId == booking.GuestId
                ? "/HostBookings/Index"
                : $"/Bookings/Details/{booking.Id}";

            await _notifications.NotifyAsync(
                recipientId,
                notification.Item1,
                notification.Item2,
                notification.Item3,
                link);

            if (transaction is not null)
                await transaction.CommitAsync();

            return (true, $"Booking {to}.");
        }
        catch (Exception ex)
        {
            if (transaction is not null)
                await transaction.RollbackAsync();

            _applicationLogger.LogError(ex, "Could not change booking {BookingId} status to {Status}.", bookingId, to);
            return (false, "The booking could not be updated. Please try again.");
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    /// <summary>
    /// The booking state machine. Approved bookings can still be cancelled by the guest,
    /// and only a host that verified the check-in documents can move a booking to CheckedIn.
    /// </summary>
    public static bool IsValidTransition(BookingStatus from, BookingStatus to) => (from, to) switch
    {
        (BookingStatus.Pending, BookingStatus.Approved) => true,
        (BookingStatus.Pending, BookingStatus.Rejected) => true,
        (BookingStatus.Pending, BookingStatus.Cancelled) => true,
        (BookingStatus.Approved, BookingStatus.CheckedIn) => true,
        (BookingStatus.Approved, BookingStatus.Cancelled) => true,
        (BookingStatus.CheckedIn, BookingStatus.Completed) => true,
        _ => false
    };
}
