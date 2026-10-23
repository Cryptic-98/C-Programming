using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;
using StayBnB.Models.ViewModels;
using StayBnB.Services;

namespace StayBnB.Controllers;

/// <summary>
/// The "Become a Host" flow. A guest submits their property and images together;
/// the property and the application are saved in one transaction so an application
/// can never exist without its property or its photos.
/// </summary>
[Authorize(Roles = RoleSeeder.GuestRole)]
public class HostApplicationController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IActivityLogger _logger;
    private readonly INotificationService _notifications;
    private readonly ILogger<HostApplicationController> _applicationLogger;

    public HostApplicationController(
        ApplicationDbContext db,
        IFileStorageService storage,
        IActivityLogger logger,
        INotificationService notifications,
        ILogger<HostApplicationController> applicationLogger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
        _notifications = notifications;
        _applicationLogger = applicationLogger;
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = CurrentUserId;

        var pending = await _db.HostApplications
            .FirstOrDefaultAsync(a => a.ApplicationUserId == userId && a.Status == ApplicationStatus.Pending);

        if (pending is not null)
        {
            TempData["Error"] = "You already have an application under review.";
            return RedirectToAction(nameof(Status));
        }

        ViewBag.Amenities = await _db.Amenities.OrderBy(a => a.Name).ToListAsync();
        ViewBag.PropertyTypes = Enum.GetValues<PropertyType>();
        return View(new HostApplicationViewModel { Country = "South Africa", MaxGuests = 2 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HostApplicationViewModel vm)
    {
        ViewBag.Amenities = await _db.Amenities.OrderBy(a => a.Name).ToListAsync();
        ViewBag.PropertyTypes = Enum.GetValues<PropertyType>();

        if (!ModelState.IsValid)
            return View(vm);

        var userId = CurrentUserId;

        if (await _db.HostApplications.AnyAsync(a => a.ApplicationUserId == userId && a.Status == ApplicationStatus.Pending))
        {
            TempData["Error"] = "You already have an application under review.";
            return RedirectToAction(nameof(Status));
        }

        var savedFiles = new List<string>();

        try
        {
            var property = new HostProperty
            {
                Title = vm.Title.Trim(),
                Description = vm.Description.Trim(),
                PricePerNight = vm.PricePerNight!.Value,
                CleaningFee = vm.CleaningFee,
                ServiceFee = vm.ServiceFee,
                Address = vm.Address.Trim(),
                City = vm.City.Trim(),
                Country = vm.Country.Trim(),
                PropertyType = vm.PropertyType!.Value,
                MaxGuests = vm.MaxGuests!.Value,
                Bedrooms = vm.Bedrooms,
                // The listing only goes live once an admin approves the application.
                IsActive = false,
                HostId = userId,
                CreatedAt = DateTime.UtcNow
            };

            if (vm.SelectedAmenityIds.Count > 0)
            {
                var amenities = await _db.Amenities.Where(a => vm.SelectedAmenityIds.Contains(a.Id)).ToListAsync();
                property.Amenities = amenities;
            }

            var files = vm.Images.Where(f => f.Length > 0).ToList();
            for (var i = 0; i < files.Count; i++)
            {
                var url = await _storage.SaveImageAsync(files[i]);
                savedFiles.Add(url);
                property.Images.Add(new PropertyImage
                {
                    ImageUrl = url,
                    IsPrimary = i == vm.PrimaryImageIndex
                });
            }

            if (!property.Images.Any(i => i.IsPrimary))
                property.Images.First().IsPrimary = true;

            var transaction = _db.Database.IsRelational() ? await _db.Database.BeginTransactionAsync() : null;

            try
            {
                _db.HostProperties.Add(property);
                await _db.SaveChangesAsync();

                var application = new HostApplication
                {
                    ApplicationUserId = userId,
                    PropertyId = property.Id,
                    Status = ApplicationStatus.Pending,
                    AppliedAt = DateTime.UtcNow
                };

                _db.HostApplications.Add(application);
                await _db.SaveChangesAsync();

                await _logger.LogAsync(userId, $"Submitted a host application for '{property.Title}'", ActivityType.Application);

                // Tell the admins that a new application is waiting.
                foreach (var adminId in await FindAdminIdsAsync())
                {
                    await _notifications.NotifyAsync(
                        adminId,
                        NotificationType.System,
                        "New host application",
                        $"{vm.Title} is waiting for review.",
                        "/ApplicationReview/Index");
                }

                if (transaction is not null)
                    await transaction.CommitAsync();
            }
            catch
            {
                if (transaction is not null)
                    await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                if (transaction is not null)
                    await transaction.DisposeAsync();
            }

            TempData["Toast"] = "Your application has been submitted. We will review it shortly.";
            return RedirectToAction(nameof(Status));
        }
        catch (Exception ex)
        {
            _applicationLogger.LogError(ex, "Could not save the host application for user {UserId}.", userId);

            // Roll back the files we already wrote so no orphans are left behind.
            foreach (var file in savedFiles)
                _storage.DeleteImage(file);

            var message = ex is InvalidDataException
                ? ex.Message
                : "We could not save your application. Please try again.";
            ModelState.AddModelError(string.Empty, message);
            return View(vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var userId = CurrentUserId;

        var applications = await _db.HostApplications
            .Include(a => a.Property).ThenInclude(p => p.Images)
            .Where(a => a.ApplicationUserId == userId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

        var vm = new List<HostApplicationStatusViewModel>();

        foreach (var application in applications)
        {
            var primary = application.Property.Images.FirstOrDefault(i => i.IsPrimary) ?? application.Property.Images.FirstOrDefault();

            vm.Add(new HostApplicationStatusViewModel
            {
                ApplicationId = application.Id,
                Status = application.Status,
                AppliedAt = application.AppliedAt,
                ReviewedAt = application.ReviewedAt,
                RejectionReason = application.RejectionReason,
                PropertyTitle = application.Property.Title,
                PropertyCity = application.Property.City,
                PropertyImageUrl = primary?.ImageUrl,
                PropertyId = application.PropertyId,
                ApplicantName = application.ApplicationUser?.FullName ?? string.Empty,
                Images = application.Property.Images
                    .Select(i => new PropertyImageViewModel { Id = i.Id, ImageUrl = i.ImageUrl, IsPrimary = i.IsPrimary })
                    .ToList()
            });
        }

        ViewBag.Applications = vm;
        return View();
    }

    private async Task<List<string>> FindAdminIdsAsync()
    {
        var roleIds = await _db.Roles.Where(r => r.Name == RoleSeeder.AdminRole || r.Name == RoleSeeder.SuperAdminRole)
            .Select(r => r.Id).ToListAsync();

        return await _db.UserRoles.Where(ur => roleIds.Contains(ur.RoleId)).Select(ur => ur.UserId).Distinct().ToListAsync();
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
