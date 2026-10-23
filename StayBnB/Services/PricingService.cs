using StayBnB.Models.Entities;

namespace StayBnB.Services;

/// <summary>
/// The server side booking price calculation. The browser may preview a total,
/// but this service is the only thing that decides what is stored.
/// </summary>
public class BookingPrice
{
    public int Nights { get; init; }
    public decimal PricePerNight { get; init; }
    public decimal CleaningFee { get; init; }
    public decimal ServiceFee { get; init; }
    public decimal TotalPrice { get; init; }
    public decimal NightlySubtotal => PricePerNight * Nights;
}

public interface IPricingService
{
    BookingPrice Calculate(HostProperty property, DateTime checkIn, DateTime checkOut);
}

public class PricingService : IPricingService
{
    public BookingPrice Calculate(HostProperty property, DateTime checkIn, DateTime checkOut)
    {
        // The rubric formula is fixed: nights x nightly rate + cleaning fee + service fee.
        // Every component is read from the authoritative property record, never the form.
        var nights = Math.Max(0, (checkOut.Date - checkIn.Date).Days);
        var nightly = property.PricePerNight;
        var subtotal = nightly * nights;
        var cleaningFee = property.CleaningFee;
        var serviceFee = property.ServiceFee;
        var total = subtotal + cleaningFee + serviceFee;

        return new BookingPrice
        {
            Nights = nights,
            PricePerNight = nightly,
            CleaningFee = cleaningFee,
            ServiceFee = serviceFee,
            TotalPrice = total
        };
    }
}
