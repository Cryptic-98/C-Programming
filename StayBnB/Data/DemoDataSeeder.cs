using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StayBnB.Models.Entities;
using StayBnB.Models.Enums;

namespace StayBnB.Data;

/// <summary>
/// Seeds a realistic South African demo dataset so every dashboard has content the
/// moment the application starts: amenities, a host, a guest, an admin, listings,
/// bookings, messages, notifications and activity log entries.
/// </summary>
public static class DemoDataSeeder
{
    public const string HostEmail = "host@staybnb.co.za";
    public const string GuestEmail = "guest@staybnb.co.za";
    public const string AdminEmail = "admin@staybnb.co.za";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedAmenitiesAsync(db);

        if (await db.HostProperties.AnyAsync())
            return; // Already seeded.

        var host = await EnsureUserAsync(userManager, configuration, HostEmail, "Host@2026", "Sarah", "Mbeki", RoleSeeder.HostRole);
        var guest = await EnsureUserAsync(userManager, configuration, GuestEmail, "Guest@2026", "Thandi", "Ndlovu", RoleSeeder.GuestRole);
        var admin = await EnsureUserAsync(userManager, configuration, AdminEmail, "Admin@2026", "Nomsa", "Khumalo", RoleSeeder.AdminRole);

        var amenities = await db.Amenities.ToListAsync();

