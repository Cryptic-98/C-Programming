using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;
using StayBnB.Models.ViewModels;
using StayBnB.Services;

namespace StayBnB.Controllers.Admin;

/// <summary>User management. Only the SuperAdmin can promote a guest to Admin.</summary>
[Authorize(Roles = RoleSeeder.SuperAdminRole)]
public class UserManagementController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _logger;
    private readonly INotificationService _notifications;
    private readonly ILogger<UserManagementController> _applicationLogger;

    public UserManagementController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IActivityLogger logger,
        INotificationService notifications,
        ILogger<UserManagementController> applicationLogger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
        _notifications = notifications;
        _applicationLogger = applicationLogger;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var guestRoleId = await _db.Roles.Where(r => r.Name == RoleSeeder.GuestRole).Select(r => r.Id).FirstOrDefaultAsync();
        var adminRoleId = await _db.Roles.Where(r => r.Name == RoleSeeder.AdminRole).Select(r => r.Id).FirstOrDefaultAsync();

        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.FirstName.Contains(term) ||
                u.LastName.Contains(term) ||
                (u.Email != null && u.Email.Contains(term)));
        }

        var users = await query.OrderBy(u => u.FirstName).Take(100).ToListAsync();
        var userIds = users.Select(u => u.Id).ToList();

        var guestIds = guestRoleId is null
            ? new List<string>()
            : await _db.UserRoles.Where(ur => ur.RoleId == guestRoleId && userIds.Contains(ur.UserId)).Select(ur => ur.UserId).ToListAsync();

        var adminIds = adminRoleId is null
            ? new List<string>()
            : await _db.UserRoles.Where(ur => ur.RoleId == adminRoleId && userIds.Contains(ur.UserId)).Select(ur => ur.UserId).ToListAsync();

        var propertyCounts = await _db.HostProperties
            .Where(p => userIds.Contains(p.HostId))
            .GroupBy(p => p.HostId)
            .Select(g => new { HostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.HostId, x => x.Count);

        var vm = users.Select(u => new AdminUserViewModel
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? string.Empty,
            ProfilePictureUrl = u.ProfilePictureUrl,
            Role = adminIds.Contains(u.Id) ? "Admin" : guestIds.Contains(u.Id) ? "Guest" : "User",
            CreatedAt = u.CreatedAt,
            PropertyCount = propertyCounts.TryGetValue(u.Id, out var count) ? count : 0
        }).ToList();

        ViewBag.Search = search;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToAdmin(PromoteUserViewModel vm)
    {
        var adminId = CurrentUserId;

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Choose a user to promote.";
            return RedirectToAction(nameof(Index));
        }

        if (vm.UserId == adminId)
        {
            TempData["Error"] = "You cannot promote your own account.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(vm.UserId);
        if (user is null)
        {
            TempData["Error"] = "That user could not be found.";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.IsInRoleAsync(user, RoleSeeder.SuperAdminRole))
        {
            TempData["Error"] = "A SuperAdmin cannot be promoted.";
            return RedirectToAction(nameof(Index));
        }

        if (!await _userManager.IsInRoleAsync(user, RoleSeeder.GuestRole))
        {
            TempData["Error"] = "Only a guest can be promoted to Admin.";
            return RedirectToAction(nameof(Index));
        }

        var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync()
            : null;
        var addedAdmin = false;
        var removedGuest = false;
        try
        {
            var addResult = await _userManager.AddToRoleAsync(user, RoleSeeder.AdminRole);
            if (!addResult.Succeeded)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync();
                TempData["Error"] = string.Join(" ", addResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }
            addedAdmin = true;

            var removeResult = await _userManager.RemoveFromRoleAsync(user, RoleSeeder.GuestRole);
            if (!removeResult.Succeeded)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync();
                else
                    await _userManager.RemoveFromRoleAsync(user, RoleSeeder.AdminRole);

                TempData["Error"] = string.Join(" ", removeResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }
            removedGuest = true;

            // Refresh the promoted user's session, but never treat a stamp failure as
            // a reason to undo a role change that already committed.
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
                _applicationLogger.LogWarning("Admin role granted to {UserId} but the security stamp could not be refreshed.", user.Id);

            await _logger.LogAsync(adminId, $"Promoted {user.FullName} from Guest to Admin", ActivityType.RoleChange);
            await _notifications.NotifyAsync(user.Id, NotificationType.System, "You are now an Admin",
                "An administrator gave you access to the StayBnB admin dashboard.",
                "/AdminDashboard/Index");

            if (transaction is not null)
                await transaction.CommitAsync();

            TempData["Toast"] = $"{user.FullName} is now an Admin.";
        }
        catch (Exception ex)
        {
            if (transaction is not null)
                await transaction.RollbackAsync();
            else
            {
                if (removedGuest)
                    await _userManager.AddToRoleAsync(user, RoleSeeder.GuestRole);
                if (addedAdmin)
                    await _userManager.RemoveFromRoleAsync(user, RoleSeeder.AdminRole);
            }

            _applicationLogger.LogError(ex, "Could not promote user {UserId} to Admin.", vm.UserId);
            TempData["Error"] = "The role change could not be completed.";
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
