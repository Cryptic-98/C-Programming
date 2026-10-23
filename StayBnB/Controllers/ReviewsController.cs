using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.ViewModels;
using StayBnB.Services;

namespace StayBnB.Controllers;

[Authorize(Roles = RoleSeeder.GuestRole)]
public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IReviewService _reviews;

    public ReviewsController(ApplicationDbContext db, IReviewService reviews)
    {
        _db = db;
        _reviews = reviews;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int propertyId)
    {
        var userId = CurrentUserId;

        var property = await _db.HostProperties.FirstOrDefaultAsync(p => p.Id == propertyId);
        if (property is null) return NotFound();

        var (allowed, message) = await _reviews.CanReviewAsync(userId, propertyId);
        if (!allowed)
        {
            TempData["Error"] = message;
            return RedirectToAction("Details", "Properties", new { id = propertyId });
        }

        return View(new ReviewViewModel { PropertyId = propertyId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _reviews.AddReviewAsync(CurrentUserId, vm.PropertyId, vm.Rating, vm.Comment);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction("Details", "Properties", new { id = vm.PropertyId });
        }

        TempData["Toast"] = result.Message;
        return RedirectToAction("Details", "Properties", new { id = vm.PropertyId });
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
