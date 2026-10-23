# StayBnB

StayBnB is an ASP.NET Core MVC accommodation marketplace for South African stays. Guests can explore and filter listings, request bookings, manage a wishlist, message hosts, complete a guided check-in and review a property after their stay. Hosts can apply to host, publish and manage listings, handle booking requests, configure check-in steps and verify guest documents. Administrators review host applications and monitor activity, while a SuperAdmin can promote Guests to Admin.

# Features

- Browse a discovery **Explore** page grouped by city and property type, with top-rated and newly listed stays.
- Search the **Stays** page by city, property type, check-in/check-out dates, guest count and price range.
- Landing-page search that passes real city, date and guest filters through to the Stays results.
- Wishlist: add or remove a property as a signed-in Guest.
- Booking requests with server-calculated pricing, date-overlap protection and a guarded status workflow.
- Host applications with admin review. Approval promotes the applicant from **Guest** to **Host**, removes the Guest role and publishes the property in one operation.
- Host dashboard: create, edit, activate/deactivate and delete listings, and upload property images and amenities.
- Host booking management: approve, reject (with a reason sent to the guest), and complete stays.
- Guided guest check-in: ordered steps, private ID/passport uploads, host verification and a check-in status per booking.
- Messaging between guests and hosts, with conversations, unread state, notifications and activity logging.
- Reviews restricted to guests who have checked in at or completed a stay, with one review per guest per property.
- Notifications for bookings, applications, messages and check-in events.
- Admin dashboard, host-application review and a filterable activity log.
- SuperAdmin user management for promoting eligible Guests to Admin.
- Role-based authorization, anti-forgery protection, ownership checks and server-side validation of all prices, dates and status changes.
- Fifteen seeded places across fifteen South African cities for an immediately usable demo.

# Built with:

- Programming Language: C#
- Framework: ASP.NET Core MVC (.NET 10)
- Database: SQL Server through EF Core 10.0.13, with an automatic EF Core in-memory fallback
- Authentication and roles: ASP.NET Core Identity
- Front end: Razor views, plain CSS, Bootstrap Icons, jQuery validation
- File storage: local disk (public property images; private guest documents)
- Testing: xUnit 2.9.2 with the EF Core in-memory provider

# Application areas

1. `HomeController.cs` — landing page, featured stays and status-code pages.
2. `PropertiesController.cs` — Explore discovery hub, filterable/paginated Stays list and property details.
3. `AccountController.cs` — registration, login, logout and profile editing.
4. `WishlistController.cs` — the signed-in Guest's saved properties.
5. `BookingsController.cs` — a Guest's trips, booking requests, details and cancellation.
6. `GuestCheckInController.cs` — the guest's guided check-in steps and document uploads.
7. `ReviewsController.cs` — review eligibility and submission.
8. `MessagesController.cs` — inbox, conversation thread and message composition.
9. `NotificationsController.cs` — notification list, unread count and read actions.
10. `HostApplicationController.cs` — a Guest's host application and its status.
11. `Host/HostDashboardController.cs` — the Host's summary dashboard.
12. `Host/HostPropertiesController.cs` — listing management and property images.
13. `Host/HostBookingsController.cs` — approve, reject and complete bookings.
14. `Host/HostCheckInController.cs` — configure check-in steps and verify uploaded documents.
15. `Admin/AdminDashboardController.cs` — platform totals and admin overview.
16. `Admin/ApplicationReviewController.cs` — review host applications and record decisions.
17. `Admin/ActivityLogController.cs` — filterable audit trail.
18. `Admin/UserManagementController.cs` — SuperAdmin-only Guest-to-Admin promotion.

# Booking and role rules

## Booking price

The stored total is calculated only on the server:

```text
TotalPrice = (PricePerNight × Nights) + CleaningFee + ServiceFee
```

Nights are the whole days between check-in and check-out. Client-supplied totals, prices and fees are ignored.

## Booking states

| From        | Allowed next states                  |
|-------------|--------------------------------------|
| `Pending`   | `Approved`, `Rejected`, `Cancelled`  |
| `Approved`  | `CheckedIn`, `Cancelled`             |
| `CheckedIn` | `Completed`                          |
| `Completed` | —                                    |
| `Rejected`  | —                                    |
| `Cancelled` | —                                    |

