using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;
using StayBnB.Models.ViewModels;

namespace StayBnB.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminDashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminDashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;

        var vm = new AdminDashboardViewModel
        {
            PendingApplications = await _db.HostApplications.CountAsync(a => a.Status == ApplicationStatus.Pending),
            TotalApplications = await _db.HostApplications.CountAsync(),
            BookingsToday = await _db.Bookings.CountAsync(b => b.CheckInDate == today),
            PendingBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Pending),
            CheckInsAwaitingVerification = await _db.GuestCheckIns.CountAsync(c => c.Status == CheckInStatus.Completed),
            TotalUsers = await _db.Users.CountAsync()
        };

        var guestRoleId = await _db.Roles.Where(r => r.Name == RoleSeeder.GuestRole).Select(r => r.Id).FirstOrDefaultAsync();
        var hostRoleId = await _db.Roles.Where(r => r.Name == RoleSeeder.HostRole).Select(r => r.Id).FirstOrDefaultAsync();

        vm.TotalGuests = guestRoleId is null
            ? 0
            : await _db.UserRoles.CountAsync(ur => ur.RoleId == guestRoleId);

        vm.ActiveHosts = hostRoleId is null
            ? 0
            : await _db.UserRoles.CountAsync(ur => ur.RoleId == hostRoleId);

        var applications = await _db.HostApplications
            .Include(a => a.Property).ThenInclude(p => p.Images)
            .Include(a => a.Property).ThenInclude(p => p.Amenities)
            .Include(a => a.ApplicationUser)
            .OrderByDescending(a => a.AppliedAt)
            .Take(6)
            .ToListAsync();

        vm.RecentApplications = applications.Select(a => new HostApplicationReviewViewModel
        {
            Id = a.Id,
            ApplicantName = a.ApplicationUser?.FullName ?? "Applicant",
            ApplicantEmail = a.ApplicationUser?.Email ?? string.Empty,
            PropertyTitle = a.Property.Title,
            PricePerNight = a.Property.PricePerNight,
            CleaningFee = a.Property.CleaningFee,
            ServiceFee = a.Property.ServiceFee,
            Address = a.Property.Address,
            City = a.Property.City,
            Country = a.Property.Country,
            PropertyType = a.Property.PropertyType,
            MaxGuests = a.Property.MaxGuests,
            Bedrooms = a.Property.Bedrooms,
            Status = a.Status,
            AppliedAt = a.AppliedAt,
            ReviewedAt = a.ReviewedAt,
            RejectionReason = a.RejectionReason,
            ImageCount = a.Property.Images.Count,
            Images = a.Property.Images.Select(i => new PropertyImageViewModel { Id = i.Id, ImageUrl = i.ImageUrl, IsPrimary = i.IsPrimary }).ToList(),
            Amenities = a.Property.Amenities.Select(x => new AmenityViewModel { Id = x.Id, Name = x.Name, IconClass = x.IconClass ?? "bi bi-check-circle" }).ToList()
        }).ToList();

        var logs = await _db.ActivityLogs
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Take(8)
            .ToListAsync();

        vm.RecentActivity = logs.Select(l => new ActivityLogViewModel
        {
            Id = l.Id,
            UserId = l.UserId,
            UserName = l.User?.FullName ?? "System",
            Action = l.Action,
            ActivityType = l.ActivityType,
            IpAddress = l.IpAddress,
            CreatedAt = l.CreatedAt
        }).ToList();

        return View(vm);
    }
}
