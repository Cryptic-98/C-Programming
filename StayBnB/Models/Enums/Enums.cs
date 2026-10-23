namespace StayBnB.Models.Enums;

/// <summary>Kind of property a host lists. Stored as a string in the database.</summary>
public enum PropertyType
{
    Apartment,
    House,
    Villa,
    Cabin,
    Cottage,
    Studio,
    Guesthouse,
    Hotel
}

/// <summary>Lifecycle of a booking request. Stored as a string in the database.</summary>
public enum BookingStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    CheckedIn,
    Completed
}

/// <summary>Lifecycle of the payment attached to a booking.</summary>
public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

/// <summary>Lifecycle of a host application.</summary>
public enum ApplicationStatus
{
    Pending,
    Approved,
    Rejected
}

/// <summary>Lifecycle of a guest check-in (the guided document flow).</summary>
public enum CheckInStatus
{
    Pending,
    InProgress,
    Completed,
    Rejected
}

/// <summary>Lifecycle of an uploaded guest document.</summary>
public enum DocumentStatus
{
    Pending,
    Approved,
    Rejected
}

/// <summary>
/// Category of an entry in the activity log. These are the categories required by
/// the functional specification: Authentication, RoleChange, Booking, Application,
/// Property, CheckIn, Document, Messaging and System.
/// </summary>
public enum ActivityType
{
    Authentication,
    RoleChange,
    Booking,
    Application,
    Property,
    CheckIn,
    Document,
    Messaging,
    System
}

/// <summary>
/// Category of a notification: Booking, Application, Message, CheckIn or System.
/// The descriptive title stored with the notification carries the specific event.
/// </summary>
public enum NotificationType
{
    Booking,
    Application,
    Message,
    CheckIn,
    System
}