- A guest may cancel only their own booking.
- A host may approve or reject only bookings on their own property.
- A booking can move to `CheckedIn` only after the guest has completed check-in and every uploaded document has been approved.
- A booking can move to `Completed` only after the check-out date.

## Date availability

A property is excluded from a date search when it has a `Pending`, `Approved` or `CheckedIn` booking that overlaps the requested window. Overlap means the existing check-in is before the requested check-out and the existing check-out is after the requested check-in. Cancelled and rejected bookings do not block dates, and a stay that ends on the day the next stay begins does not overlap.

## Roles

- New registrations always receive the `Guest` role.
- Approving a host application adds `Host` and removes `Guest`.
- Only a `SuperAdmin` can promote a Guest to `Admin`.
- Admin pages accept `Admin` and `SuperAdmin`; user management is `SuperAdmin` only.

## Reviews

A guest may review a property only when they have a `CheckedIn` or `Completed` booking for it. Each guest may review a property once, the rating must be between 1 and 5, and the comment must contain at least 10 characters.

# Business Logic

1. `PricingService.cs` — the single authoritative booking-price calculation.
2. `BookingService.cs` — date-overlap detection, allowed status transitions, ownership and role checks, notifications and audit entries.
3. `HostApplicationService.cs` — transactional approval/rejection, the Guest-to-Host role swap, property activation and applicant notification.
4. `ReviewService.cs` — review eligibility, validation, persistence and activity logging.
5. `MessagingService.cs` — message persistence, receiver validation, notifications and activity logging.
6. `ActivityLogger.cs` — audit logging and the notification service used across the application.
7. `FileStorageService.cs` — upload validation, public property images and private guest-document storage.

# Data

1. `ApplicationDbContext.cs` — Identity tables plus properties, images, amenities, bookings, payments, reviews, wishlist items, messages, notifications, activity logs, host applications, check-in processes, guest check-ins and guest documents.
2. `Configurations/EntityConfigurations.cs` — entity relationships, constraints, indexes and delete behaviour.
3. `Migrations/` — the EF Core initial migration used for SQL Server.
4. `RoleSeeder.cs` — creates the `Guest`, `Host`, `Admin` and `SuperAdmin` roles and the configured SuperAdmin account.
5. `DemoDataSeeder.cs` — seeds the demo users, properties, bookings, reviews, messages, notifications and check-in data.
6. `database.sql` — the equivalent SQL Server schema for manual database setup.

# Project structure

