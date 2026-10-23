using System.Data;
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
public class BookingsController : Controller
{
    private const int MaxNights = 90;

    private readonly ApplicationDbContext _db;
    private readonly IBookingService _bookingService;
    private readonly IPricingService _pricing;
    private readonly INotificationService _notifications;
    private readonly IActivityLogger _logger;
    private readonly ILogger<BookingsController> _applicationLogger;

    public BookingsController(
        ApplicationDbContext db,
        IBookingService bookingService,
        IPricingService pricing,
        INotificationService notifications,
        IActivityLogger logger,
        ILogger<BookingsController> applicationLogger)
    {
        _db = db;
        _bookingService = bookingService;
        _pricing = pricing;
        _notifications = notifications;
        _logger = logger;
        _applicationLogger = applicationLogger;
    }

    /// <summary>"My trips" - the guest dashboard.</summary>
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;

        var bookings = await _db.Bookings
            .Include(b => b.Property).ThenInclude(p => p.Images)
            .Include(b => b.Property).ThenInclude(p => p.Host)
            .Include(b => b.GuestCheckIn)
            .Where(b => b.GuestId == userId)
            .OrderByDescending(b => b.CheckInDate)
            .ToListAsync();

        var today = DateTime.Today;
        var upcoming = bookings.Where(b => b.CheckOutDate >= today && b.Status != BookingStatus.Rejected && b.Status != BookingStatus.Cancelled).ToList();

        ViewBag.FirstName = (await _db.Users.FindAsync(userId))?.FirstName ?? "Traveller";
        ViewBag.Upcoming = upcoming.Select(MapItem).ToList();
        var history = bookings
            .Where(b => b.CheckOutDate < today ||
                        b.Status is BookingStatus.Completed or BookingStatus.Rejected or BookingStatus.Cancelled)
            .ToList();
        ViewBag.Past = history.Select(MapItem).ToList();

        var wishlisted = await _db.WishlistItems
            .Include(w => w.Property).ThenInclude(p => p.Images)
            .Include(w => w.Property).ThenInclude(p => p.Reviews)
            .Where(w => w.UserId == userId)
            .Take(4)
            .ToListAsync();

        ViewBag.WishlistPreview = wishlisted.Select(w => HomeController.MapCard(w.Property)).ToList();

        var explore = await _db.HostProperties
            .Where(p => p.IsActive)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
            .ToListAsync();

