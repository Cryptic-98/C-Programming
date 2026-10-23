using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;
using StayBnB.Models.ViewModels;
using StayBnB.Services;

namespace StayBnB.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
public class ApplicationReviewController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IHostApplicationService _hostApplications;

    public ApplicationReviewController(ApplicationDbContext db, IHostApplicationService hostApplications)
    {
        _db = db;
        _hostApplications = hostApplications;
    }

    public async Task<IActionResult> Index(ApplicationStatus? status)
    {
        IQueryable<HostApplication> query = _db.HostApplications
            .Include(a => a.Property).ThenInclude(p => p.Images)
            .Include(a => a.ApplicationUser);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var applications = await query.OrderByDescending(a => a.AppliedAt).ToListAsync();

        var vm = applications.Select(a => new HostApplicationReviewViewModel
        {
            Id = a.Id,
            ApplicantName = a.ApplicationUser?.FullName ?? "Applicant",
            ApplicantEmail = a.ApplicationUser?.Email ?? string.Empty,
            PropertyTitle = a.Property.Title,
            PropertyDescription = a.Property.Description,
            PricePerNight = a.Property.PricePerNight,
            CleaningFee = a.Property.CleaningFee,
            ServiceFee = a.Property.ServiceFee,
            Address = a.Property.Address,
            City = a.Property.City,
            Country = a.Property.Country,
            PropertyType = a.Property.PropertyType,
            MaxGuests = a.Property.MaxGuests,
            Bedrooms = a.Property.Bedrooms,
            Status = a.Status,
            AppliedAt = a.AppliedAt,
            ReviewedAt = a.ReviewedAt,
            RejectionReason = a.RejectionReason,
            ImageCount = a.Property.Images.Count,
            Images = a.Property.Images.Select(i => new PropertyImageViewModel { Id = i.Id, ImageUrl = i.ImageUrl, IsPrimary = i.IsPrimary }).ToList()
        }).ToList();

        ViewBag.SelectedStatus = status;

        var counts = new Dictionary<ApplicationStatus, int>();
        foreach (var value in Enum.GetValues<ApplicationStatus>())
            counts[value] = await _db.HostApplications.CountAsync(a => a.Status == value);
        ViewBag.Counts = counts;

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var application = await _db.HostApplications
            .Include(a => a.Property).ThenInclude(p => p.Images)
            .Include(a => a.Property).ThenInclude(p => p.Amenities)
            .Include(a => a.ApplicationUser)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
            return NotFound();

        var vm = new HostApplicationReviewViewModel
        {
            Id = application.Id,
            ApplicantName = application.ApplicationUser?.FullName ?? "Applicant",
            ApplicantEmail = application.ApplicationUser?.Email ?? string.Empty,
            PropertyTitle = application.Property.Title,
            PropertyDescription = application.Property.Description,
            PricePerNight = application.Property.PricePerNight,
            CleaningFee = application.Property.CleaningFee,
            ServiceFee = application.Property.ServiceFee,
            Address = application.Property.Address,
            City = application.Property.City,
            Country = application.Property.Country,
            PropertyType = application.Property.PropertyType,
            MaxGuests = application.Property.MaxGuests,
            Bedrooms = application.Property.Bedrooms,
            Status = application.Status,
            AppliedAt = application.AppliedAt,
            ReviewedAt = application.ReviewedAt,
            RejectionReason = application.RejectionReason,
            ImageCount = application.Property.Images.Count,
            Images = application.Property.Images.Select(i => new PropertyImageViewModel { Id = i.Id, ImageUrl = i.ImageUrl, IsPrimary = i.IsPrimary }).ToList(),
            Amenities = application.Property.Amenities.Select(x => new AmenityViewModel { Id = x.Id, Name = x.Name, IconClass = x.IconClass ?? "bi bi-check-circle" }).ToList()
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ApplicationReviewViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please give a reason of at least 10 characters when rejecting an application.";
            return RedirectToAction(nameof(Details), new { id = vm.ApplicationId });
        }

        var adminId = CurrentUserId;

        var result = vm.Approve
            ? await _hostApplications.ApproveAsync(vm.ApplicationId, adminId)
            : await _hostApplications.RejectAsync(vm.ApplicationId, adminId, vm.RejectionReason!);

        if (result.Success)
        {
            TempData["Toast"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