        var properties = new List<HostProperty>
        {
            New("Pretoria Apartment in Arcadia", "A bright, secure two-bedroom apartment in the heart of Arcadia with a shaded balcony, fibre WiFi and easy access to the Gautrain station. Perfect for a weekend in the capital.", 1250m, 350m, "221 Church Street", "Pretoria", "Gauteng", PropertyType.Apartment, 4, 2, "/img/properties/pretoria-apartment.jpg", host.Id, amenities, new[] { "/img/properties/interior-living.jpg", "/img/properties/kitchen-dining.jpg" }),
            New("Sea View Villa in Camps Bay", "A four-bedroom villa with uninterrupted Atlantic views, an indoor braai and a heated pool. Ten minutes from the V&A Waterfront, with a dedicated concierge.", 4800m, 650m, "12 Victoria Road", "Cape Town", "Western Cape", PropertyType.Villa, 8, 4, "/img/properties/capetown-villa.jpg", host.Id, amenities, new[] { "/img/properties/pool-deck.jpg", "/img/properties/interior-bedroom.jpg" }),
            New("Garden Cottage in Berea", "A charming garden cottage with a wraparound porch, indigenous garden and a wood-fired braai. Walk to the botanic gardens and the Sunday food market.", 980m, 250m, "41 Marwick Road", "Durban", "KwaZulu-Natal", PropertyType.Cottage, 2, 1, "/img/properties/durban-cottage.jpg", host.Id, amenities, new[] { "/img/properties/interior-living.jpg" }),
            New("City Studio in Braamfontein", "A compact, well-designed studio in a converted 1930s building. Work desk, load-shedding backup and a rooftop view over the city skyline.", 1100m, 200m, "8 De Korte Street", "Johannesburg", "Gauteng", PropertyType.Studio, 2, 1, "/img/properties/joburg-studio.jpg", host.Id, amenities, new[] { "/img/properties/kitchen-dining.jpg" }),
            New("Family House in Stellenbosch", "A spacious family home with a swimming pool, vineyard views and a fully equipped kitchen. Ten minutes from the town square and the Stellenbosch wine estates.", 1850m, 400m, "7 Dorp Street", "Stellenbosch", "Western Cape", PropertyType.House, 8, 4, "/img/properties/stellenbosch-house.jpg", host.Id, amenities, new[] { "/img/properties/pool-deck.jpg", "/img/properties/interior-bedroom.jpg", "/img/properties/kitchen-dining.jpg" }),
            New("Boutique Hotel Stay in Knysna", "A boutique hotel room in the heart of Knysna lagoon. Breakfast included, secure parking and a rooftop terrace over the water.", 1450m, 180m, "3 Thesen Road", "Knysna", "Western Cape", PropertyType.Hotel, 2, 1, "/img/properties/knysna-hotel.jpg", host.Id, amenities, new[] { "/img/properties/interior-living.jpg" }),
            New("Beach Cabin in Pearl Beach", "A timber cabin on the dunes at Pearl Beach. Braai area, outdoor shower and a path straight to the sand. The perfect quiet escape.", 1320m, 220m, "5 Ocean View Road", "Gqeberha", "Eastern Cape", PropertyType.Cabin, 4, 2, "/img/properties/pearl-beach-cabin.jpg", host.Id, amenities, new[] { "/img/properties/interior-living.jpg" }),
            New("Guesthouse in Franschhoek", "A six-room guesthouse on a wine farm, with a pool, mountain views and breakfast made from the garden. Tours to the surrounding estates can be arranged.", 1650m, 300m, "18 Main Road", "Franschhoek", "Western Cape", PropertyType.Guesthouse, 10, 6, "/img/properties/franschhoek-guesthouse.jpg", host.Id, amenities, new[] { "/img/properties/interior-bedroom.jpg", "/img/properties/pool-deck.jpg" }),
            New("Family Home in Soweto", "A generous family home with a large garden, trampoline and secure parking. Twenty minutes from Mapungubwe and close to the heritage sites.", 950m, 180m, "23 Makhulong Street", "Soweto", "Gauteng", PropertyType.House, 6, 3, "/img/properties/soweto-family.jpg", host.Id, amenities, new[] { "/img/properties/kitchen-dining.jpg" }),
            New("Loft in Bloemfontein", "A converted warehouse loft with high ceilings and original brickwork. Central Bloemfontein, easy walk to the hospital and the botanical garden.", 880m, 200m, "14 President Brand Street", "Bloemfontein", "Free State", PropertyType.Studio, 3, 1, "/img/properties/bloemfontein-loft.jpg", host.Id, amenities, new[] { "/img/properties/interior-living.jpg" }),
            New("Beach House in Hermanus", "A four-bedroom beach house right on the Hermanus cliff path, with a sea-view deck and a wood stove for winter evenings.", 2400m, 450m, "9 Cliff Road", "Hermanus", "Western Cape", PropertyType.House, 8, 4, "/img/properties/hermanus-beach.jpg", host.Id, amenities, new[] { "/img/properties/interior-bedroom.jpg", "/img/properties/pool-deck.jpg" }),
            New("Lodge in George", "A timber-and-stone lodge at the foot of the Outeniqua mountains, with a fireside lounge and a five-minute drive to the Wilderness beach.", 1720m, 320m, "2 Mountain View Lane", "George", "Western Cape", PropertyType.Cabin, 6, 3, "/img/properties/george-lodge.jpg", host.Id, amenities, new[] { "/img/properties/interior-living.jpg", "/img/properties/kitchen-dining.jpg" }),
            New("Beach Cottage in Ballito", "A breezy cottage a short walk from Compensation Beach, with a covered veranda, outdoor shower and a braai for long summer evenings on the KwaZulu-Natal north coast.", 1480m, 280m, "27 Compensation Beach Road", "Ballito", "KwaZulu-Natal", PropertyType.Cottage, 6, 3, "/img/properties/ballito-cottage.jpg", host.Id, amenities, new[] { "/img/properties/interior-living.jpg", "/img/properties/kitchen-dining.jpg" }),
            New("Mountain Cabin in Dullstroom", "A cosy log cabin among the trout dams and highland trails, with a wood-burning fireplace, sheltered deck and easy access to the village restaurants.", 1150m, 220m, "5 Teding van Berkhout Street", "Dullstroom", "Mpumalanga", PropertyType.Cabin, 4, 2, "/img/properties/dullstroom-cabin.jpg", host.Id, amenities, new[] { "/img/properties/interior-bedroom.jpg", "/img/properties/interior-living.jpg" }),
            New("Skyline Penthouse in Umhlanga", "A light-filled penthouse with floor-to-ceiling windows, a private balcony and ocean views. Walk to the promenade, restaurants and the Umhlanga lighthouse.", 2350m, 380m, "14 Lagoon Drive", "Umhlanga", "KwaZulu-Natal", PropertyType.Apartment, 4, 2, "/img/properties/umhlanga-penthouse.jpg", host.Id, amenities, new[] { "/img/properties/interior-living.jpg", "/img/properties/pool-deck.jpg" })
        };

