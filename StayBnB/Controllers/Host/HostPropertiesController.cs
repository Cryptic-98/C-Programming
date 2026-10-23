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
public class HostPropertiesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IActivityLogger _logger;

    public HostPropertiesController(ApplicationDbContext db, IFileStorageService storage, IActivityLogger logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;

        var properties = await _db.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Bookings)
            .Include(p => p.CheckInProcess)
            .Where(p => p.HostId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var vm = properties.Select(p => new HostPropertyListItemViewModel
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
        }).ToList();

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
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
            IsActive = true,
            HostId = userId,
            CreatedAt = DateTime.UtcNow
        };

        if (vm.SelectedAmenityIds.Count > 0)
        {
            property.Amenities = await _db.Amenities.Where(a => vm.SelectedAmenityIds.Contains(a.Id)).ToListAsync();
        }

        var files = vm.Images.Where(f => f.Length > 0).ToList();
        for (var i = 0; i < files.Count; i++)
        {
            var url = await _storage.SaveImageAsync(files[i]);
            property.Images.Add(new PropertyImage { ImageUrl = url, IsPrimary = i == vm.PrimaryImageIndex });
        }

        if (!property.Images.Any(i => i.IsPrimary) && property.Images.Any())
            property.Images.First().IsPrimary = true;

        _db.HostProperties.Add(property);
        await _db.SaveChangesAsync();

        await _logger.LogAsync(userId, $"Created the listing '{property.Title}'", ActivityType.Property);

        TempData["Toast"] = "Your listing has been added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var property = await _db.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Amenities)
            .FirstOrDefaultAsync(p => p.Id == id && p.HostId == CurrentUserId);

        if (property is null)
            return NotFound();

        ViewBag.Amenities = await _db.Amenities.OrderBy(a => a.Name).ToListAsync();
        ViewBag.PropertyTypes = Enum.GetValues<PropertyType>();

        var vm = new HostPropertyEditViewModel
        {
            Id = property.Id,
            Title = property.Title,
            Description = property.Description,
            PricePerNight = property.PricePerNight,
            CleaningFee = property.CleaningFee,
            Address = property.Address,
            City = property.City,
            Country = property.Country,
            PropertyType = property.PropertyType,
            MaxGuests = property.MaxGuests,
            Bedrooms = property.Bedrooms,
            SelectedAmenityIds = property.Amenities.Select(a => a.Id).ToList(),
            ExistingImages = property.Images
                .Select(i => new PropertyImageViewModel { Id = i.Id, ImageUrl = i.ImageUrl, IsPrimary = i.IsPrimary })
                .ToList()
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(HostPropertyEditViewModel vm)
    {
        ViewBag.Amenities = await _db.Amenities.OrderBy(a => a.Name).ToListAsync();
        ViewBag.PropertyTypes = Enum.GetValues<PropertyType>();

        var property = await _db.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Amenities)
            .FirstOrDefaultAsync(p => p.Id == vm.Id && p.HostId == CurrentUserId);

        if (property is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            vm.ExistingImages = property.Images
                .Select(i => new PropertyImageViewModel { Id = i.Id, ImageUrl = i.ImageUrl, IsPrimary = i.IsPrimary })
                .ToList();
            return View(vm);
        }

        property.Title = vm.Title.Trim();
        property.Description = vm.Description.Trim();
        property.PricePerNight = vm.PricePerNight!.Value;
        property.CleaningFee = vm.CleaningFee;
        property.ServiceFee = vm.ServiceFee;
        property.Address = vm.Address.Trim();
        property.City = vm.City.Trim();
        property.Country = vm.Country.Trim();
        property.PropertyType = vm.PropertyType;
        property.MaxGuests = vm.MaxGuests;
        property.Bedrooms = vm.Bedrooms;

        var amenities = await _db.Amenities.Where(a => vm.SelectedAmenityIds.Contains(a.Id)).ToListAsync();
        property.Amenities = amenities;

        var removedImageUrls = new List<string>();
        if (vm.RemoveImageIds.Count > 0)
        {
            var toRemove = property.Images.Where(i => vm.RemoveImageIds.Contains(i.Id)).ToList();
            if (property.Images.Count - toRemove.Count < 1)
            {
                ModelState.AddModelError(string.Empty, "A listing needs at least one photo.");
                vm.ExistingImages = property.Images
                    .Select(i => new PropertyImageViewModel { Id = i.Id, ImageUrl = i.ImageUrl, IsPrimary = i.IsPrimary })
                    .ToList();
                return View(vm);
            }

            foreach (var image in toRemove)
            {
                removedImageUrls.Add(image.ImageUrl);
                property.Images.Remove(image);
            }
        }

        var files = vm.NewImages.Where(f => f.Length > 0).ToList();
        for (var i = 0; i < files.Count; i++)
        {
            var url = await _storage.SaveImageAsync(files[i]);
            property.Images.Add(new PropertyImage { ImageUrl = url, IsPrimary = property.Images.Count == 0 && i == 0 });
        }

        await _db.SaveChangesAsync();

        foreach (var imageUrl in removedImageUrls)
            _storage.DeleteImage(imageUrl);

        await _logger.LogAsync(CurrentUserId, $"Updated the listing '{property.Title}'", ActivityType.Property);

        TempData["Toast"] = "Your listing has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var property = await _db.HostProperties.FirstOrDefaultAsync(p => p.Id == id && p.HostId == CurrentUserId);
        if (property is null)
            return NotFound();

        if (!property.IsActive && !await _db.PropertyImages.AnyAsync(i => i.PropertyId == property.Id))
        {
            TempData["Error"] = "Add at least one photo before activating this listing.";
            return RedirectToAction(nameof(Index));
        }

        property.IsActive = !property.IsActive;
        await _db.SaveChangesAsync();

        await _logger.LogAsync(CurrentUserId,
            $"{(property.IsActive ? "Activated" : "Deactivated")} the listing '{property.Title}'",
            ActivityType.Property);

        TempData["Toast"] = property.IsActive ? "Your listing is now live." : "Your listing has been paused.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId;

        var property = await _db.HostProperties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.HostId == userId);

        if (property is null)
            return NotFound();

        var hasFutureBookings = await _db.Bookings.AnyAsync(b =>
            b.PropertyId == property.Id &&
            b.CheckOutDate >= DateTime.Today &&
            (b.Status == BookingStatus.Approved || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Pending));

        if (hasFutureBookings)
        {
            TempData["Error"] = "This listing still has upcoming bookings. Cancel or complete them before deleting it.";
            return RedirectToAction(nameof(Index));
        }

        var title = property.Title;
        var imageUrls = property.Images.Select(i => i.ImageUrl).ToList();
        _db.HostProperties.Remove(property);
        await _db.SaveChangesAsync();

        foreach (var imageUrl in imageUrls)
            _storage.DeleteImage(imageUrl);

        await _logger.LogAsync(userId, $"Deleted the listing '{title}'", ActivityType.Property);

        TempData["Toast"] = "The listing has been deleted.";
        return RedirectToAction(nameof(Index));
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
