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

[Authorize(Roles = "Guest,Host,Admin,SuperAdmin")]
public class MessagesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IMessagingService _messaging;

    public MessagesController(ApplicationDbContext db, IMessagingService messaging)
    {
        _db = db;
        _messaging = messaging;
    }

    /// <summary>The conversation list, newest activity first, unread conversations on top.</summary>
    public async Task<IActionResult> Inbox()
    {
        var userId = CurrentUserId;

        var messages = await _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();

        var conversations = messages
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g =>
            {
                var last = g.OrderByDescending(m => m.Timestamp).First();
                var other = last.SenderId == userId ? last.Receiver : last.Sender;

                return new ConversationViewModel
                {
                    UserId = g.Key,
                    DisplayName = other?.FullName ?? "Unknown user",
                    AvatarUrl = other?.ProfilePictureUrl ?? string.Empty,
                    LastMessage = last.Content,
                    LastMessageAt = last.Timestamp,
                    UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
                };
            })
            .OrderByDescending(c => c.UnreadCount > 0)
            .ThenByDescending(c => c.LastMessageAt)
            .ToList();

        ViewBag.Conversations = conversations;
        return View();
    }

    /// <summary>Opens a conversation and marks the incoming messages as read.</summary>
    public async Task<IActionResult> Thread(string withUserId)
    {
        var userId = CurrentUserId;

        if (string.IsNullOrWhiteSpace(withUserId))
            return RedirectToAction(nameof(Inbox));

        var other = await _db.Users.FirstOrDefaultAsync(u => u.Id == withUserId);
        if (other is null)
            return NotFound();

        var messages = await _db.Messages
            .Where(m => (m.SenderId == userId && m.ReceiverId == withUserId) ||
                        (m.SenderId == withUserId && m.ReceiverId == userId))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        // Opening the thread marks the incoming messages as read in the database.
        var unread = messages.Where(m => m.ReceiverId == userId && !m.IsRead).ToList();
        if (unread.Count > 0)
        {
            foreach (var message in unread)
                message.IsRead = true;

            await _db.SaveChangesAsync();
        }

        var propertyId = messages.LastOrDefault()?.PropertyId;

        var vm = new MessageThreadViewModel
        {
            OtherUserId = other.Id,
            OtherDisplayName = other.FullName,
            OtherAvatarUrl = other.ProfilePictureUrl ?? string.Empty,
            PropertyId = propertyId,
            PropertyTitle = propertyId is null ? null : await _db.HostProperties.Where(p => p.Id == propertyId).Select(p => p.Title).FirstOrDefaultAsync(),
            Messages = messages.Select(m => new MessageViewModel
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                Timestamp = m.Timestamp,
                IsMine = m.SenderId == userId
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Compose(string? receiverId, int? propertyId)
    {
        ViewBag.ReceiverId = receiverId ?? string.Empty;
        ViewBag.PropertyId = propertyId;
        ViewBag.Recipients = await _db.Users
            .Where(u => u.Id != CurrentUserId)
            .OrderBy(u => u.FirstName)
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync();

        return View(new SendMessageViewModel { ReceiverId = receiverId ?? string.Empty, PropertyId = propertyId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(SendMessageViewModel vm)
    {
        var userId = CurrentUserId;

        if (!ModelState.IsValid)
        {
            ViewBag.Recipients = await _db.Users
                .Where(u => u.Id != userId)
                .OrderBy(u => u.FirstName)
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync();
            return View("Compose", vm);
        }

        if (vm.ReceiverId == userId)
        {
            ModelState.AddModelError(nameof(vm.ReceiverId), "You cannot send a message to yourself.");
            ViewBag.Recipients = await _db.Users
                .Where(u => u.Id != userId)
                .OrderBy(u => u.FirstName)
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync();
            return View("Compose", vm);
        }

        var receiver = await _db.Users.FirstOrDefaultAsync(u => u.Id == vm.ReceiverId);
        if (receiver is null)
        {
            ModelState.AddModelError(nameof(vm.ReceiverId), "That user could not be found.");
            ViewBag.Recipients = await _db.Users
                .Where(u => u.Id != userId)
                .OrderBy(u => u.FirstName)
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync();
            return View("Compose", vm);
        }

        await _messaging.SendAsync(userId, receiver.Id, vm.Content, vm.PropertyId);

        TempData["Toast"] = "Your message was sent.";
        return RedirectToAction(nameof(Thread), new { withUserId = receiver.Id });
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