        db.HostProperties.AddRange(properties);
        await db.SaveChangesAsync();

        // ---- Bookings in every state of the workflow -------------------------------
        var today = DateTime.Today;

        var pending = NewBooking(properties[0], guest.Id, today.AddDays(14), today.AddDays(17), 2, BookingStatus.Pending);
        var approved = NewBooking(properties[1], guest.Id, today.AddDays(30), today.AddDays(35), 4, BookingStatus.Approved);
        var checkedIn = NewBooking(properties[4], guest.Id, today.AddDays(-1), today.AddDays(4), 6, BookingStatus.CheckedIn);
        var completed = NewBooking(properties[6], guest.Id, today.AddDays(-40), today.AddDays(-33), 2, BookingStatus.Completed);
        var rejected = NewBooking(properties[2], guest.Id, today.AddDays(60), today.AddDays(62), 2, BookingStatus.Rejected);

        db.Bookings.AddRange(pending, approved, checkedIn, completed, rejected);
        await db.SaveChangesAsync();

        db.Payments.AddRange(
            new Payment { BookingId = pending.Id, Amount = pending.TotalPrice, Status = PaymentStatus.Pending, CreatedAt = today },
            new Payment { BookingId = approved.Id, Amount = approved.TotalPrice, Status = PaymentStatus.Paid, CreatedAt = today },
            new Payment { BookingId = checkedIn.Id, Amount = checkedIn.TotalPrice, Status = PaymentStatus.Paid, CreatedAt = today.AddDays(-1) },
            new Payment { BookingId = completed.Id, Amount = completed.TotalPrice, Status = PaymentStatus.Paid, CreatedAt = today.AddDays(-40) });

        // ---- Reviews ---------------------------------------------------------------
        db.Reviews.AddRange(
            new Review { PropertyId = properties[4].Id, ReviewerId = guest.Id, Rating = 5, Comment = "A wonderful stay. The pool and the mountain view were exactly what the photos promised, and check-in took two minutes.", CreatedAt = today.AddDays(-30) },
            new Review { PropertyId = properties[1].Id, ReviewerId = admin.Id, Rating = 5, Comment = "Unbeatable view and the concierge sorted our dinner reservations within minutes. Will book again.", CreatedAt = today.AddDays(-21) },
            new Review { PropertyId = properties[6].Id, ReviewerId = guest.Id, Rating = 4, Comment = "Beautiful cabin right on the dunes. The braai needs a little attention but the location is unbeatable.", CreatedAt = today.AddDays(-32) });

        // ---- Wishlist --------------------------------------------------------------
        db.WishlistItems.AddRange(
            new WishlistItem { UserId = guest.Id, PropertyId = properties[8].Id, CreatedAt = today.AddDays(-5) },
            new WishlistItem { UserId = guest.Id, PropertyId = properties[10].Id, CreatedAt = today.AddDays(-2) });

        // ---- Check-in process + guest check-in ------------------------------------
        var checkInProcess = new CheckInProcess
        {
            PropertyId = properties[4].Id,
            Title = "Stellenbosch house check-in",
            CreatedAt = today.AddDays(-20),
            StepsJson = System.Text.Json.JsonSerializer.Serialize(new List<CheckInStepDto>
            {
                new() { Order = 1, Type = "Acknowledge", Title = "Booking confirmed", Instructions = "Please confirm you have read the house rules." },
                new() { Order = 2, Type = "Document", Title = "Upload identification", Instructions = "Please upload your ID or passport so the host can verify your identity.", DocumentType = "Passport" },
                new() { Order = 3, Type = "Text", Title = "Review property information", Instructions = "Parking is behind the house, gate code 4821. The braai is stocked on request." },
                new() { Order = 4, Type = "Acknowledge", Title = "Complete check-in", Instructions = "Once the host has verified your documents you are all set." }
            })
        };
        db.CheckInProcesses.Add(checkInProcess);
        await db.SaveChangesAsync();

