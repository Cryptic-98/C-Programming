using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnB.Data;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;
using StayBnB.Models.ViewModels;
using StayBnB.Services;

namespace StayBnB.Controllers.Host;

[Authorize(Roles = RoleSeeder.HostRole)]
public class HostCheckInController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IActivityLogger _logger;
    private readonly INotificationService _notifications;
    private readonly IBookingService _bookings;
    private readonly ILogger<HostCheckInController> _applicationLogger;

    public HostCheckInController(
        ApplicationDbContext db,
        IFileStorageService storage,
        IActivityLogger logger,
        INotificationService notifications,
        IBookingService bookings,
        ILogger<HostCheckInController> applicationLogger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
        _notifications = notifications;
        _bookings = bookings;
        _applicationLogger = applicationLogger;
    }

    [HttpGet]
    public async Task<IActionResult> Configure(int propertyId)
    {
        var property = await _db.HostProperties
            .Include(p => p.CheckInProcess)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == CurrentUserId);

        if (property is null)
            return NotFound();

        var vm = new CheckInProcessViewModel
        {
            PropertyId = property.Id,
            PropertyTitle = property.Title,
            Title = property.CheckInProcess?.Title ?? $"{property.Title} check-in"
        };

        if (property.CheckInProcess is not null)
            vm.Steps = CheckInStepReader.DeserializeSteps(property.CheckInProcess.StepsJson);

        if (vm.Steps.Count == 0)
        {
            vm.Steps = new List<CheckInStepDto>
            {
                new() { Order = 1, Type = "Acknowledge", Title = "Booking confirmed", Instructions = "Please confirm you have read the house rules." },
                new() { Order = 2, Type = "Document", Title = "Upload identification", Instructions = "Please upload your ID or passport.", DocumentType = "Passport" },
                new() { Order = 3, Type = "Text", Title = "Review property information", Instructions = "Add arrival and access information here." }
            };
        }

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Configure(CheckInProcessViewModel vm)
    {
        var property = await _db.HostProperties
            .Include(p => p.CheckInProcess)
            .FirstOrDefaultAsync(p => p.Id == vm.PropertyId && p.HostId == CurrentUserId);

        if (property is null)
            return NotFound();

        vm.Steps = vm.Steps.Where(s => !string.IsNullOrWhiteSpace(s.Title)).OrderBy(s => s.Order).ToList();
        for (var i = 0; i < vm.Steps.Count; i++)
            vm.Steps[i].Order = i + 1;

        if (vm.Steps.Count == 0)
            ModelState.AddModelError(nameof(vm.Steps), "Add at least one step.");

        if (!vm.Steps.Any(step => step.Type == "Document"))
            ModelState.AddModelError(nameof(vm.Steps), "Add at least one required ID or passport document step.");

        if (vm.Steps.Any(step =>
                step.Type == "Document" &&
                step.DocumentType is not ("ID" or "Passport")))
            ModelState.AddModelError(nameof(vm.Steps), "Document steps must require either an ID or Passport.");

        if (!ModelState.IsValid)
            return View(vm);

        var json = System.Text.Json.JsonSerializer.Serialize(vm.Steps);

        if (property.CheckInProcess is null)
        {
            property.CheckInProcess = new CheckInProcess
            {
                PropertyId = property.Id,
                Title = vm.Title.Trim(),
                StepsJson = json,
                CreatedAt = DateTime.UtcNow
            };
            _db.CheckInProcesses.Add(property.CheckInProcess);
        }
        else
        {
            property.CheckInProcess.Title = vm.Title.Trim();
            property.CheckInProcess.StepsJson = json;
        }

        await _db.SaveChangesAsync();

        await _logger.LogAsync(CurrentUserId, $"Updated the check-in process for '{property.Title}'", ActivityType.Property);

        TempData["Toast"] = "Your check-in process has been saved.";
        return RedirectToAction(nameof(Configure), new { propertyId = property.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Submissions()
    {
        var userId = CurrentUserId;

        var submissions = await _db.GuestCheckIns
            .Include(c => c.Booking).ThenInclude(b => b.Property)
            .Include(c => c.Booking).ThenInclude(b => b.Guest)
            .Include(c => c.CheckInProcess)
            .Include(c => c.Documents)
            .Where(c => c.Booking.Property.HostId == userId &&
                        c.Status != CheckInStatus.Pending &&
                        c.Status != CheckInStatus.InProgress)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var vm = submissions.Select(c => new HostCheckInSubmissionViewModel
        {
            GuestCheckInId = c.Id,
            BookingId = c.BookingId,
            PropertyTitle = c.Booking.Property.Title,
            GuestName = c.Booking.Guest?.FullName ?? "Guest",
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            Steps = CheckInStepReader.DeserializeSteps(c.CheckInProcess.StepsJson),
            Documents = c.Documents
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
        }).ToList();

        return View(vm);
    }

    /// <summary>Streams a guest document. Only the host who owns the property may open it.</summary>
    [HttpGet]
    public async Task<IActionResult> ViewDocument(int documentId)
    {
        var userId = CurrentUserId;

        var document = await _db.GuestDocuments
            .Include(d => d.GuestCheckIn).ThenInclude(c => c.Booking).ThenInclude(b => b.Property)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document is null || document.GuestCheckIn.Booking.Property.HostId != userId)
            return NotFound();

        if (document.GuestCheckIn.Status != CheckInStatus.Completed ||
            document.Status != DocumentStatus.Pending)
        {
            TempData["Error"] = "This document is not awaiting review.";
            return RedirectToAction(nameof(Submissions));
        }

        if (!_storage.PrivateFileExists(document.FileName))
            return NotFound("The stored document could not be found.");

        var stream = await _storage.OpenPrivateAsync(document.FileName);
        var contentType = Path.GetExtension(document.FileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return File(stream, contentType, document.OriginalFileName ?? document.FileName);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(DocumentVerificationViewModel vm)
    {
        var userId = CurrentUserId;

        var document = await _db.GuestDocuments
            .Include(d => d.GuestCheckIn).ThenInclude(c => c.Booking).ThenInclude(b => b.Property)
            .Include(d => d.GuestCheckIn).ThenInclude(c => c.Documents)
            .FirstOrDefaultAsync(d => d.Id == vm.DocumentId);

        if (document is null || document.GuestCheckIn.Booking.Property.HostId != userId)
            return NotFound();

        if (!vm.Approve && string.IsNullOrWhiteSpace(vm.Reason))
        {
            TempData["Error"] = "Please give a reason when rejecting a document.";
            return RedirectToAction(nameof(Submissions));
        }

        var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync()
            : null;
        try
        {
            document.Status = vm.Approve ? DocumentStatus.Approved : DocumentStatus.Rejected;

            var checkIn = document.GuestCheckIn;
            var guestId = checkIn.Booking.GuestId;
            var bookingId = checkIn.BookingId;
            var propertyTitle = checkIn.Booking.Property.Title;

            if (!vm.Approve)
            {
                checkIn.Status = CheckInStatus.Rejected;

                foreach (var other in checkIn.Documents.Where(d => d.Status == DocumentStatus.Pending))
                    other.Status = DocumentStatus.Rejected;

                await _db.SaveChangesAsync();

                await _logger.LogAsync(userId, $"Rejected a document for booking #{bookingId}", ActivityType.CheckIn);
                await _notifications.NotifyAsync(guestId, NotificationType.CheckIn, "Document rejected",
                    vm.Reason, $"/Bookings/Details/{bookingId}");

                if (transaction is not null)
                    await transaction.CommitAsync();

                TempData["Toast"] = "The document was rejected and the guest was notified.";
                return RedirectToAction(nameof(Submissions));
            }

            if (checkIn.Documents.All(d => d.Status == DocumentStatus.Approved))
            {
                checkIn.Status = CheckInStatus.Completed;
                await _db.SaveChangesAsync();

                var statusResult = await _bookings.ChangeStatusAsync(bookingId, BookingStatus.CheckedIn, userId);
                if (!statusResult.Success)
                {
                    if (transaction is not null)
                        await transaction.RollbackAsync();

                    TempData["Error"] = statusResult.Message;
                    return RedirectToAction(nameof(Submissions));
                }

                await _logger.LogAsync(userId, $"Verified all documents for booking #{bookingId}", ActivityType.CheckIn);
                await _notifications.NotifyAsync(guestId, NotificationType.CheckIn, "You are checked in",
                    $"Your documents for {propertyTitle} were verified. Enjoy your stay.",
                    $"/Bookings/Details/{bookingId}");

                if (transaction is not null)
                    await transaction.CommitAsync();

                TempData["Toast"] = "Documents verified. The guest is now checked in.";
            }
            else
            {
                await _db.SaveChangesAsync();
                await _logger.LogAsync(userId, $"Approved a document for booking #{bookingId}", ActivityType.CheckIn);

                if (transaction is not null)
                    await transaction.CommitAsync();

                TempData["Toast"] = "Document approved.";
            }

            return RedirectToAction(nameof(Submissions));
        }
        catch (Exception ex)
        {
            if (transaction is not null)
                await transaction.RollbackAsync();

            _applicationLogger.LogError(ex, "Could not verify document {DocumentId}.", vm.DocumentId);
            TempData["Error"] = "The document could not be updated. Please try again.";
            return RedirectToAction(nameof(Submissions));
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
