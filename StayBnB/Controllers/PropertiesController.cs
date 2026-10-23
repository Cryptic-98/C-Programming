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

[AllowAnonymous]
public class PropertiesController : Controller
{
    private const int PageSize = 12;

    private readonly ApplicationDbContext _db;
    private readonly IPricingService _pricing;

    public PropertiesController(ApplicationDbContext db, IPricingService pricing)
    {
        _db = db;
        _pricing = pricing;
    }

    public async Task<IActionResult> Index(PropertyFilterViewModel filter)
    {
        if (filter.Page < 1) filter.Page = 1;

        var query = _db.HostProperties.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var city = filter.City.Trim();
            query = query.Where(p => p.City.Contains(city));
        }

        if (filter.PropertyType.HasValue)
            query = query.Where(p => p.PropertyType == filter.PropertyType.Value);

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.PricePerNight >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.PricePerNight <= filter.MaxPrice.Value);

        if (filter.Guests.HasValue)
            query = query.Where(p => p.MaxGuests >= filter.Guests.Value);

        // Date search from the landing page: only show places that are free for the
        // whole requested window. Cancelled/rejected bookings do not block dates.
        if (filter.CheckIn.HasValue && filter.CheckOut.HasValue)
        {
            var from = filter.CheckIn.Value.Date;
            var to = filter.CheckOut.Value.Date;

            if (to > from)
            {
                query = query.Where(p => !p.Bookings.Any(b =>
                    b.Status != BookingStatus.Cancelled &&
                    b.Status != BookingStatus.Rejected &&
                    b.CheckInDate < to &&
                    b.CheckOutDate > from));
            }
        }

        var total = await query.CountAsync();
        filter.TotalResults = total;
        filter.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));

        if (filter.Page > filter.TotalPages)
            filter.Page = filter.TotalPages;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var wishlisted = userId is null
            ? new List<int>()
            : await _db.WishlistItems.Where(w => w.UserId == userId).Select(w => w.PropertyId).ToListAsync();

        var properties = await query
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .Include(p => p.Host)
            .OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => (double)r.Rating) : 0)
            .ThenBy(p => p.Title)
            .Skip((filter.Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        filter.Results = properties.Select(p =>
        {
            var card = HomeController.MapCard(p);
            card.IsWishlisted = wishlisted.Contains(p.Id);
            return card;
        }).ToList();

        return View(filter);
    }

    /// <summary>
    /// Discovery hub. Deliberately different from <see cref="Index"/>: this page
    /// groups stays by city and type and surfaces top-rated / newest picks,
    /// whereas Stays is the filterable, paginated result list.
    /// </summary>
    public async Task<IActionResult> Explore()
    {
        var properties = await _db.HostProperties
            .Where(p => p.IsActive)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .Include(p => p.Host)
            .ToListAsync();

        var cities = properties
            .GroupBy(p => p.City)
            .Select(g => new ExploreCityViewModel
            {
                City = g.Key,
                Country = g.Select(p => p.Country).FirstOrDefault() ?? "South Africa",
                Count = g.Count(),
                FromPrice = g.Min(p => p.PricePerNight),
                ImageUrl = g.SelectMany(p => p.Images).FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                    ?? g.SelectMany(p => p.Images).FirstOrDefault()?.ImageUrl
            })
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.City)
            .ToList();

        var types = properties
            .GroupBy(p => p.PropertyType)
            .Select(g => new ExploreTypeViewModel
            {
                PropertyType = g.Key,
                Count = g.Count(),
                ImageUrl = g.SelectMany(p => p.Images).FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                    ?? g.SelectMany(p => p.Images).FirstOrDefault()?.ImageUrl
            })
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.PropertyType)
            .ToList();

        var topRated = properties
            .Where(p => p.Reviews.Any())
            .OrderByDescending(p => p.Reviews.Average(r => (double)r.Rating))
            .ThenByDescending(p => p.Reviews.Count)
            .Take(4)
            .Select(HomeController.MapCard)
            .ToList();

        var newest = properties
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
            .Select(HomeController.MapCard)
            .ToList();

        var model = new ExploreViewModel
        {
            TotalStays = properties.Count,
            TotalCities = cities.Count,
            Cities = cities,
            Types = types,
            TopRated = topRated,
            Newest = newest
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var property = await _db.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Amenities)
            .Include(p => p.Reviews).ThenInclude(r => r.Reviewer)
            .Include(p => p.Host)
            .Include(p => p.CheckInProcess)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // A host may open their own listing, everyone else only sees active listings.
        if (!property.IsActive && property.HostId != userId)
            return NotFound();

        var detail = new HostPropertyDetail
        {
            Id = property.Id,
            Title = property.Title,
            Description = property.Description,
            PricePerNight = property.PricePerNight,
            CleaningFee = property.CleaningFee,
            ServiceFee = property.ServiceFee,
            Address = property.Address,
            City = property.City,
            Country = property.Country,
            PropertyType = property.PropertyType,
            MaxGuests = property.MaxGuests,
            Bedrooms = property.Bedrooms,
            IsActive = property.IsActive,
            HostId = property.HostId,
            HostName = property.Host?.FullName ?? "Staybnb host",
            HostAvatarUrl = property.Host?.ProfilePictureUrl ?? string.Empty,
            HostMemberSince = property.Host?.CreatedAt ?? DateTime.UtcNow,
            AverageRating = property.Reviews.Any() ? Math.Round(property.Reviews.Average(r => (double)r.Rating), 2) : 0,
            ReviewCount = property.Reviews.Count,
            Images = property.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                .Select(i => new PropertyImageViewModel { Id = i.Id, ImageUrl = i.ImageUrl, IsPrimary = i.IsPrimary })
                .ToList(),
            Amenities = property.Amenities.Select(a => new AmenityViewModel { Id = a.Id, Name = a.Name, IconClass = a.IconClass ?? "bi bi-check-circle" }).ToList(),
            Reviews = property.Reviews.OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewViewModel
                {
                    Id = r.Id,
                    PropertyId = r.PropertyId,
                    ReviewerName = r.Reviewer?.FullName ?? "Guest",
                    ReviewerAvatarUrl = r.Reviewer?.ProfilePictureUrl ?? string.Empty,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    StayPeriod = r.CreatedAt.ToString("MMMM yyyy")
                })
                .ToList()
        };

        if (property.CheckInProcess is not null)
        {
            detail.CheckIn = new CheckInInfo
            {
                Title = property.CheckInProcess.Title,
                Steps = CheckInStepReader.DeserializeSteps(property.CheckInProcess.StepsJson)
            };
        }

        var similar = await _db.HostProperties
            .Where(p => p.IsActive && p.Id != property.Id && p.City == property.City)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .Take(4)
            .ToListAsync();

        var vm = new PropertyDetailsViewModel
        {
            Property = detail,
            Similar = similar.Select(HomeController.MapCard).ToList(),
            IsOwner = userId is not null && property.HostId == userId,
            Booking = new BookingRequestViewModel
            {
                PropertyId = property.Id,
                PricePerNight = property.PricePerNight,
                CleaningFee = property.CleaningFee,
                PropertyTitle = property.Title,
                PropertyCity = property.City,
                PropertyImageUrl = detail.Images.FirstOrDefault()?.ImageUrl,
                HostName = detail.HostName
            }
        };

        if (userId is not null)
        {
            vm.IsWishlisted = await _db.WishlistItems.AnyAsync(w => w.UserId == userId && w.PropertyId == property.Id);
            vm.CanReview = await _db.Bookings.AnyAsync(b =>
                b.GuestId == userId && b.PropertyId == property.Id &&
                (b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Completed));
        }

        // Live preview of the price for the dates already in the query string.
        var checkIn = DateTime.Today.AddDays(14);
        var checkOut = checkIn.AddDays(3);
        var price = _pricing.Calculate(property, checkIn, checkOut);
        vm.Booking.CheckInDate = checkIn;
        vm.Booking.CheckOutDate = checkOut;
        vm.Booking.NumberOfGuests = Math.Min(2, property.MaxGuests);
        vm.Booking.Nights = price.Nights;
        vm.Booking.ServiceFee = price.ServiceFee;
        vm.Booking.TotalPrice = price.TotalPrice;

        return View(vm);
    }
}

internal static class CheckInStepReader
{
    public static List<CheckInStepDto> DeserializeSteps(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<CheckInStepDto>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<CheckInStepDto>>(json) ?? new List<CheckInStepDto>();
        }
        catch (System.Text.Json.JsonException)
        {
            return new List<CheckInStepDto>();
        }
    }
}
