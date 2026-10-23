/*
    Staybnb - SQL Server Database Schema
    Generated from the Staybnb project relationships and requirements.

    Notes:
    - Designed for Microsoft SQL Server.
    - ASP.NET Core Identity normally creates its own AspNet* tables through EF Core migrations.
    - This script includes an Identity-compatible AspNetUsers/AspNetRoles foundation so the
      application schema can be understood as a complete database design.
    - Password/security values should be managed by ASP.NET Core Identity, not manually inserted.
*/

IF DB_ID(N'StaybnbDb') IS NULL
BEGIN
    CREATE DATABASE [StaybnbDb];
END
GO

USE [StaybnbDb];
GO

/* ============================================================
   IDENTITY
   ============================================================ */

IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles
    (
        Id                nvarchar(450) NOT NULL,
        Name              nvarchar(256) NULL,
        NormalizedName    nvarchar(256) NULL,
        ConcurrencyStamp  nvarchar(max) NULL,

        CONSTRAINT PK_AspNetRoles PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX IX_AspNetRoles_NormalizedName
        ON dbo.AspNetRoles (NormalizedName)
        WHERE NormalizedName IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers
    (
        Id                   nvarchar(450) NOT NULL,
        UserName             nvarchar(256) NULL,
        NormalizedUserName   nvarchar(256) NULL,
        Email                nvarchar(256) NULL,
        NormalizedEmail      nvarchar(256) NULL,
        EmailConfirmed       bit NOT NULL DEFAULT 0,
        PasswordHash         nvarchar(max) NULL,
        SecurityStamp        nvarchar(max) NULL,
        ConcurrencyStamp     nvarchar(max) NULL,
        PhoneNumber          nvarchar(max) NULL,
        PhoneNumberConfirmed bit NOT NULL DEFAULT 0,
        TwoFactorEnabled     bit NOT NULL DEFAULT 0,
        LockoutEnd           datetimeoffset NULL,
        LockoutEnabled       bit NOT NULL DEFAULT 1,
        AccessFailedCount    int NOT NULL DEFAULT 0,

        FirstName            nvarchar(100) NOT NULL,
        LastName             nvarchar(100) NOT NULL,
        ProfilePictureUrl    nvarchar(500) NULL,
        CreatedAt            datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_AspNetUsers PRIMARY KEY (Id)
    );

    CREATE INDEX IX_AspNetUsers_NormalizedEmail
        ON dbo.AspNetUsers (NormalizedEmail);

    CREATE UNIQUE INDEX IX_AspNetUsers_NormalizedUserName
        ON dbo.AspNetUsers (NormalizedUserName)
        WHERE NormalizedUserName IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserRoles
    (
        UserId nvarchar(450) NOT NULL,
        RoleId nvarchar(450) NOT NULL,

        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),

        CONSTRAINT FK_AspNetUserRoles_Users
            FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers (Id)
            ON DELETE CASCADE,

        CONSTRAINT FK_AspNetUserRoles_Roles
            FOREIGN KEY (RoleId)
            REFERENCES dbo.AspNetRoles (Id)
            ON DELETE CASCADE
    );
END
GO

/* ============================================================
   ENUMS
   Stored as strings to make database records readable.
   ============================================================ */

-- BookingStatus:
-- Pending, Approved, Rejected, Cancelled, CheckedIn, Completed
--
-- PaymentStatus:
-- Pending, Paid, Failed, Refunded
--
-- ApplicationStatus:
-- Pending, Approved, Rejected
--
-- ActivityType:
-- Authentication, RoleChange, Booking, Application, Property,
-- CheckIn, Document, Messaging, System
--
-- NotificationType:
-- Booking, Application, Message, CheckIn, System
--
-- CheckInStatus:
-- Pending, InProgress, Completed, Rejected
--
-- DocumentStatus:
-- Pending, Approved, Rejected

/* ============================================================
   HOST PROPERTY
   ============================================================ */

