using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;
using StayBnB.Models.ViewModels;

namespace StayBnB.Controllers;

[Authorize(Roles = "Guest,Host,Admin,SuperAdmin")]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;

    public NotificationsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;

        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();

        var vm = new NotificationListViewModel
        {
            UnreadCount = notifications.Count(n => !n.IsRead),
            Items = notifications.Select(n => new NotificationItemViewModel
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                LinkUrl = n.LinkUrl,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = CurrentUserId;

        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification is null)
            return NotFound();

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { id, isRead = true, unreadCount = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead) });

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(int id)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == CurrentUserId);

        if (notification is null)
            return NotFound();

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(notification.LinkUrl) && Url.IsLocalUrl(notification.LinkUrl))
            return LocalRedirect(notification.LinkUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = CurrentUserId;

        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var notification in unread)
            notification.IsRead = true;

        await _db.SaveChangesAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { unreadCount = 0 });

        TempData["Toast"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Badge count for the bell icon.</summary>
    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = CurrentUserId;

        var count = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        return Json(new { count });
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
