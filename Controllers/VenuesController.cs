using Microsoft.AspNetCore.Mvc;
using api.Data;
using api.Models;
using api.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers
{
    [Route("api/venues")]
    [ApiController]
    public class VenuesController : ControllerBase
    {
        private readonly ApplicationDBcontext _context;

        public VenuesController(ApplicationDBcontext context)
        {
            _context = context;
        }

        // Anyone logged in can view venues and time slots
        [Authorize]
        // GET: api/venues/{name}/timeslots
        [HttpGet("{name}/timeslots")]
        public async Task<IActionResult> GetVenueWithTimeSlots([FromRoute] string name)
        {
            // Find the venue by name
            var venue = await _context.Venues
                .Include(v => v.TimeSlots) // Include related time slots
                .FirstOrDefaultAsync(v => v.Name == name);

            if (venue == null)
            {
                return NotFound($"Venue with name '{name}' not found.");
            }

            // Map venue and time slots to DTOs
            var venueWithTimeSlots = new
            {
                Venue = new VenuesDTO
                {
                    Name = venue.Name,
                    Location = venue.Location,
                    Capacity = venue.Capacity,
                    Type = venue.Type,
                    Status = venue.Status,
                    Price = venue.Price
                },
                TimeSlots = venue.TimeSlots.Select(ts => new TimeSlotDTO
                {
                    ID = ts.ID,
                    VenueName = ts.VenueName,
                    StartTime = ts.StartTime,
                    EndTime = ts.EndTime,
                    IsAvailable = ts.IsAvailable
                })
            };

            return Ok(venueWithTimeSlots);
        }

        [Authorize]
        // GET: api/venues
        [HttpGet]
        public async Task<IActionResult> GetAllVenuesWithTimeSlots()
        {
            // Retrieve all venues and include their time slots
            var venues = await _context.Venues
                .Include(v => v.TimeSlots) // Include related time slots
                .ToListAsync();

            // Map venues and their time slots to DTOs
            var venueDtos = venues.Select(v => new
            {
                Venue = new VenuesDTO
                {
                    Name = v.Name,
                    Location = v.Location,
                    Capacity = v.Capacity,
                    Type = v.Type,
                    Status = v.Status,
                    Price = v.Price // Include price in the DTO
                },
                TimeSlots = v.TimeSlots.Select(ts => new TimeSlotDTO
                {
                    ID = ts.ID,
                    VenueName = ts.VenueName,
                    StartTime = ts.StartTime,
                    EndTime = ts.EndTime,
                    IsAvailable = ts.IsAvailable
                })
            });

            return Ok(venueDtos);
        }

        // Only admin and staff can create venues
        [Authorize(Roles = "admin,staff")]
        // POST: api/venues
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VenuesCreateUpdateDTO venueDto)
        {
            if (await _context.Venues.AnyAsync(v => v.Name == venueDto.Name))
            {
                return BadRequest("A venue with this name already exists.");
            }

            var venue = new Venues
            {
                Name = venueDto.Name,
                Location = venueDto.Location,
                Capacity = venueDto.Capacity,
                Type = venueDto.Type,
                Status = venueDto.Status,
                Price = (decimal)venueDto.Price
            };

            await _context.Venues.AddAsync(venue);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVenueWithTimeSlots), new { name = venue.Name }, venueDto);
        }

        // Only admin and staff can update venues
        [Authorize(Roles = "admin,staff")]
        // PUT: api/venues/{name}
        [HttpPut("{name}")]
        public async Task<IActionResult> Update([FromRoute] string name, [FromBody] VenuesCreateUpdateDTO venueDto)
        {
            if (name != venueDto.Name)
            {
                return BadRequest("Venue name cannot be changed.");
            }

            var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Name == name);

            if (venue == null)
            {
                return NotFound("Venue not found");
            }

            venue.Location = venueDto.Location;
            venue.Capacity = venueDto.Capacity;
            venue.Type = venueDto.Type;
            venue.Status = venueDto.Status;
            venue.Price = (decimal)venueDto.Price; // Update price

            await _context.SaveChangesAsync();
            return Ok(new { message = "Venue updated successfully" });
        }

        // Only admin and staff can delete venues
        [Authorize(Roles = "admin,staff")]
        // DELETE: api/venues/{name}
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete([FromRoute] string name)
        {
            var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Name == name);

            if (venue == null)
            {
                return NotFound("Venue not found");
            }

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}