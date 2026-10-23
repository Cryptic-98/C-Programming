using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayBnB.Models.Entities;

namespace StayBnB.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AspNetUsers");

        builder.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
        builder.Property(u => u.CreatedAt).HasColumnType("datetime2(7)");

        builder.HasIndex(u => u.NormalizedEmail).HasDatabaseName("IX_AspNetUsers_NormalizedEmail");
        builder.HasIndex(u => u.NormalizedUserName).IsUnique().HasDatabaseName("IX_AspNetUsers_NormalizedUserName");
    }
}

public class HostPropertyConfiguration : IEntityTypeConfiguration<HostProperty>
{
    public void Configure(EntityTypeBuilder<HostProperty> builder)
    {
        builder.ToTable("HostProperties", table =>
        {
            table.HasCheckConstraint("CK_HostProperties_Price", "PricePerNight >= 0");
            table.HasCheckConstraint("CK_HostProperties_CleaningFee", "CleaningFee >= 0");
            table.HasCheckConstraint("CK_HostProperties_ServiceFee", "ServiceFee >= 0");
        });

        builder.Property(p => p.Title).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(p => p.PricePerNight).HasColumnType("decimal(18,2)");
        builder.Property(p => p.CleaningFee).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ServiceFee).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Address).HasMaxLength(500).IsRequired();
        builder.Property(p => p.City).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Country).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PropertyType).HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.CreatedAt).HasColumnType("datetime2(7)");

        builder.HasIndex(p => p.HostId).HasDatabaseName("IX_HostProperties_HostId");
        builder.HasIndex(p => p.City).HasDatabaseName("IX_HostProperties_City");
        builder.HasIndex(p => p.PropertyType).HasDatabaseName("IX_HostProperties_PropertyType");
        builder.HasIndex(p => p.IsActive).HasDatabaseName("IX_HostProperties_IsActive");

        builder.HasOne(p => p.Host)
            .WithMany(u => u.Properties)
            .HasForeignKey(p => p.HostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Amenities)
            .WithMany(a => a.Properties)
            .UsingEntity(j => j.ToTable("HostPropertyAmenities"));

        builder.HasOne(p => p.HostApplication)
            .WithOne(a => a.Property)
            .HasForeignKey<HostApplication>(a => a.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.CheckInProcess)
            .WithOne(c => c.Property)
            .HasForeignKey<CheckInProcess>(c => c.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
{
    public void Configure(EntityTypeBuilder<PropertyImage> builder)
    {
        builder.ToTable("PropertyImages");

        builder.Property(i => i.ImageUrl).HasMaxLength(1000).IsRequired();

        builder.HasIndex(i => i.PropertyId).HasDatabaseName("IX_PropertyImages_PropertyId");

        builder.HasOne(i => i.Property)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("Amenities");

        builder.Property(a => a.Name).HasMaxLength(50).IsRequired();
        builder.Property(a => a.IconClass).HasMaxLength(50);

        builder.HasIndex(a => a.Name).IsUnique().HasDatabaseName("UQ_Amenities_Name");
    }
}

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", table =>
        {
            table.HasCheckConstraint("CK_Bookings_Dates", "CheckOutDate > CheckInDate");
            table.HasCheckConstraint("CK_Bookings_TotalPrice", "TotalPrice >= 0");
            table.HasCheckConstraint("CK_Bookings_Status",
                "Status IN (N'Pending',N'Approved',N'Rejected',N'Cancelled',N'CheckedIn',N'Completed')");
        });

        builder.Property(b => b.PricePerNight).HasColumnType("decimal(18,2)");
        builder.Property(b => b.CleaningFee).HasColumnType("decimal(18,2)");
        builder.Property(b => b.ServiceFee).HasColumnType("decimal(18,2)");
        builder.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)");
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(b => b.CreatedAt).HasColumnType("datetime2(7)");

        builder.HasIndex(b => b.PropertyId).HasDatabaseName("IX_Bookings_PropertyId");
        builder.HasIndex(b => b.GuestId).HasDatabaseName("IX_Bookings_GuestId");
        builder.HasIndex(b => b.Status).HasDatabaseName("IX_Bookings_Status");
        builder.HasIndex(b => new { b.PropertyId, b.CheckInDate, b.CheckOutDate })
            .HasDatabaseName("IX_Bookings_PropertyId_CheckInDate_CheckOutDate");

        builder.HasOne(b => b.Property)
            .WithMany(p => p.Bookings)
            .HasForeignKey(b => b.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Guest)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Payment)
            .WithOne(p => p.Booking)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.GuestCheckIn)
            .WithOne(c => c.Booking)
            .HasForeignKey<GuestCheckIn>(c => c.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", table =>
        {
            table.HasCheckConstraint("CK_Payments_Amount", "Amount >= 0");
            table.HasCheckConstraint("CK_Payments_Status", "Status IN (N'Pending',N'Paid',N'Failed',N'Refunded')");
        });

        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.CreatedAt).HasColumnType("datetime2(7)");

        builder.HasIndex(p => p.BookingId).IsUnique().HasDatabaseName("UQ_Payments_BookingId");
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", table =>
        {
            table.HasCheckConstraint("CK_Reviews_Rating", "Rating BETWEEN 1 AND 5");
        });

        builder.Property(r => r.Comment).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnType("datetime2(7)");

        builder.HasIndex(r => r.PropertyId).HasDatabaseName("IX_Reviews_PropertyId");
        builder.HasIndex(r => r.ReviewerId).HasDatabaseName("IX_Reviews_ReviewerId");
        builder.HasIndex(r => new { r.ReviewerId, r.PropertyId }).IsUnique().HasDatabaseName("UQ_Reviews_ReviewerId_PropertyId");

        builder.HasOne(r => r.Property)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Reviewer)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("WishlistItems");

        builder.Property(w => w.CreatedAt).HasColumnType("datetime2(7)");

        builder.HasIndex(w => w.PropertyId).HasDatabaseName("IX_WishlistItems_PropertyId");
        builder.HasIndex(w => new { w.UserId, w.PropertyId }).IsUnique().HasDatabaseName("UQ_WishlistItems_User_Property");

        builder.HasOne(w => w.User)
            .WithMany(u => u.WishlistItems)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.Property)
            .WithMany(p => p.WishlistItems)
            .HasForeignKey(w => w.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.Property(m => m.Content).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(m => m.Timestamp).HasColumnType("datetime2(7)");

        builder.HasIndex(m => m.SenderId).HasDatabaseName("IX_Messages_SenderId");
        builder.HasIndex(m => m.ReceiverId).HasDatabaseName("IX_Messages_ReceiverId");
        builder.HasIndex(m => m.Timestamp).HasDatabaseName("IX_Messages_Timestamp");
        builder.HasIndex(m => new { m.ReceiverId, m.IsRead }).HasDatabaseName("IX_Messages_ReceiverId_IsRead");

        builder.HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", table =>
        {
            table.HasCheckConstraint("CK_Notifications_Type",
                "Type IN (N'Booking',N'Application',N'Message',N'CheckIn',N'System')");
        });

        builder.Property(n => n.Title).HasMaxLength(100).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(500);
        builder.Property(n => n.LinkUrl).HasMaxLength(300);
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(n => n.CreatedAt).HasColumnType("datetime2(7)");

        builder.HasIndex(n => new { n.UserId, n.IsRead }).HasDatabaseName("IX_Notifications_UserId_IsRead");
    }
}

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs", table =>
        {
            table.HasCheckConstraint("CK_ActivityLogs_Type",
                "ActivityType IN (N'Authentication',N'RoleChange',N'Booking',N'Application',N'Property',N'CheckIn',N'Document',N'Messaging',N'System')");
        });

        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.ActivityType).HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.CreatedAt).HasColumnType("datetime2(7)");

        builder.HasIndex(a => a.UserId).HasDatabaseName("IX_ActivityLogs_UserId");
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("IX_ActivityLogs_CreatedAt");
        builder.HasIndex(a => a.ActivityType).HasDatabaseName("IX_ActivityLogs_ActivityType");

        builder.HasOne(a => a.User)
            .WithMany(u => u.ActivityLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class HostApplicationConfiguration : IEntityTypeConfiguration<HostApplication>
{
    public void Configure(EntityTypeBuilder<HostApplication> builder)
    {
        builder.ToTable("HostApplications", table =>
        {
            table.HasCheckConstraint("CK_HostApplications_Status", "Status IN (N'Pending',N'Approved',N'Rejected')");
        });

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.AppliedAt).HasColumnType("datetime2(7)");
        builder.Property(a => a.ReviewedAt).HasColumnType("datetime2(7)");
        builder.Property(a => a.RejectionReason).HasMaxLength(500);

        builder.HasIndex(a => a.ApplicationUserId).HasDatabaseName("IX_HostApplications_UserId");
        builder.HasIndex(a => a.PropertyId).HasDatabaseName("IX_HostApplications_PropertyId");
        builder.HasIndex(a => a.Status).HasDatabaseName("IX_HostApplications_Status");

        builder.HasOne(a => a.ApplicationUser)
            .WithMany(u => u.HostApplications)
            .HasForeignKey(a => a.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CheckInProcessConfiguration : IEntityTypeConfiguration<CheckInProcess>
{
    public void Configure(EntityTypeBuilder<CheckInProcess> builder)
    {
        builder.ToTable("CheckInProcesses");

        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.StepsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnType("datetime2(7)");

        builder.HasIndex(c => c.PropertyId).IsUnique().HasDatabaseName("UQ_CheckInProcesses_PropertyId");
    }
}

public class GuestCheckInConfiguration : IEntityTypeConfiguration<GuestCheckIn>
{
    public void Configure(EntityTypeBuilder<GuestCheckIn> builder)
    {
        builder.ToTable("GuestCheckIns", table =>
        {
            table.HasCheckConstraint("CK_GuestCheckIns_Status",
                "Status IN (N'Pending',N'InProgress',N'Completed',N'Rejected')");
        });

        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.CreatedAt).HasColumnType("datetime2(7)");

        builder.HasIndex(c => c.CheckInProcessId).HasDatabaseName("IX_GuestCheckIns_CheckInProcessId");
        builder.HasIndex(c => c.BookingId).IsUnique().HasDatabaseName("UQ_GuestCheckIns_BookingId");
    }
}

public class GuestDocumentConfiguration : IEntityTypeConfiguration<GuestDocument>
{
    public void Configure(EntityTypeBuilder<GuestDocument> builder)
    {
        builder.ToTable("GuestDocuments", table =>
        {
            table.HasCheckConstraint("CK_GuestDocuments_Status", "Status IN (N'Pending',N'Approved',N'Rejected')");
        });

        builder.Property(d => d.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.FileName).HasMaxLength(200).IsRequired();
        builder.Property(d => d.OriginalFileName).HasMaxLength(260);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(d => d.UploadedAt).HasColumnType("datetime2(7)");

        builder.HasIndex(d => d.GuestCheckInId).HasDatabaseName("IX_GuestDocuments_GuestCheckInId");
    }
}