IF OBJECT_ID(N'dbo.HostProperties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HostProperties
    (
        Id               int IDENTITY(1,1) NOT NULL,
        Title            nvarchar(100) NOT NULL,
        Description      nvarchar(max) NOT NULL,
        PricePerNight    decimal(18,2) NOT NULL,
        HostId           nvarchar(450) NOT NULL,
        Address          nvarchar(500) NOT NULL,

        -- Required by the project for host listing management.
        IsActive         bit NOT NULL DEFAULT 1,

        -- Required by the booking pricing formula:
        -- TotalPrice = PricePerNight * Nights + CleaningFee + ServiceFee
        CleaningFee      decimal(18,2) NOT NULL DEFAULT 0,
        ServiceFee       decimal(18,2) NOT NULL DEFAULT 0,

        -- Supports the required city/property-type filtering.
        City             nvarchar(100) NULL,
        PropertyType     nvarchar(50) NULL,

        CONSTRAINT PK_HostProperties PRIMARY KEY (Id),

        CONSTRAINT FK_HostProperties_Host
            FOREIGN KEY (HostId)
            REFERENCES dbo.AspNetUsers (Id),

        CONSTRAINT CK_HostProperties_Price
            CHECK (PricePerNight >= 0),

        CONSTRAINT CK_HostProperties_CleaningFee
            CHECK (CleaningFee >= 0),

        CONSTRAINT CK_HostProperties_ServiceFee
            CHECK (ServiceFee >= 0)
    );

    CREATE INDEX IX_HostProperties_HostId
        ON dbo.HostProperties (HostId);

    CREATE INDEX IX_HostProperties_City
        ON dbo.HostProperties (City);

    CREATE INDEX IX_HostProperties_PropertyType
        ON dbo.HostProperties (PropertyType);

    CREATE INDEX IX_HostProperties_IsActive
        ON dbo.HostProperties (IsActive);
END
GO

/* ============================================================
   AMENITY
   ============================================================ */

IF OBJECT_ID(N'dbo.Amenities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Amenities
    (
        Id         int IDENTITY(1,1) NOT NULL,
        Name       nvarchar(50) NOT NULL,
        IconClass  nvarchar(50) NULL,

        CONSTRAINT PK_Amenities PRIMARY KEY (Id),

        CONSTRAINT UQ_Amenities_Name UNIQUE (Name)
    );
END
GO

/* ============================================================
   HOST PROPERTY <-> AMENITY
   Many-to-many junction table
   ============================================================ */

IF OBJECT_ID(N'dbo.HostPropertyAmenities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HostPropertyAmenities
    (
        PropertyId int NOT NULL,
        AmenityId  int NOT NULL,

        CONSTRAINT PK_HostPropertyAmenities
            PRIMARY KEY (PropertyId, AmenityId),

        CONSTRAINT FK_HostPropertyAmenities_Property
            FOREIGN KEY (PropertyId)
            REFERENCES dbo.HostProperties (Id)
            ON DELETE CASCADE,

        CONSTRAINT FK_HostPropertyAmenities_Amenity
            FOREIGN KEY (AmenityId)
            REFERENCES dbo.Amenities (Id)
            ON DELETE CASCADE
    );
END
GO

/* ============================================================
   PROPERTY IMAGE
   ============================================================ */

IF OBJECT_ID(N'dbo.PropertyImages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyImages
    (
        Id          int IDENTITY(1,1) NOT NULL,
        PropertyId  int NOT NULL,
        ImageUrl    nvarchar(1000) NOT NULL,

        CONSTRAINT PK_PropertyImages PRIMARY KEY (Id),

        CONSTRAINT FK_PropertyImages_Property
            FOREIGN KEY (PropertyId)
            REFERENCES dbo.HostProperties (Id)
            ON DELETE CASCADE
    );

    CREATE INDEX IX_PropertyImages_PropertyId
        ON dbo.PropertyImages (PropertyId);
END
GO

/* ============================================================
   BOOKING
   ============================================================ */

IF OBJECT_ID(N'dbo.Bookings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bookings
    (
        Id            int IDENTITY(1,1) NOT NULL,
        PropertyId    int NOT NULL,
        GuestId       nvarchar(450) NOT NULL,
        CheckInDate   datetime2(7) NOT NULL,
        CheckOutDate  datetime2(7) NOT NULL,
        TotalPrice    decimal(18,2) NOT NULL,
        Status        nvarchar(30) NOT NULL DEFAULT N'Pending',

        CONSTRAINT PK_Bookings PRIMARY KEY (Id),

        CONSTRAINT FK_Bookings_Property
            FOREIGN KEY (PropertyId)
            REFERENCES dbo.HostProperties (Id),

        CONSTRAINT FK_Bookings_Guest
            FOREIGN KEY (GuestId)
            REFERENCES dbo.AspNetUsers (Id),

        CONSTRAINT CK_Bookings_Dates
            CHECK (CheckOutDate > CheckInDate),

        CONSTRAINT CK_Bookings_TotalPrice
            CHECK (TotalPrice >= 0),

        CONSTRAINT CK_Bookings_Status
            CHECK (Status IN
            (
                N'Pending',
                N'Approved',
                N'Rejected',
                N'Cancelled',
                N'CheckedIn',
                N'Completed'
            ))
    );

    CREATE INDEX IX_Bookings_PropertyId
        ON dbo.Bookings (PropertyId);

    CREATE INDEX IX_Bookings_GuestId
        ON dbo.Bookings (GuestId);

    CREATE INDEX IX_Bookings_Status
        ON dbo.Bookings (Status);
END
GO

/* ============================================================
   PAYMENT
   One-to-one with Booking
   ============================================================ */

IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        Id          int IDENTITY(1,1) NOT NULL,
        BookingId   int NOT NULL,
        Amount      decimal(18,2) NOT NULL,
        Status      nvarchar(30) NOT NULL DEFAULT N'Pending',
        CreatedAt   datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Payments PRIMARY KEY (Id),

        CONSTRAINT FK_Payments_Booking
            FOREIGN KEY (BookingId)
            REFERENCES dbo.Bookings (Id)
            ON DELETE CASCADE,

        CONSTRAINT UQ_Payments_BookingId
            UNIQUE (BookingId),

        CONSTRAINT CK_Payments_Amount
            CHECK (Amount >= 0),

        CONSTRAINT CK_Payments_Status
            CHECK (Status IN
            (
                N'Pending',
                N'Paid',
                N'Failed',
                N'Refunded'
            ))
    );
END
GO

/* ============================================================
   REVIEW
   ============================================================ */

IF OBJECT_ID(N'dbo.Reviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reviews
    (
        Id           int IDENTITY(1,1) NOT NULL,
        PropertyId   int NOT NULL,
        ReviewerId   nvarchar(450) NOT NULL,
        Rating       int NOT NULL,
        Comment      nvarchar(1000) NULL,
        CreatedAt    datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Reviews PRIMARY KEY (Id),

        CONSTRAINT FK_Reviews_Property
            FOREIGN KEY (PropertyId)
            REFERENCES dbo.HostProperties (Id)
            ON DELETE CASCADE,

        CONSTRAINT FK_Reviews_Reviewer
            FOREIGN KEY (ReviewerId)
            REFERENCES dbo.AspNetUsers (Id),

        CONSTRAINT CK_Reviews_Rating
            CHECK (Rating BETWEEN 1 AND 5)
    );

    CREATE INDEX IX_Reviews_PropertyId
        ON dbo.Reviews (PropertyId);

    CREATE INDEX IX_Reviews_ReviewerId
        ON dbo.Reviews (ReviewerId);
END
GO

/* ============================================================
   WISHLIST ITEM
   ============================================================ */

IF OBJECT_ID(N'dbo.WishlistItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WishlistItems
    (
        Id          int IDENTITY(1,1) NOT NULL,
        UserId      nvarchar(450) NOT NULL,
        PropertyId  int NOT NULL,
        CreatedAt   datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_WishlistItems PRIMARY KEY (Id),

        CONSTRAINT FK_WishlistItems_User
            FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers (Id)
            ON DELETE CASCADE,

        CONSTRAINT FK_WishlistItems_Property
            FOREIGN KEY (PropertyId)
            REFERENCES dbo.HostProperties (Id)
            ON DELETE CASCADE,

        CONSTRAINT UQ_WishlistItems_User_Property
            UNIQUE (UserId, PropertyId)
    );

    CREATE INDEX IX_WishlistItems_PropertyId
        ON dbo.WishlistItems (PropertyId);
END
GO

/* ============================================================
   MESSAGE
   ============================================================ */

IF OBJECT_ID(N'dbo.Messages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Messages
    (
        Id          int IDENTITY(1,1) NOT NULL,
        SenderId    nvarchar(450) NOT NULL,
        ReceiverId  nvarchar(450) NOT NULL,
        Content     nvarchar(max) NOT NULL,
        [Timestamp] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Messages PRIMARY KEY (Id),

        CONSTRAINT FK_Messages_Sender
            FOREIGN KEY (SenderId)
            REFERENCES dbo.AspNetUsers (Id),

        CONSTRAINT FK_Messages_Receiver
            FOREIGN KEY (ReceiverId)
            REFERENCES dbo.AspNetUsers (Id)
    );

    CREATE INDEX IX_Messages_SenderId
        ON dbo.Messages (SenderId);

    CREATE INDEX IX_Messages_ReceiverId
        ON dbo.Messages (ReceiverId);

    CREATE INDEX IX_Messages_Timestamp
        ON dbo.Messages ([Timestamp]);
END
GO

/* ============================================================
   ACTIVITY LOG
   ============================================================ */

IF OBJECT_ID(N'dbo.ActivityLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ActivityLogs
    (
        Id            int IDENTITY(1,1) NOT NULL,
        UserId        nvarchar(450) NOT NULL,
        Action        nvarchar(100) NOT NULL,
        ActivityType  nvarchar(50) NOT NULL,
        CreatedAt     datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_ActivityLogs PRIMARY KEY (Id),

        CONSTRAINT FK_ActivityLogs_User
            FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers (Id),

        CONSTRAINT CK_ActivityLogs_Type
            CHECK (ActivityType IN
            (
                N'Authentication',
                N'RoleChange',
                N'Booking',
                N'Application',
                N'Property',
                N'CheckIn',
                N'Document',
                N'Messaging',
                N'System'
            ))
    );

    CREATE INDEX IX_ActivityLogs_UserId
        ON dbo.ActivityLogs (UserId);

    CREATE INDEX IX_ActivityLogs_CreatedAt
        ON dbo.ActivityLogs (CreatedAt);

    CREATE INDEX IX_ActivityLogs_ActivityType
        ON dbo.ActivityLogs (ActivityType);
END
GO

/* ============================================================
   NOTIFICATION
   ============================================================ */

IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications
    (
        Id          int IDENTITY(1,1) NOT NULL,
        UserId      nvarchar(450) NOT NULL,
        Title       nvarchar(100) NOT NULL,
        Type        nvarchar(50) NOT NULL,
        IsRead      bit NOT NULL DEFAULT 0,
        CreatedAt   datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Notifications PRIMARY KEY (Id),

        CONSTRAINT FK_Notifications_User
            FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers (Id)
            ON DELETE CASCADE,

        CONSTRAINT CK_Notifications_Type
            CHECK (Type IN
            (
                N'Booking',
                N'Application',
                N'Message',
                N'CheckIn',
                N'System'
            ))
    );

    CREATE INDEX IX_Notifications_UserId_IsRead
        ON dbo.Notifications (UserId, IsRead);
END
GO

/* ============================================================
   HOST APPLICATION
   ============================================================ */

IF OBJECT_ID(N'dbo.HostApplications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HostApplications
    (
        Id                 int IDENTITY(1,1) NOT NULL,
        ApplicationUserId  nvarchar(450) NOT NULL,
        PropertyId         int NOT NULL,
        Status             nvarchar(30) NOT NULL DEFAULT N'Pending',
        AppliedAt          datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_HostApplications PRIMARY KEY (Id),

        CONSTRAINT FK_HostApplications_User
            FOREIGN KEY (ApplicationUserId)
            REFERENCES dbo.AspNetUsers (Id),

        CONSTRAINT FK_HostApplications_Property
            FOREIGN KEY (PropertyId)
            REFERENCES dbo.HostProperties (Id),

        CONSTRAINT CK_HostApplications_Status
            CHECK (Status IN
            (
                N'Pending',
                N'Approved',
                N'Rejected'
            ))
    );

    CREATE INDEX IX_HostApplications_UserId
        ON dbo.HostApplications (ApplicationUserId);

    CREATE INDEX IX_HostApplications_PropertyId
        ON dbo.HostApplications (PropertyId);

    CREATE INDEX IX_HostApplications_Status
        ON dbo.HostApplications (Status);
END
GO

/* ============================================================
   CHECK-IN PROCESS
   One-to-one with HostProperty
   ============================================================ */

IF OBJECT_ID(N'dbo.CheckInProcesses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CheckInProcesses
    (
        Id          int IDENTITY(1,1) NOT NULL,
        PropertyId  int NOT NULL,
        Title       nvarchar(200) NOT NULL,
        StepsJson   nvarchar(max) NOT NULL,
        CreatedAt   datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_CheckInProcesses PRIMARY KEY (Id),

        CONSTRAINT FK_CheckInProcesses_Property
            FOREIGN KEY (PropertyId)
            REFERENCES dbo.HostProperties (Id)
            ON DELETE CASCADE,

        CONSTRAINT UQ_CheckInProcesses_PropertyId
            UNIQUE (PropertyId)
    );
END
GO

/* ============================================================
   GUEST CHECK-IN
   One-to-one with Booking
   ============================================================ */

IF OBJECT_ID(N'dbo.GuestCheckIns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GuestCheckIns
    (
        Id                int IDENTITY(1,1) NOT NULL,
        BookingId         int NOT NULL,
        CheckInProcessId  int NOT NULL,
        Status            nvarchar(30) NOT NULL DEFAULT N'Pending',
        CreatedAt         datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_GuestCheckIns PRIMARY KEY (Id),

        CONSTRAINT FK_GuestCheckIns_Booking
            FOREIGN KEY (BookingId)
            REFERENCES dbo.Bookings (Id)
            ON DELETE CASCADE,

        CONSTRAINT FK_GuestCheckIns_CheckInProcess
            FOREIGN KEY (CheckInProcessId)
            REFERENCES dbo.CheckInProcesses (Id),

        CONSTRAINT UQ_GuestCheckIns_BookingId
            UNIQUE (BookingId),

        CONSTRAINT CK_GuestCheckIns_Status
            CHECK (Status IN
            (
                N'Pending',
                N'InProgress',
                N'Completed',
                N'Rejected'
            ))
    );

    CREATE INDEX IX_GuestCheckIns_CheckInProcessId
        ON dbo.GuestCheckIns (CheckInProcessId);
END
GO

/* ============================================================
   GUEST DOCUMENT
   ============================================================ */

IF OBJECT_ID(N'dbo.GuestDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GuestDocuments
    (
        Id              int IDENTITY(1,1) NOT NULL,
        GuestCheckInId  int NOT NULL,
        DocumentType    nvarchar(100) NOT NULL,
        FileName        nvarchar(200) NOT NULL,
        Status          nvarchar(30) NOT NULL DEFAULT N'Pending',

        CONSTRAINT PK_GuestDocuments PRIMARY KEY (Id),

        CONSTRAINT FK_GuestDocuments_GuestCheckIn
            FOREIGN KEY (GuestCheckInId)
            REFERENCES dbo.GuestCheckIns (Id)
            ON DELETE CASCADE,

        CONSTRAINT CK_GuestDocuments_Status
            CHECK (Status IN
            (
                N'Pending',
                N'Approved',
                N'Rejected'
            ))
    );

    CREATE INDEX IX_GuestDocuments_GuestCheckInId
        ON dbo.GuestDocuments (GuestCheckInId);
END
GO

/* ============================================================
   ROLE SEEDING
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.AspNetRoles
    WHERE NormalizedName = N'GUEST'
)
BEGIN
    INSERT INTO dbo.AspNetRoles
    (
        Id,
        Name,
        NormalizedName,
        ConcurrencyStamp
    )
    VALUES
    (
        CONVERT(nvarchar(450), NEWID()),
        N'Guest',
        N'GUEST',
        CONVERT(nvarchar(36), NEWID())
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.AspNetRoles
    WHERE NormalizedName = N'HOST'
)
BEGIN
    INSERT INTO dbo.AspNetRoles
    (
        Id,
        Name,
        NormalizedName,
        ConcurrencyStamp
    )
    VALUES
    (
        CONVERT(nvarchar(450), NEWID()),
        N'Host',
        N'HOST',
        CONVERT(nvarchar(36), NEWID())
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.AspNetRoles
    WHERE NormalizedName = N'ADMIN'
)
BEGIN
    INSERT INTO dbo.AspNetRoles
    (
        Id,
        Name,
        NormalizedName,
        ConcurrencyStamp
    )
    VALUES
    (
        CONVERT(nvarchar(450), NEWID()),
        N'Admin',
        N'ADMIN',
        CONVERT(nvarchar(36), NEWID())
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.AspNetRoles
    WHERE NormalizedName = N'SUPERADMIN'
)
BEGIN
    INSERT INTO dbo.AspNetRoles
    (
        Id,
        Name,
        NormalizedName,
        ConcurrencyStamp
    )
    VALUES
    (
        CONVERT(nvarchar(450), NEWID()),
        N'SuperAdmin',
        N'SUPERADMIN',
        CONVERT(nvarchar(36), NEWID())
    );
END
GO

/* ============================================================
   SAMPLE AMENITIES
   ============================================================ */

IF NOT EXISTS (SELECT 1 FROM dbo.Amenities WHERE Name = N'WiFi')
    INSERT INTO dbo.Amenities (Name, IconClass)
    VALUES (N'WiFi', N'fa-wifi');

IF NOT EXISTS (SELECT 1 FROM dbo.Amenities WHERE Name = N'Pool')
    INSERT INTO dbo.Amenities (Name, IconClass)
    VALUES (N'Pool', N'fa-person-swimming');

IF NOT EXISTS (SELECT 1 FROM dbo.Amenities WHERE Name = N'Parking')
    INSERT INTO dbo.Amenities (Name, IconClass)
    VALUES (N'Parking', N'fa-car');

IF NOT EXISTS (SELECT 1 FROM dbo.Amenities WHERE Name = N'Air Conditioning')
    INSERT INTO dbo.Amenities (Name, IconClass)
    VALUES (N'Air Conditioning', N'fa-snowflake');

IF NOT EXISTS (SELECT 1 FROM dbo.Amenities WHERE Name = N'Kitchen')
    INSERT INTO dbo.Amenities (Name, IconClass)
    VALUES (N'Kitchen', N'fa-kitchen-set');

IF NOT EXISTS (SELECT 1 FROM dbo.Amenities WHERE Name = N'TV')
    INSERT INTO dbo.Amenities (Name, IconClass)
    VALUES (N'TV', N'fa-tv');
GO

/* ============================================================
   END OF DATABASE SCRIPT
   ============================================================ */
