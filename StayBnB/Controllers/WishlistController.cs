using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.ViewModels;

namespace StayBnB.Controllers;

[Authorize(Roles = RoleSeeder.GuestRole)]
public class WishlistController : Controller
{
    private readonly ApplicationDbContext _db;

    public WishlistController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;

        var items = await _db.WishlistItems
            .Include(w => w.Property).ThenInclude(p => p.Images)
            .Include(w => w.Property).ThenInclude(p => p.Reviews)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        var vm = new WishlistViewModel
        {
            Items = items.Select(w =>
            {
                var card = HomeController.MapCard(w.Property);
                card.IsWishlisted = true;
                return card;
            }).ToList()
        };

        return View(vm);
    }

    /// <summary>Adds or removes a property. Idempotent: posting twice toggles it back.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int propertyId)
    {
        var userId = CurrentUserId;

        var existing = await _db.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.PropertyId == propertyId);

        if (existing is not null)
        {
            _db.WishlistItems.Remove(existing);
            await _db.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { saved = false, propertyId });

            return RedirectBack();
        }

        var property = await _db.HostProperties.FirstOrDefaultAsync(p => p.Id == propertyId);
        if (property is null)
            return NotFound();

        _db.WishlistItems.Add(new WishlistItem { UserId = userId, PropertyId = propertyId, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { saved = true, propertyId });

        return RedirectBack();
    }

    private IActionResult RedirectBack()
    {
        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            return LocalRedirect(referer);

        return RedirectToAction(nameof(Index));
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
