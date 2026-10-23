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

[Authorize(Roles = RoleSeeder.GuestRole)]
public class GuestCheckInController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IActivityLogger _logger;
    private readonly INotificationService _notifications;
    private readonly ILogger<GuestCheckInController> _applicationLogger;

    public GuestCheckInController(
        ApplicationDbContext db,
        IFileStorageService storage,
        IActivityLogger logger,
        INotificationService notifications,
        ILogger<GuestCheckInController> applicationLogger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
        _notifications = notifications;
        _applicationLogger = applicationLogger;
    }

    /// <summary>Starts (or re-opens) the guided check-in for an approved booking.</summary>
    [HttpGet]
    public async Task<IActionResult> Start(int bookingId)
    {
        var userId = CurrentUserId;

        var booking = await _db.Bookings
            .Include(b => b.Property).ThenInclude(p => p.CheckInProcess)
            .Include(b => b.GuestCheckIn)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == userId);

        if (booking is null)
            return NotFound();

        if (booking.Status is not (BookingStatus.Approved or BookingStatus.CheckedIn))
        {
            TempData["Error"] = "You can only start check-in once the host has approved your booking.";
            return RedirectToAction("Details", "Bookings", new { id = bookingId });
        }

        if (booking.GuestCheckIn?.Status == CheckInStatus.Completed)
            return RedirectToAction("Details", "Bookings", new { id = bookingId });

        if (booking.Property.CheckInProcess is null)
        {
            TempData["Error"] = "The host has not set up a check-in process for this property yet.";
            return RedirectToAction("Details", "Bookings", new { id = bookingId });
        }

        var checkIn = booking.GuestCheckIn;

        if (checkIn is null)
        {
            checkIn = new GuestCheckIn
            {
                BookingId = booking.Id,
                CheckInProcessId = booking.Property.CheckInProcess.Id,
                Status = CheckInStatus.InProgress,
                CurrentStep = 1,
                CreatedAt = DateTime.UtcNow
            };

            _db.GuestCheckIns.Add(checkIn);
            await _db.SaveChangesAsync();

            await _logger.LogAsync(userId, $"Started check-in for booking #{booking.Id}", ActivityType.CheckIn);
        }

        return RedirectToAction(nameof(Step), new { guestCheckInId = checkIn.Id, order = checkIn.CurrentStep });
    }

    [HttpGet]
    public async Task<IActionResult> Step(int guestCheckInId, int order = 1)
    {
        var vm = await BuildStepViewModelAsync(guestCheckInId, order);
        if (vm is null)
            return NotFound();

        if (vm.Status is not (CheckInStatus.Pending or CheckInStatus.InProgress))
            return RedirectToAction("Details", "Bookings", new { id = vm.BookingId });

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Step(CheckInStepViewModel vm)
    {
        var model = await BuildStepViewModelAsync(vm.GuestCheckInId, vm.StepOrder);
        if (model is null)
            return NotFound();

        if (model.Status is not (CheckInStatus.Pending or CheckInStatus.InProgress))
            return RedirectToAction("Details", "Bookings", new { id = model.BookingId });

        if (!ModelState.IsValid)
            return View(model);

        var checkIn = await _db.GuestCheckIns.FirstAsync(c => c.Id == vm.GuestCheckInId);
        var steps = CheckInStepReader.DeserializeSteps(checkIn.CheckInProcess.StepsJson);
        var step = steps.FirstOrDefault(s => s.Order == vm.StepOrder);

        if (step is null)
            return RedirectToAction(nameof(Step), new { guestCheckInId = vm.GuestCheckInId, order = 1 });

        // A guest cannot jump ahead of the step they are on.
        if (vm.StepOrder > checkIn.CurrentStep)
            return RedirectToAction(nameof(Step), new { guestCheckInId = vm.GuestCheckInId, order = checkIn.CurrentStep });

        if (step.Type == "Acknowledge" && !vm.Acknowledged)
        {
            ModelState.AddModelError(nameof(vm.Acknowledged), "Please confirm this step to continue.");
            return View(model);
        }

        if (step.Type == "Document")
        {
            var hasDocument = await _db.GuestDocuments
                .AnyAsync(d => d.GuestCheckInId == vm.GuestCheckInId && d.DocumentType == step.DocumentType);

            if (!hasDocument)
            {
                ModelState.AddModelError(string.Empty, "Please upload the requested document before continuing.");
                return View(model);
            }
        }

        var nextStep = steps.FirstOrDefault(s => s.Order == vm.StepOrder + 1);
        checkIn.CurrentStep = nextStep?.Order ?? vm.StepOrder;
        if (checkIn.Status == CheckInStatus.Pending)
            checkIn.Status = CheckInStatus.InProgress;

        await _db.SaveChangesAsync();

        if (nextStep is null)
        {
            TempData["Toast"] = "All steps are complete. Submit your check-in for the host to verify.";
            return RedirectToAction(nameof(Submit), new { guestCheckInId = vm.GuestCheckInId });
        }

        return RedirectToAction(nameof(Step), new { guestCheckInId = vm.GuestCheckInId, order = nextStep.Order });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(GuestDocumentUploadViewModel vm)
    {
        var userId = CurrentUserId;

        var checkIn = await _db.GuestCheckIns
            .Include(c => c.Booking)
            .FirstOrDefaultAsync(c => c.Id == vm.GuestCheckInId && c.Booking.GuestId == userId);

        if (checkIn is null)
            return NotFound();

        if (checkIn.Status != CheckInStatus.InProgress ||
            checkIn.Booking.Status != BookingStatus.Approved)
        {
            TempData["Error"] = "Documents can only be uploaded during an active check-in.";
            return RedirectToAction("Details", "Bookings", new { id = checkIn.BookingId });
        }

        if (!ModelState.IsValid)
        {
            var model = await BuildStepViewModelAsync(vm.GuestCheckInId, checkIn.CurrentStep);
            if (model is null) return NotFound();
            ModelState.AddModelError(nameof(vm.File), ModelState[string.Empty]?.Errors.FirstOrDefault()?.ErrorMessage ?? "Please choose a file.");
            return View(model);
        }

        string fileName;
        try
        {
            fileName = await _storage.SavePrivateDocumentAsync(vm.File);
        }
        catch (InvalidDataException ex)
        {
            var model = await BuildStepViewModelAsync(vm.GuestCheckInId, checkIn.CurrentStep);
            if (model is null)
                return NotFound();

            ModelState.AddModelError(nameof(vm.File), ex.Message);
            return View("Step", model);
        }

        try
        {
            _db.GuestDocuments.Add(new GuestDocument
            {
                GuestCheckInId = vm.GuestCheckInId,
                DocumentType = vm.DocumentType,
                FileName = fileName,
                OriginalFileName = Path.GetFileName(vm.File.FileName),
                Status = DocumentStatus.Pending,
                UploadedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await _logger.LogAsync(userId, $"Uploaded a {vm.DocumentType} document", ActivityType.CheckIn);
        }
        catch (Exception ex)
        {
            _storage.DeletePrivate(fileName);
            _applicationLogger.LogError(ex, "Could not save uploaded guest document for check-in {GuestCheckInId}.", vm.GuestCheckInId);
            throw;
        }

        TempData["Toast"] = $"{vm.DocumentType} uploaded. The host will verify it shortly.";
        return RedirectToAction(nameof(Step), new { guestCheckInId = vm.GuestCheckInId, order = checkIn.CurrentStep });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int guestCheckInId)
    {
        var userId = CurrentUserId;

        var checkIn = await _db.GuestCheckIns
            .Include(c => c.Booking).ThenInclude(b => b.Property)
            .Include(c => c.Documents)
            .Include(c => c.CheckInProcess)
            .FirstOrDefaultAsync(c => c.Id == guestCheckInId && c.Booking.GuestId == userId);

        if (checkIn is null)
            return NotFound();

        if (checkIn.Status != CheckInStatus.InProgress ||
            checkIn.Booking.Status != BookingStatus.Approved)
        {
            TempData["Error"] = "This check-in is not ready to be submitted.";
            return RedirectToAction("Details", "Bookings", new { id = checkIn.BookingId });
        }

        var lastStepOrder = CheckInStepReader.DeserializeSteps(checkIn.CheckInProcess.StepsJson)
            .Select(step => step.Order)
            .DefaultIfEmpty(0)
            .Max();
        if (checkIn.CurrentStep < lastStepOrder)
        {
            TempData["Error"] = "Complete all check-in steps before submitting.";
            return RedirectToAction(nameof(Step), new { guestCheckInId, order = checkIn.CurrentStep });
        }

        var steps = CheckInStepReader.DeserializeSteps(checkIn.CheckInProcess.StepsJson);
        var required = steps.Where(s => s.Type == "Document" && !string.IsNullOrEmpty(s.DocumentType)).ToList();

        foreach (var step in required)
        {
            if (!checkIn.Documents.Any(d =>
                    d.DocumentType == step.DocumentType &&
                    d.Status is DocumentStatus.Pending or DocumentStatus.Approved))
            {
                TempData["Error"] = $"Please upload the '{step.DocumentType}' document before submitting.";
                return RedirectToAction(nameof(Step), new { guestCheckInId, order = step.Order });
            }
        }

        checkIn.Status = CheckInStatus.Completed;
        await _db.SaveChangesAsync();

        await _logger.LogAsync(userId, $"Submitted check-in for booking #{checkIn.BookingId}", ActivityType.CheckIn);
        await _notifications.NotifyAsync(
            checkIn.Booking.Property.HostId,
            NotificationType.CheckIn,
            "Check-in submitted",
            $"A guest submitted their check-in documents for {checkIn.Booking.Property.Title}.",
            "/HostCheckIn/Submissions");

        TempData["Toast"] = "Check-in submitted. The host will verify your documents shortly.";
        return RedirectToAction("Details", "Bookings", new { id = checkIn.BookingId });
    }

    private async Task<GuestCheckInViewModel?> BuildStepViewModelAsync(int guestCheckInId, int order)
    {
        var userId = CurrentUserId;

        var checkIn = await _db.GuestCheckIns
            .Include(c => c.Booking).ThenInclude(b => b.Property).ThenInclude(p => p.Images)
            .Include(c => c.Booking).ThenInclude(b => b.Property).ThenInclude(p => p.Host)
            .Include(c => c.Documents)
            .Include(c => c.CheckInProcess)
            .FirstOrDefaultAsync(c => c.Id == guestCheckInId && c.Booking.GuestId == userId);

        if (checkIn is null)
            return null;

        var steps = CheckInStepReader.DeserializeSteps(checkIn.CheckInProcess.StepsJson);
        var primary = checkIn.Booking.Property.Images.FirstOrDefault(i => i.IsPrimary) ?? checkIn.Booking.Property.Images.FirstOrDefault();

        return new GuestCheckInViewModel
        {
            GuestCheckInId = checkIn.Id,
            BookingId = checkIn.BookingId,
            PropertyTitle = checkIn.Booking.Property.Title,
            PropertyAddress = checkIn.Booking.Property.Address,
            PropertyImageUrl = primary?.ImageUrl,
            CheckInTitle = checkIn.CheckInProcess.Title,
            Status = checkIn.Status,
            CurrentStep = order,
            TotalSteps = steps.Count,
            Steps = steps,
            Documents = checkIn.Documents
                .OrderBy(d => d.UploadedAt)
                .Select(d => new GuestDocumentViewModel
                {
                    Id = d.Id,
                    DocumentType = d.DocumentType,
                    OriginalFileName = d.OriginalFileName ?? d.FileName,
                    Status = d.Status,
                    UploadedAt = d.UploadedAt
                })
                .ToList()
        };
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
