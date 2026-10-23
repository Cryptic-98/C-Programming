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
public class HostDashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public HostDashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;

        var properties = await _db.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Bookings)
            .Where(p => p.HostId == userId)
            .ToListAsync();

        var propertyIds = properties.Select(p => p.Id).ToList();

        var bookings = await _db.Bookings
            .Include(b => b.Property).ThenInclude(p => p.Images)
            .Include(b => b.Property).ThenInclude(p => p.Host)
            .Where(b => propertyIds.Contains(b.PropertyId))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var checkInsToVerify = await _db.GuestCheckIns
            .Include(c => c.Booking)
            .Where(c => propertyIds.Contains(c.Booking.PropertyId) && c.Status == CheckInStatus.Completed)
            .CountAsync();
        var vm = new HostDashboardViewModel
        {
            HostName = (await _db.Users.FindAsync(userId))?.FirstName ?? "Host",
            TotalListings = properties.Count,
            ActiveListings = properties.Count(p => p.IsActive),
            PendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending),
            CheckInsToVerify = checkInsToVerify,
            Earnings = bookings.Where(b => b.Status is BookingStatus.CheckedIn or BookingStatus.Completed).Sum(b => b.TotalPrice),
            UnreadMessages = await _db.Messages.CountAsync(m => m.ReceiverId == userId && !m.IsRead),
            UnreadNotifications = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead),
            Properties = properties.Select(p => new HostPropertyListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                City = p.City,
                PropertyType = p.PropertyType,
                PricePerNight = p.PricePerNight,
                IsActive = p.IsActive,
                ImageCount = p.Images.Count,
                ImageUrl = (p.Images.FirstOrDefault(i => i.IsPrimary) ?? p.Images.FirstOrDefault())?.ImageUrl,
                BookingCount = p.Bookings.Count,
                PendingBookingCount = p.Bookings.Count(b => b.Status == BookingStatus.Pending),
                CreatedAt = p.CreatedAt,
                HasCheckInProcess = p.CheckInProcess != null
            }).ToList(),
            RecentBookings = bookings.Take(6).Select(b => BookingsController.MapItem(b)).ToList()
        };

        return View(vm);
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