        var guestCheckIn = new GuestCheckIn
        {
            BookingId = checkedIn.Id,
            CheckInProcessId = checkInProcess.Id,
            Status = CheckInStatus.Completed,
            CurrentStep = 4,
            CreatedAt = today.AddDays(-1)
        };
        db.GuestCheckIns.Add(guestCheckIn);
        await db.SaveChangesAsync();

        db.GuestDocuments.AddRange(
            new GuestDocument { GuestCheckInId = guestCheckIn.Id, DocumentType = "Passport", FileName = "demo-passport.jpg", OriginalFileName = "passport.jpg", Status = DocumentStatus.Approved, UploadedAt = today.AddHours(-20) },
            new GuestDocument { GuestCheckInId = guestCheckIn.Id, DocumentType = "ID", FileName = "demo-id.jpg", OriginalFileName = "id-card.jpg", Status = DocumentStatus.Approved, UploadedAt = today.AddHours(-19) });

        // ---- Messages --------------------------------------------------------------
        db.Messages.AddRange(
            new Message { SenderId = guest.Id, ReceiverId = host.Id, Content = "Hi Sarah, is the Cape Town villa available over the long weekend?", Timestamp = today.AddHours(-6), IsRead = true },
            new Message { SenderId = host.Id, ReceiverId = guest.Id, Content = "Hi Thandi, yes it is. I have sent you a booking request for those dates.", Timestamp = today.AddHours(-5), IsRead = true },
            new Message { SenderId = guest.Id, ReceiverId = host.Id, Content = "Thank you! I have submitted the request. Can we check in late on the Friday evening?", Timestamp = today.AddHours(-3), IsRead = false });

        // ---- Notifications ----------------------------------------------------------
        db.Notifications.AddRange(
            new Notification { UserId = guest.Id, Title = "Booking approved", Message = "Your booking of Sea View Villa in Camps Bay was approved.", LinkUrl = $"/Bookings/Details/{approved.Id}", Type = NotificationType.Booking, IsRead = false, CreatedAt = today.AddHours(-4) },
            new Notification { UserId = guest.Id, Title = "New message", Message = "Sarah Mbeki sent you a message.", LinkUrl = "/Messages/Thread", Type = NotificationType.Message, IsRead = false, CreatedAt = today.AddHours(-3) },
            new Notification { UserId = host.Id, Title = "New booking request", Message = "Thandi Ndlovu requested Sea View Villa in Camps Bay.", LinkUrl = "/HostBookings/Index", Type = NotificationType.Booking, IsRead = false, CreatedAt = today.AddHours(-5) },
            new Notification { UserId = host.Id, Title = "Check-in submitted", Message = "Thandi Ndlovu submitted their check-in documents.", LinkUrl = "/HostCheckIn/Submissions", Type = NotificationType.CheckIn, IsRead = false, CreatedAt = today.AddHours(-2) },
            new Notification { UserId = admin.Id, Title = "Application awaiting review", Message = "A new host application is waiting for review.", LinkUrl = "/ApplicationReview/Index", Type = NotificationType.System, IsRead = false, CreatedAt = today.AddHours(-1) });

