using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;

namespace StayBnB.Services;

/// <summary>
/// Enforces the review rules on the server: only a guest who has actually stayed at a
/// property (a checked-in or completed booking) may review it, and each guest may review
/// a property only once. Keeping the rule here means a tampered form cannot bypass it.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Checks whether <paramref name="guestId"/> is allowed to review <paramref name="propertyId"/>.
    /// </summary>
    Task<(bool Allowed, string Message)> CanReviewAsync(string guestId, int propertyId);

    /// <summary>
    /// Stores a review once the eligibility rules pass, writing an activity-log entry.
    /// </summary>
    Task<(bool Success, string Message)> AddReviewAsync(string guestId, int propertyId, int rating, string comment);
}

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _db;
    private readonly IActivityLogger _logger;

    public ReviewService(ApplicationDbContext db, IActivityLogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(bool Allowed, string Message)> CanReviewAsync(string guestId, int propertyId)
    {
        var propertyExists = await _db.HostProperties.AnyAsync(p => p.Id == propertyId);
        if (!propertyExists)
            return (false, "That property could not be found.");

        if (await _db.Reviews.AnyAsync(r => r.ReviewerId == guestId && r.PropertyId == propertyId))
            return (false, "You have already reviewed this property.");

        var hasStay = await _db.Bookings.AnyAsync(b =>
            b.GuestId == guestId && b.PropertyId == propertyId &&
            (b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Completed));

        return hasStay
            ? (true, string.Empty)
            : (false, "Only guests who have stayed at a property can review it.");
    }

    public async Task<(bool Success, string Message)> AddReviewAsync(string guestId, int propertyId, int rating, string comment)
    {
        var (allowed, message) = await CanReviewAsync(guestId, propertyId);
        if (!allowed)
            return (false, message);

        if (rating is < 1 or > 5)
            return (false, "Please choose a rating between 1 and 5.");

        var trimmed = (comment ?? string.Empty).Trim();
        if (trimmed.Length < 10)
            return (false, "Please write at least 10 characters in your review.");

        var property = await _db.HostProperties.FirstAsync(p => p.Id == propertyId);

        _db.Reviews.Add(new Review
        {
            PropertyId = propertyId,
            ReviewerId = guestId,
            Rating = rating,
            Comment = trimmed.Length > 1000 ? trimmed[..1000] : trimmed,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        await _logger.LogAsync(guestId, $"Reviewed {property.Title}", ActivityType.Property);

        return (true, "Thanks for your review!");
    }
}