```text
StayBnB/
│
├── Controllers/
│   ├── AccountController.cs
│   ├── BookingsController.cs
│   ├── GuestCheckInController.cs
│   ├── HomeController.cs
│   ├── HostApplicationController.cs
│   ├── MessagesController.cs
│   ├── NotificationsController.cs
│   ├── PropertiesController.cs
│   ├── ReviewsController.cs
│   ├── WishlistController.cs
│   │
│   ├── Admin/
│   │   ├── ActivityLogController.cs
│   │   ├── AdminDashboardController.cs
│   │   ├── ApplicationReviewController.cs
│   │   └── UserManagementController.cs
│   │
│   └── Host/
│       ├── HostBookingsController.cs
│       ├── HostCheckInController.cs
│       ├── HostDashboardController.cs
│       └── HostPropertiesController.cs
│
├── Data/
│   ├── Configurations/
│   │   └── EntityConfigurations.cs
│   ├── Migrations/
│   │   ├── 20261022204028_InitialCreate.cs
│   │   ├── 20261022204028_InitialCreate.Designer.cs
│   │   └── ApplicationDbContextModelSnapshot.cs
│   ├── ApplicationDbContext.cs
│   ├── ApplicationDbContextFactory.cs
│   ├── DemoDataSeeder.cs
│   └── RoleSeeder.cs
│
├── Models/
│   ├── Entities/
│   │   ├── ApplicationUser.cs
│   │   ├── Booking.cs
│   │   ├── HostApplication.cs
│   │   ├── HostProperty.cs
│   │   └── Messaging.cs
│   ├── Enums/
│   │   └── Enums.cs
│   └── ViewModels/
│       ├── AccountViewModels.cs
│       └── HostViewModels.cs
│
├── Services/
│   ├── ActivityLogger.cs
│   ├── BookingService.cs
│   ├── FileStorageService.cs
│   ├── HostApplicationService.cs
│   ├── MessagingService.cs
│   ├── PricingService.cs
│   └── ReviewService.cs
│
├── Validation/
│   └── ValidationAttributes.cs
│
├── Views/
│   ├── Account/
│   ├── ActivityLog/
│   ├── AdminDashboard/
│   ├── ApplicationReview/
│   ├── Bookings/
│   ├── GuestCheckIn/
│   ├── Home/
│   ├── HostApplication/
│   ├── HostBookings/
│   ├── HostCheckIn/
│   ├── HostDashboard/
│   ├── HostProperties/
│   ├── Messages/
│   ├── Notifications/
│   ├── Properties/
│   ├── Reviews/
│   ├── Shared/
│   ├── UserManagement/
│   ├── Wishlist/
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
│
├── wwwroot/
│   ├── css/
│   │   ├── fonts.css
│   │   └── site.css
│   ├── fonts/
│   ├── img/
│   │   ├── hero.jpg
│   │   └── properties/
│   ├── js/
│   │   └── site.js
│   └── lib/
│       ├── bootstrap-icons/
│       ├── jquery/
│       └── jquery-validation/
│
├── appsettings.json
├── appsettings.Development.json
├── database.sql
├── dummy-accounts.txt
├── instructions.md
├── Program.cs
├── README.md
└── StayBnB.csproj

StayBnB.Tests/
├── BookingServiceTests.cs
├── HostApplicationServiceTests.cs
├── MessagingAndOwnershipTests.cs
├── PropertySearchTests.cs
├── ReviewServiceTests.cs
├── TestHarness.cs
└── StayBnB.Tests.csproj
```

# Configuration

| Setting                              | Default              | Purpose |
|--------------------------------------|----------------------|---------|
| `Database:Provider`                  | `Auto`               | `Auto` tries SQL Server and falls back to in-memory; `SqlServer` and `InMemory` force a provider. |
| `ConnectionStrings:DefaultConnection`| LocalDB `StaybnbDb`  | SQL Server connection used when the provider is relational. |
| `SuperAdmin:Email`                   | `superadmin@staybnb.co.za` | Email of the SuperAdmin created by `RoleSeeder`. |
| `SuperAdmin:Password`                | Set in `appsettings.json` or user secrets | Password used only when the SuperAdmin account does not exist yet. |

The application applies EF Core migrations automatically when SQL Server is available. When it is not, it uses the in-memory provider and recreates the demo data on every restart. `database.sql` can be used to create the schema manually.

# Demo data

The seed creates fifteen active places across fifteen cities, together with bookings in the pending, approved, checked-in, completed and rejected states, reviews, wishlists, messages, notifications, an activity log and a completed check-in example for the Stellenbosch property. Seeded test-account credentials are recorded separately in `dummy-accounts.txt`; new registrations through the application always start as Guests.

# Tests

The business rules are covered by xUnit tests using the EF Core in-memory provider:

```bash
cd StayBnB.Tests
dotnet test
```

| Test class                    | What it covers |
|-------------------------------|----------------|
| `PricingServiceTests`         | The price formula, ignoring client totals, and same-day stays. |
| `BookingStateMachineTests`    | Every allowed and illegal booking-status transition. |
| `BookingServiceTests`         | Overlap detection, guest/host permissions and ownership. |
| `HostApplicationServiceTests` | The Guest-to-Host role swap, image requirement, rejection and single-decision rule. |
| `MessagingServiceTests`       | Persistence, notifications, logging and invalid messages. |
| `OwnershipTests`              | Notification scoping and unique wishlist entries. |
| `PropertySearchTests`         | Date search, overlap boundaries and cancelled/rejected bookings. |
| `ReviewServiceTests`          | Stay eligibility, duplicate reviews and rating/comment validation. |

All 40 tests pass. Run the suite before submitting any change to booking, role, pricing, search or review behaviour.

# Getting started

```bash
cd StayBnB
dotnet restore
dotnet run
```

Open the address printed in the console. A full walkthrough of setup, roles, workflows and troubleshooting is in `instructions.md`.
