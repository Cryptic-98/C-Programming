using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Enums;
using StayBnB.Models.ViewModels;

namespace StayBnB.Controllers.Admin;

/// <summary>Read-only audit trail. There is deliberately no edit or delete action.</summary>
[Authorize(Roles = "Admin,SuperAdmin")]
public class ActivityLogController : Controller
{
    private const int PageSize = 25;

    private readonly ApplicationDbContext _db;

    public ActivityLogController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(ActivityLogFilterViewModel filter)
    {
        if (filter.Page < 1) filter.Page = 1;

        var query = _db.ActivityLogs.AsQueryable();

        if (filter.Type.HasValue)
            query = query.Where(l => l.ActivityType == filter.Type.Value);

        if (!string.IsNullOrWhiteSpace(filter.UserSearch))
        {
            var term = filter.UserSearch.Trim();
            query = query.Where(l => l.User != null && (l.User.FirstName.Contains(term) || l.User.LastName.Contains(term) || (l.User.Email != null && l.User.Email.Contains(term))));
        }

        if (filter.From.HasValue)
            query = query.Where(l => l.CreatedAt >= filter.From.Value.Date);

        if (filter.To.HasValue)
            query = query.Where(l => l.CreatedAt < filter.To.Value.Date.AddDays(1));

        var total = await query.CountAsync();
        filter.TotalResults = total;
        filter.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));

        if (filter.Page > filter.TotalPages)
            filter.Page = filter.TotalPages;

        var logs = await query
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((filter.Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        filter.Results = logs.Select(l => new ActivityLogViewModel
        {
            Id = l.Id,
            UserId = l.UserId,
            UserName = l.User?.FullName ?? "System",
            Action = l.Action,
            ActivityType = l.ActivityType,
            IpAddress = l.IpAddress,
            CreatedAt = l.CreatedAt
        }).ToList();

        ViewBag.Types = Enum.GetValues<ActivityType>();
        return View(filter);
    }
}
