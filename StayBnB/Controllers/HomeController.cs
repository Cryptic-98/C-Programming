using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;
using StayBnB.Models.ViewModels;
using StayBnB.Services;

namespace StayBnB.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var featured = await _db.HostProperties
            .Where(p => p.IsActive)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .ToListAsync();

        var cards = featured.Select(MapCard).ToList();

        ViewBag.TotalStays = await _db.HostProperties.CountAsync(p => p.IsActive);
        ViewBag.TotalCities = await _db.HostProperties.Select(p => p.City).Distinct().CountAsync();
        ViewBag.TotalHosts = await _db.HostProperties.Select(p => p.HostId).Distinct().CountAsync();

        return View(cards);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public new IActionResult StatusCode(int code)
    {
        Response.StatusCode = code;
        ViewBag.StatusCode = code;
        return View();
    }

    internal static PropertyCardViewModel MapCard(HostProperty property)
    {
        var primary = property.Images.FirstOrDefault(i => i.IsPrimary) ?? property.Images.FirstOrDefault();

        return new PropertyCardViewModel
        {
            Id = property.Id,
            Title = property.Title,
            City = property.City,
            PricePerNight = property.PricePerNight,
            PrimaryImageUrl = primary?.ImageUrl,
            AverageRating = property.Reviews.Any()
                ? Math.Round(property.Reviews.Average(r => (double)r.Rating), 2)
                : 0,
            ReviewCount = property.Reviews.Count,
            PropertyType = property.PropertyType,
            MaxGuests = property.MaxGuests,
            Bedrooms = property.Bedrooms,
            IsActive = property.IsActive,
            CleaningFee = property.CleaningFee,
            ServiceFee = property.ServiceFee,
            HostName = property.Host?.FullName ?? string.Empty
        };
    }

    /// <summary>The signed in user's id, or null for an anonymous visitor.</summary>
    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected bool IsInRole(string role) => User.Identity?.IsAuthenticated == true && User.IsInRole(role);
}