        ViewBag.Explore = explore.Select(HomeController.MapCard).ToList();

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Create(int propertyId, DateTime? checkIn, DateTime? checkOut, int? guests)
    {
        var property = await _db.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Host)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive);

        if (property is null)
            return NotFound();

        var vm = await BuildRequestAsync(property,
            checkIn ?? DateTime.Today.AddDays(14),
            checkOut ?? DateTime.Today.AddDays(17),
            Math.Clamp(guests ?? 2, 1, property.MaxGuests));

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingRequestViewModel vm)
    {
        var property = await _db.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Host)
            .FirstOrDefaultAsync(p => p.Id == vm.PropertyId && p.IsActive);

        if (property is null)
            return NotFound();

        var userId = CurrentUserId;

        if (property.HostId == userId)
        {
            ModelState.AddModelError(string.Empty, "You cannot book your own listing.");
            return View(await BuildRequestAsync(property, vm.CheckInDate ?? DateTime.Today, vm.CheckOutDate ?? DateTime.Today.AddDays(3), vm.NumberOfGuests ?? 1));
        }

        if (!ModelState.IsValid)
            return View(await BuildRequestAsync(property, vm.CheckInDate ?? DateTime.Today, vm.CheckOutDate ?? DateTime.Today.AddDays(3), vm.NumberOfGuests ?? 1));

        var checkIn = vm.CheckInDate!.Value.Date;
        var checkOut = vm.CheckOutDate!.Value.Date;
        var guests = vm.NumberOfGuests!.Value;

        if (guests > property.MaxGuests)
        {
            ModelState.AddModelError(nameof(vm.NumberOfGuests), $"This property sleeps up to {property.MaxGuests} guests.");
            return View(await BuildRequestAsync(property, checkIn, checkOut, guests));
        }

        if ((checkOut - checkIn).Days > MaxNights)
        {
            ModelState.AddModelError(nameof(vm.CheckOutDate), $"Stays are limited to {MaxNights} nights.");
            return View(await BuildRequestAsync(property, checkIn, checkOut, guests));
        }

        var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        try
        {
            if (await _bookingService.HasOverlapAsync(property.Id, checkIn, checkOut))
            {
                if (transaction is not null)
                    await transaction.RollbackAsync();

                ModelState.AddModelError(string.Empty, "These dates are no longer available. Please choose different dates.");
                return View(await BuildRequestAsync(property, checkIn, checkOut, guests));
            }

            // The server always recalculates the price; nothing posted by the browser is trusted.
            var price = _pricing.Calculate(property, checkIn, checkOut);

            var booking = new Booking
            {
                PropertyId = property.Id,
                GuestId = userId,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                NumberOfGuests = guests,
                PricePerNight = price.PricePerNight,
                CleaningFee = price.CleaningFee,
                ServiceFee = price.ServiceFee,
                TotalPrice = price.TotalPrice,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            booking.Payment = new Payment
            {
                Amount = price.TotalPrice,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            await _logger.LogAsync(userId, $"Booking #{booking.Id} created for {property.Title}", ActivityType.Booking);
            await _notifications.NotifyAsync(
                property.HostId,
                NotificationType.Booking,
                "New booking request",
                $"A guest requested {property.Title} for {checkIn: d MMM} – {checkOut: d MMM yyyy}.",
                "/HostBookings/Index");

            if (transaction is not null)
                await transaction.CommitAsync();

            TempData["Toast"] = "Your booking request has been sent. The host will respond soon.";
            return RedirectToAction(nameof(Details), new { id = booking.Id });
        }
        catch (Exception ex)
        {
            if (transaction is not null)
                await transaction.RollbackAsync();

            _applicationLogger.LogError(ex, "Could not create a booking for user {UserId} and property {PropertyId}.", userId, property.Id);
            ModelState.AddModelError(string.Empty, "Your booking request could not be saved. Please try again.");
            return View(await BuildRequestAsync(property, checkIn, checkOut, guests));
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = CurrentUserId;

        var booking = await _db.Bookings
            .Include(b => b.Property).ThenInclude(p => p.Images)
            .Include(b => b.Property).ThenInclude(p => p.Host)
            .Include(b => b.Payment)
            .Include(b => b.GuestCheckIn)
            .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == userId);

        if (booking is null)
            return NotFound();

        var vm = new BookingDetailsViewModel
        {
            Booking = MapItem(booking),
            PropertyAddress = booking.Property.Address,
            PricePerNight = booking.PricePerNight,
            CleaningFee = booking.CleaningFee,
            ServiceFee = booking.ServiceFee,
            Timeline = BuildTimeline(booking)
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = CurrentUserId;

        var result = await _bookingService.ChangeStatusAsync(id, BookingStatus.Cancelled, userId);
        if (!result.Success)
        {
            if (result.Message == "Booking not found.")
                return NotFound();

            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Toast"] = "Your booking has been cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private List<BookingEventViewModel> BuildTimeline(Booking booking)
    {
        var events = new List<BookingEventViewModel>
        {
            new() { Title = "Request sent", Description = booking.CreatedAt.ToString("d MMM yyyy HH:mm"), Status = BookingStatus.Pending, Done = true }
        };

        if (booking.Status is BookingStatus.Approved or BookingStatus.CheckedIn or BookingStatus.Completed)
            events.Add(new BookingEventViewModel { Title = "Approved by the host", Description = "The host approved your request.", Status = BookingStatus.Approved, Done = true });

        if (booking.Status is BookingStatus.CheckedIn or BookingStatus.Completed)
            events.Add(new BookingEventViewModel { Title = "Checked in", Description = "Your documents were verified and you are checked in.", Status = BookingStatus.CheckedIn, Done = true });

        if (booking.Status is BookingStatus.Completed)
            events.Add(new BookingEventViewModel { Title = "Completed", Description = "Thanks for staying with us.", Status = BookingStatus.Completed, Done = true });

        if (booking.Status is BookingStatus.Rejected or BookingStatus.Cancelled)
            events.Add(new BookingEventViewModel { Title = booking.Status.ToString(), Description = "This request was not completed.", Status = booking.Status, Done = true });

        return events;
    }

    private async Task<BookingRequestViewModel> BuildRequestAsync(HostProperty property, DateTime checkIn, DateTime checkOut, int guests)
    {
        var price = _pricing.Calculate(property, checkIn, checkOut);
        var primary = property.Images.FirstOrDefault(i => i.IsPrimary) ?? property.Images.FirstOrDefault();

        return new BookingRequestViewModel
        {
            PropertyId = property.Id,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NumberOfGuests = guests,
            PricePerNight = price.PricePerNight,
            CleaningFee = price.CleaningFee,
            ServiceFee = price.ServiceFee,
            Nights = price.Nights,
            TotalPrice = price.TotalPrice,
            PropertyTitle = property.Title,
            PropertyCity = property.City,
            PropertyImageUrl = primary?.ImageUrl,
            HostName = property.Host?.FullName ?? "Staybnb host"
        };
    }

    internal static BookingListItemViewModel MapItem(Booking booking)
    {
        var primary = booking.Property.Images.FirstOrDefault(i => i.IsPrimary) ?? booking.Property.Images.FirstOrDefault();

        return new BookingListItemViewModel
        {
            Id = booking.Id,
            PropertyId = booking.PropertyId,
            PropertyTitle = booking.Property.Title,
            PropertyCity = booking.Property.City,
            PropertyImageUrl = primary?.ImageUrl,
            HostName = booking.Property.Host?.FullName ?? "Staybnb host",
            HostId = booking.Property.HostId,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            NumberOfGuests = booking.NumberOfGuests,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            Nights = booking.Nights,
            HasCheckIn = booking.GuestCheckIn is not null,
            GuestCheckInId = booking.GuestCheckIn?.Id,
            CheckInStatus = booking.GuestCheckIn?.Status
        };
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
