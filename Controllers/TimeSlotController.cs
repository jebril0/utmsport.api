using Microsoft.AspNetCore.Mvc;
using api.Data;
using api.Models;
using api.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers
{
    [Route("api/timeslots")]
    [ApiController]
    public class TimeSlotController : ControllerBase
    {
        private readonly ApplicationDBcontext _context;

        public TimeSlotController(ApplicationDBcontext context)
        {
            _context = context;
        }

        // Anyone logged in can view time slots
        [Authorize]
        // GET: api/timeslots/venue/{venueName}
        [HttpGet("venue/{venueName}")]
        public async Task<IActionResult> GetByVenueName([FromRoute] string venueName)
        {
            // Find all time slots for the given venue name
            var timeSlots = await _context.TimeSlots
                .Where(ts => ts.VenueName == venueName)
                .ToListAsync();

            if (!timeSlots.Any())
            {
                return NotFound($"No time slots found for venue '{venueName}'.");
            }

            // Map to DTOs
            var timeSlotDtos = timeSlots.Select(ts => new TimeSlotDTO
            {
                ID = ts.ID,
                VenueName = ts.VenueName,
                StartTime = ts.StartTime,
                EndTime = ts.EndTime,
                IsAvailable = ts.IsAvailable
            });

            return Ok(timeSlotDtos);
        }

        // Only admin and staff can create time slots
        [Authorize(Roles = "admin,staff")]
        // POST: api/timeslots/{venueName}
        [HttpPost("{venueName}")]
        public async Task<IActionResult> Create([FromRoute] string venueName, [FromBody] TimeSlotCreateUpdateDTO timeSlotDto)
        {
            // Check if the venue exists
            var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Name == venueName);
            if (venue == null)
            {
                return NotFound($"Venue with name '{venueName}' not found.");
            }

            // Create the time slot with only time-related fields
            var timeSlot = new TimeSlot
            {
                VenueName = venueName,
                StartTime = timeSlotDto.StartTime,
                EndTime = timeSlotDto.EndTime
            };

            await _context.TimeSlots.AddAsync(timeSlot);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByVenueName), new { venueName = timeSlot.VenueName }, new
            {
                timeSlot.ID,
                timeSlot.VenueName,
                timeSlot.StartTime,
                timeSlot.EndTime
            });
        }

        // Only admin and staff can delete time slots
        [Authorize(Roles = "admin,staff")]
        // DELETE: api/timeslots/{venueName}/{startTime}/{endTime}
        [HttpDelete("{venueName}/{startTime}/{endTime}")]
        public async Task<IActionResult> DeleteByVenueAndTime(
            [FromRoute] string venueName,
            [FromRoute] TimeSpan startTime,
            [FromRoute] TimeSpan endTime)
        {
            // Find the time slot by venue name and time
            var timeSlot = await _context.TimeSlots.FirstOrDefaultAsync(ts =>
                ts.VenueName == venueName &&
                ts.StartTime == startTime &&
                ts.EndTime == endTime);

            if (timeSlot == null)
            {
                return NotFound($"Time slot for venue '{venueName}' with start time '{startTime}' and end time '{endTime}' not found.");
            }

            // Remove the time slot
            _context.TimeSlots.Remove(timeSlot);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}