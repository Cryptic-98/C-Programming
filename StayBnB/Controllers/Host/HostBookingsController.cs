using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;
using StayBnB.Models.ViewModels;
using StayBnB.Services;

namespace StayBnB.Controllers.Host;

[Authorize(Roles = RoleSeeder.HostRole)]
public class HostBookingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IBookingService _bookingService;
    private readonly IMessagingService _messaging;

    public HostBookingsController(ApplicationDbContext db, IBookingService bookingService, IMessagingService messaging)
    {
        _db = db;
        _bookingService = bookingService;
        _messaging = messaging;
    }

    public async Task<IActionResult> Index(BookingStatus? status)
    {
        var userId = CurrentUserId;

        var query = _db.Bookings
            .Include(b => b.Property).ThenInclude(p => p.Images)
            .Include(b => b.Property).ThenInclude(p => p.Host)
            .Include(b => b.Guest)
            .Include(b => b.GuestCheckIn)
            .Where(b => b.Property.HostId == userId);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

        var vm = bookings.Select(b => new HostBookingListItemViewModel
        {
            Id = b.Id,
            PropertyId = b.PropertyId,
            PropertyTitle = b.Property.Title,
            PropertyImageUrl = (b.Property.Images.FirstOrDefault(i => i.IsPrimary) ?? b.Property.Images.FirstOrDefault())?.ImageUrl,
            GuestName = b.Guest?.FullName ?? "Guest",
            GuestAvatarUrl = b.Guest?.ProfilePictureUrl ?? string.Empty,
            CheckInDate = b.CheckInDate,
            CheckOutDate = b.CheckOutDate,
            NumberOfGuests = b.NumberOfGuests,
            TotalPrice = b.TotalPrice,
            Status = b.Status,
            HasCheckInSubmission = b.GuestCheckIn?.Status == CheckInStatus.Completed,
            CreatedAt = b.CreatedAt
        }).ToList();

        ViewBag.SelectedStatus = status;
        ViewBag.Counts = Enum.GetValues<BookingStatus>().ToDictionary(s => s, s => bookings.Count(b => b.Status == s));

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var userId = CurrentUserId;

        var booking = await _db.Bookings
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b => b.Id == id && b.Property.HostId == userId);

        if (booking is null)
            return NotFound();

        var message = await _messaging.SendAsync(
            userId,
            booking.GuestId,
            $"Your booking for \"{booking.Property.Title}\" for {booking.CheckInDate:d MMM yyyy} – {booking.CheckOutDate:d MMM yyyy} has been approved. We look forward to welcoming you.",
            booking.PropertyId);

        var result = await _bookingService.ChangeStatusAsync(id, BookingStatus.Approved, userId);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
        }
        else
        {
            TempData["Toast"] = $"Booking #{id} approved.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var userId = CurrentUserId;

        var booking = await _db.Bookings
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b => b.Id == id && b.Property.HostId == userId);

        if (booking is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Please give the guest a reason for declining.";
            return RedirectToAction(nameof(Index));
        }

        var note = reason.Trim();

        // The host's note is sent to the guest as a real message in the conversation,
        // which also records an activity-log entry and a notification.
        await _messaging.SendAsync(
            userId,
            booking.GuestId,
            $"Your booking for \"{booking.Property.Title}\" was declined. Reason: {note}",
            booking.PropertyId);

        var result = await _bookingService.ChangeStatusAsync(id, BookingStatus.Rejected, userId, note);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
        }
        else
        {
            TempData["Toast"] = $"Booking #{id} declined.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await _bookingService.ChangeStatusAsync(id, BookingStatus.Completed, CurrentUserId);
        if (!result.Success)
            TempData["Error"] = result.Message;
        else
            TempData["Toast"] = $"Booking #{id} marked as completed.";

        return RedirectToAction(nameof(Index), new { status = BookingStatus.CheckedIn });
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
