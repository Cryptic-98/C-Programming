# StayBnB — Run and Use Guide

This guide explains how to start the StayBnB application and how to use it as a guest, host, administrator and SuperAdmin.

## 1. Before you start

You need:

- The **.NET 10 SDK** (the project targets `net10.0`).
- A modern web browser.
- Optional: SQL Server or SQL Server LocalDB. The application starts without it by using an in-memory database.
- Optional: the `dotnet-ef` tool if you want to manage migrations manually.

Check the SDK:

```bash
dotnet --version
```

## 2. Start the application

From the repository root:

```bash
cd StayBnB
dotnet restore
dotnet run
```

If you are already inside the `StayBnB` folder, run `dotnet restore` and `dotnet run` directly.

Or from the repository root:

```bash
dotnet run --project StayBnB/StayBnB.csproj
```

`dotnet run` prints the address it is listening on (normally `http://localhost:5000`). Open that address in your browser.

To choose a fixed port, use:

```bash
ASPNETCORE_URLS=http://localhost:5199 dotnet run
```

In PowerShell:

```powershell
$env:ASPNETCORE_URLS="http://localhost:5199"
dotnet run
```

Stop the application with `Ctrl+C`.

## 3. Database behaviour

The connection is configured in `appsettings.json`:

```json
"Database": {
  "Provider": "Auto"
}
```

- `Auto` tries to connect to SQL Server. If it succeeds, EF Core migrations are applied and data is kept between runs.
- If SQL Server cannot be reached, the app switches to the **EF Core in-memory provider**. All demo data is recreated every time the application restarts, and any changes you make are lost when it stops.
- Set `Database:Provider` to `SqlServer` to require SQL Server, or `InMemory` to force the in-memory store.
- `database.sql` contains the equivalent SQL Server schema if you prefer to create the database manually.

When using in-memory storage, the seed creates the demo accounts and fifteen demo places. The demo credentials are listed in [`dummy-accounts.txt`](dummy-accounts.txt).

## 4. Sign in

Use one of the accounts in [`dummy-accounts.txt`](dummy-accounts.txt), or select **Sign up** to register.

- Every new registration receives the **Guest** role.
- New accounts do not become Hosts automatically. They must submit a host application and be approved by an Admin or SuperAdmin.
- The dummy accounts are for development and demonstration only. Change the SuperAdmin password in `appsettings.json` or user secrets, and override the demo passwords with configuration before deploying the app publicly.

## 5. Guest walkthrough

1. Open **Explore** to browse cities and property types, or open **Stays** to search and filter.
2. On **Stays**, filter by city, property type, dates, guest count and price. The landing-page search also accepts a city, check-in/check-out dates and a guest count.
3. Open a property to view its details, amenities, images, reviews and booking form.
4. Sign in as a Guest and request a booking by choosing dates and the number of guests. The total is calculated by the server.
5. Open **Trips** to see the booking status. A pending request can be cancelled while it is still pending or once it has been approved.
6. Use **Messages** to contact the host about the stay.
7. After the host approves the booking and the property has a check-in process, open the booking and start the guided check-in. Complete each step and upload the requested identity document.
8. After the host verifies the documents, the host can check the booking in.
9. Once the stay is checked in or completed, open the property and leave a review. A guest can review each property only once.

The seeded **Family House in Stellenbosch** is the property with a preconfigured check-in process. New listings can have their own process configured by their host.

## 6. Host walkthrough

1. Sign in with the seeded Host account, or register as a Guest and submit an application through **Become a Host**.
2. An Admin or SuperAdmin reviews the application under **Admin → Applications**. Approval grants the Host role, removes the Guest role and publishes the property.
3. Open **Host dashboard** to see listings, bookings and recent activity.
4. Under **Your listings**, create a property, upload an image, add amenities and set the nightly rate, cleaning fee and service fee. You can also edit, activate/deactivate or delete a listing.
5. Open **Host bookings** to approve or reject requests. A rejection reason is sent to the guest as a message. After check-in is verified, use the same screen to complete a stay that has passed its check-out date.
6. Use **Check-in** to configure the steps guests must complete and to review uploaded identity documents.

## 7. Admin and SuperAdmin walkthrough

1. Sign in with the Admin account to open the **Admin** dashboard.
2. Review pending host applications and approve or reject them with a reason.
3. Use the activity log to inspect authentication, role, booking, application, property, check-in, document and messaging events.
4. Sign in as the SuperAdmin to open **User management**, search for a Guest and promote that user to Admin. Only the SuperAdmin can perform this promotion.

## 8. Run the tests

The business rules are covered by automated tests:

```bash
cd StayBnB.Tests
dotnet test
```

This builds the application and runs the test project. All 40 tests should pass. Re-run the suite after changing pricing, booking states, date search, host approval, reviews or messaging.

## 9. File uploads

- Property images are stored in `wwwroot/images/properties/` so they can be served to browsers.
- Guest identity documents are stored privately in `App_Data/documents/` and are streamed only through an authorised host action.
- Both folders are created automatically when the first file is uploaded.
- Uploads are limited to 5 MB per file. Property images may be JPG, JPEG, PNG or WebP. Guest documents may also be PDF.
- Uploaded files are not part of the seed data. Removing them is safe; the seed recreates only its own demo records.

## 10. Troubleshooting

**The page does not load**

Check the address printed by `dotnet run`. If the port is already in use, start with another one:

```bash
ASPNETCORE_URLS=http://localhost:5200 dotnet run
```

**The app starts with a warning about SQL Server**

This is expected when no SQL Server instance is available. The in-memory provider keeps the app usable, but data resets on restart.

**My changes disappeared after a restart**

You are using the in-memory provider. Either use SQL Server or expect the demo data to be recreated each time.

**The seeded accounts are missing**

The seed only runs when the database is empty. With SQL Server, existing data is preserved; with in-memory, restarting the app recreates it. If you changed the SuperAdmin or demo passwords, update the corresponding configuration values (`SuperAdmin:Password` and `DemoUsers:{Role}:Password`).

**A host cannot approve a booking or complete a stay**

Only the host who owns the property can do this. A stay can only be completed after its check-out date, and a booking can only be moved to `CheckedIn` after the guest check-in is complete and every document is approved.

**A guest cannot leave a review**

Reviews are limited to guests with a `CheckedIn` or `Completed` booking for that property, and each guest may review a property only once.