        // ---- Activity log ----------------------------------------------------------
        db.ActivityLogs.AddRange(
            new ActivityLog { UserId = guest.Id, Action = "Registered an account", ActivityType = ActivityType.Authentication, CreatedAt = today.AddDays(-60) },
            new ActivityLog { UserId = guest.Id, Action = "Logged in", ActivityType = ActivityType.Authentication, CreatedAt = today.AddHours(-7) },
            new ActivityLog { UserId = host.Id, Action = "Role changed Guest -> Host", ActivityType = ActivityType.RoleChange, CreatedAt = today.AddDays(-58) },
            new ActivityLog { UserId = host.Id, Action = "Approved host application #1", ActivityType = ActivityType.Application, CreatedAt = today.AddDays(-58) },
            new ActivityLog { UserId = admin.Id, Action = "Logged in", ActivityType = ActivityType.Authentication, CreatedAt = today.AddHours(-1) },
            new ActivityLog { UserId = guest.Id, Action = $"Booking #{pending.Id} created", ActivityType = ActivityType.Booking, CreatedAt = today.AddHours(-6) },
            new ActivityLog { UserId = host.Id, Action = $"Booking #{approved.Id} status changed to Approved", ActivityType = ActivityType.Booking, CreatedAt = today.AddHours(-4) });

        await db.SaveChangesAsync();
    }

    private static HostProperty New(
        string title, string description, decimal price, decimal cleaningFee, string address, string city, string country,
        PropertyType type, int maxGuests, int bedrooms, string image, string hostId,
        List<Amenity> amenities, string[] extraImages)
    {
        var property = new HostProperty
        {
            Title = title,
            Description = description,
            PricePerNight = price,
            CleaningFee = cleaningFee,
            Address = address,
            City = city,
            Country = country,
            PropertyType = type,
            MaxGuests = maxGuests,
            Bedrooms = bedrooms,
            IsActive = true,
            HostId = hostId,
            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(5, 90))
        };

        property.Amenities = amenities.Take(6).ToList();
        property.Images.Add(new PropertyImage { ImageUrl = image, IsPrimary = true });
        foreach (var extra in extraImages)
            property.Images.Add(new PropertyImage { ImageUrl = extra, IsPrimary = false });

        return property;
    }

    private static Booking NewBooking(HostProperty property, string guestId, DateTime checkIn, DateTime checkOut, int guests, BookingStatus status)
    {
        var nights = (checkOut - checkIn).Days;
        var subtotal = property.PricePerNight * nights;
        var serviceFee = Math.Round(subtotal * 0.12m, 2);

        return new Booking
        {
            PropertyId = property.Id,
            GuestId = guestId,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NumberOfGuests = guests,
            PricePerNight = property.PricePerNight,
            CleaningFee = property.CleaningFee,
            ServiceFee = serviceFee,
            TotalPrice = subtotal + property.CleaningFee + serviceFee,
            Status = status,
            CreatedAt = checkIn.AddDays(-10)
        };
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager, IConfiguration configuration,
        string email, string defaultPassword, string firstName, string lastName, string role)
    {
        var password = configuration[$"DemoUsers:{role}:Password"] ?? defaultPassword;

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 90))
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Could not seed {email}: " + string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);

        return user;
    }

    private static async Task SeedAmenitiesAsync(ApplicationDbContext db)
    {
        if (await db.Amenities.AnyAsync())
            return;

        db.Amenities.AddRange(
            new Amenity { Name = "WiFi", IconClass = "bi bi-wifi" },
            new Amenity { Name = "Pool", IconClass = "bi bi-water" },
            new Amenity { Name = "Parking", IconClass = "bi bi-car-front" },
            new Amenity { Name = "Air Conditioning", IconClass = "bi bi-snow" },
            new Amenity { Name = "Kitchen", IconClass = "bi bi-cup-hot" },
            new Amenity { Name = "TV", IconClass = "bi bi-tv" },
            new Amenity { Name = "Swimming Pool", IconClass = "bi bi-water" },
            new Amenity { Name = "Braai", IconClass = "bi bi-fire" },
            new Amenity { Name = "Garden", IconClass = "bi bi-tree" },
            new Amenity { Name = "Load-shedding backup", IconClass = "bi bi-battery-charging" },
            new Amenity { Name = "Laundry", IconClass = "bi bi-basket" },
            new Amenity { Name = "Balcony", IconClass = "bi bi-door-open" });

        await db.SaveChangesAsync();
    }
}
