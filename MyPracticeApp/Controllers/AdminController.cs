using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPracticeApp.Data;

namespace MyPracticeApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Flights()
        {
            var flights = await _context.Flights.ToListAsync();

            return View(flights);
        }

        public async Task<IActionResult> Passenger(int flightId)
        {
            var passengers = await _context.Bookings
                .Where(b => b.FlightId == flightId)
                .Select(b => b.Passenger)
                .Distinct()
                .ToListAsync();

            return View(passengers);
        }

        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Passenger)
                .Include(b => b.Flight)
                .ToListAsync();

            return View(bookings);
        }
    }
}