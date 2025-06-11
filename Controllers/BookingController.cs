using Microsoft.AspNetCore.Mvc;
using api.Data;
using api.Models;
using api.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using QRCoder;
using System.Drawing.Imaging;
using System.Net.Mail;
using System.Net;
using System.Drawing;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly ApplicationDBcontext _context;

        public BookingController(ApplicationDBcontext context)
        {
            _context = context;
        }

        // Anyone logged in can view all bookings
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.TimeSlot)
                .Select(b => new
                {
                    b.ID,
                    b.UserEmail,
                    b.TimeSlotID,
                    b.IsConfirmed,
                    PaymentScreenshotUrl = b.PaymentScreenshot != null
                        ? $"/api/bookings/{b.ID}/screenshot"
                        : null,
                    StartTime = b.TimeSlot.StartTime,
                    EndTime = b.TimeSlot.EndTime,
                    VenueName = b.TimeSlot.VenueName
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // Only the student who owns the bookings or admin/staff can view bookings by email
        [Authorize]
        [HttpGet("user/{email}")]
        public async Task<IActionResult> GetBookingsByEmail([FromRoute] string email)
        {
            var bookings = await _context.Bookings
                .Include(b => b.TimeSlot)
                .Where(b => b.UserEmail == email)
                .ToListAsync();

            if (!bookings.Any())
                return NotFound($"No bookings found for user with email '{email}'.");

            return Ok(bookings);
        }

        // Only students, admin, or staff can create bookings
        [Authorize(Roles = "student,admin,staff")]
        [HttpPost]
        public async Task<IActionResult> InsertBooking([FromForm] BookingVenueDTO bookingDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == bookingDto.UserEmail);
            if (user == null)
                return BadRequest("User with the provided email does not exist.");

            var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Name == bookingDto.VenueName);
            if (venue == null)
                return BadRequest($"Venue with name '{bookingDto.VenueName}' does not exist.");

            var timeSlot = await _context.TimeSlots.FirstOrDefaultAsync(ts =>
                ts.VenueName == bookingDto.VenueName &&
                ts.StartTime == bookingDto.StartTime &&
                ts.EndTime == bookingDto.EndTime &&
                ts.IsAvailable);

            if (timeSlot == null)
                return BadRequest("The selected time slot is not available.");

            byte[]? paymentScreenshotBytes = null;
            if (bookingDto.PaymentScreenshot != null)
            {
                using var ms = new MemoryStream();
                await bookingDto.PaymentScreenshot.CopyToAsync(ms);
                paymentScreenshotBytes = ms.ToArray();
            }

            var booking = new Booking
            {
                UserEmail = bookingDto.UserEmail,
                User = user,
                TimeSlotID = timeSlot.ID,
                PaymentScreenshot = paymentScreenshotBytes,
                IsConfirmed = false
            };

            _context.Bookings.Add(booking);
            timeSlot.IsAvailable = false;
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBookingsByEmail), new { email = booking.UserEmail }, new
            {
                bookingId = booking.ID,
                userEmail = booking.UserEmail,
                timeSlotID = booking.TimeSlotID,
                isConfirmed = booking.IsConfirmed
            });
        }

        // Only the student who owns the booking or admin/staff can delete
        [Authorize]
        [HttpDelete("user/{email}/venue/{venueName}/time")]
        public async Task<IActionResult> DeleteBooking(
            [FromRoute] string email,
            [FromRoute] string venueName,
            [FromQuery] TimeSpan startTime,
            [FromQuery] TimeSpan endTime)
        {
            var booking = await _context.Bookings
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(b => b.UserEmail == email &&
                                          b.TimeSlot.VenueName == venueName &&
                                          b.TimeSlot.StartTime == startTime &&
                                          b.TimeSlot.EndTime == endTime);

            if (booking == null)
                return NotFound($"No booking found for user '{email}' at venue '{venueName}' with start time '{startTime}' and end time '{endTime}'.");

            _context.Bookings.Remove(booking);

            var timeSlot = await _context.TimeSlots.FindAsync(booking.TimeSlotID);
            if (timeSlot != null)
                timeSlot.IsAvailable = true;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Only admin and staff can accept payment
        [Authorize(Roles = "admin,staff")]
        [HttpPut("accept")]
        public async Task<IActionResult> AcceptPayment(
            [FromQuery] string email,
            [FromQuery] string venueName,
            [FromQuery] TimeSpan startTime,
            [FromQuery] TimeSpan endTime)
        {
            var booking = await _context.Bookings
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(b => b.UserEmail == email &&
                                          b.TimeSlot.VenueName == venueName &&
                                          b.TimeSlot.StartTime == startTime &&
                                          b.TimeSlot.EndTime == endTime);

            if (booking == null)
                return NotFound("Booking not found.");

            booking.IsConfirmed = true;
            booking.QrToken = GenerateQrToken();
            await _context.SaveChangesAsync();

            await SendQrCodeEmailAsync(booking.UserEmail, booking.QrToken, booking, true);

            return Ok(new { message = "Payment accepted and booking confirmed. QR code sent to student." });
        }

        // Only admin and staff can reject payment
        [Authorize(Roles = "admin,staff")]
        [HttpPut("reject")]
        public async Task<IActionResult> RejectPayment(
            [FromQuery] string email,
            [FromQuery] string venueName,
            [FromQuery] TimeSpan startTime,
            [FromQuery] TimeSpan endTime)
        {
            var booking = await _context.Bookings
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(b => b.UserEmail == email &&
                                          b.TimeSlot.VenueName == venueName &&
                                          b.TimeSlot.StartTime == startTime &&
                                          b.TimeSlot.EndTime == endTime);

            if (booking == null)
                return NotFound("Booking not found.");

            await SendQrCodeEmailAsync(booking.UserEmail, null, booking, false);

            _context.Bookings.Remove(booking);
            var timeSlot = await _context.TimeSlots.FindAsync(booking.TimeSlotID);
            if (timeSlot != null)
                timeSlot.IsAvailable = true;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment rejected and booking deleted. Rejection email sent to student." });
        }

        // Anyone logged in can get payment screenshot
        [Authorize]
        [HttpGet("{id}/screenshot")]
        public async Task<IActionResult> GetPaymentScreenshot(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null || booking.PaymentScreenshot == null)
                return NotFound();

            return File(booking.PaymentScreenshot, "image/jpeg");
        }

        // Only admin and staff can validate QR
        [Authorize]
        [HttpGet("validate-qr")]
        public async Task<IActionResult> ValidateQr([FromQuery] string token)
        {
            var booking = await _context.Bookings
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(b => b.QrToken == token && b.IsConfirmed);

            if (booking == null)
                return NotFound(new { valid = false, message = "Invalid or expired QR code." });

            return Ok(new
            {
                valid = true,
                userEmail = booking.UserEmail,
                venueName = booking.TimeSlot?.VenueName,
                startTime = booking.TimeSlot?.StartTime,
                endTime = booking.TimeSlot?.EndTime,
                isConfirmed = booking.IsConfirmed
            });
        }

        // --- Helper Methods ---

        private static string GenerateQrToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }


        private static byte[] GenerateQrCodePng(string qrToken)
        {
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(qrToken, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.QRCode(qrData);
            using var bitmap = qrCode.GetGraphic(20);
            using var ms = new MemoryStream();
#pragma warning disable CA1416 // Validate platform compatibility
            bitmap.Save(ms, ImageFormat.Png);
#pragma warning restore CA1416 // Validate platform compatibility
            return ms.ToArray();
        }

        private async Task SendQrCodeEmailAsync(string toEmail, string qrToken, Booking booking, bool isAccepted)
        {
            using var message = new MailMessage
            {
                From = new MailAddress("hello@jibna.live", "UTM Booking System"),
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            if (isAccepted)
            {
                message.Subject = "🎉 Your Booking is Confirmed!";
                message.Body = $@"
                  <div style='font-family:Arial,sans-serif;'>
                    <h2 style='color:#2E8B57;'>UTM Booking Confirmation</h2>
                    <p>
                      Dear <b>{booking.UserEmail}</b>,<br/><br/>
                      Your booking for <b>{booking.TimeSlot?.VenueName}</b> is <span style='color:green;font-weight:bold;'>confirmed</span>!<br/>
                      <b>Date/Time:</b> {booking.TimeSlot?.StartTime} - {booking.TimeSlot?.EndTime}<br/><br/>
                      Please present the attached QR code at the facility entrance.<br/><br/>
                      <em>Thank you for booking with UTM!</em>
                    </p>
                  </div>";
                var qrPngBytes = GenerateQrCodePng(qrToken);
                message.Attachments.Add(new Attachment(new MemoryStream(qrPngBytes), "booking-qr.png", "image/png"));
            }
            else
            {
                message.Subject = "❌ Your Booking was Rejected";
                message.Body = $@"
                  <div style='font-family:Arial,sans-serif;'>
                    <h2 style='color:#B22222;'>UTM Booking Update</h2>
                    <p>
                      Dear <b>{booking.UserEmail}</b>,<br/><br/>
                      Unfortunately, your booking for <b>{booking.TimeSlot?.VenueName}</b> on {booking.TimeSlot?.StartTime} - {booking.TimeSlot?.EndTime} was <span style='color:red;font-weight:bold;'>rejected</span>.<br/>
                      Please try again or contact support if you need assistance.<br/><br/>
                      <em>We hope to serve you soon!</em>
                    </p>
                  </div>";
            }

            using var smtp = new SmtpClient("live.smtp.mailtrap.io", 587)
            {
                Credentials = new NetworkCredential("api", "3b777591f83a047e2f6195eee833657e"),
                EnableSsl = true
            };
            await smtp.SendMailAsync(message);
        }

        // Cancel Booking
        [Authorize]
        [HttpDelete("cancel")]
        public async Task<IActionResult> CancelBooking(
            [FromQuery] string email,
            [FromQuery] string venueName,
            [FromQuery] TimeSpan startTime,
            [FromQuery] TimeSpan endTime)
        {
            // Log incoming parameters
            Console.WriteLine($"CancelBooking called with email: {email}, venueName: {venueName}, startTime: {startTime}, endTime: {endTime}");

            var booking = await _context.Bookings
            .Include(b => b.TimeSlot)
            .FirstOrDefaultAsync(b => b.UserEmail == email &&
                          b.TimeSlot != null && b.TimeSlot.VenueName == venueName &&
                          b.TimeSlot.StartTime == startTime &&
                          b.TimeSlot.EndTime == endTime);

            if (booking == null)
            return NotFound("Booking not found.");

            _context.Bookings.Remove(booking);

            var timeSlot = await _context.TimeSlots.FindAsync(booking.TimeSlotID);
            if (timeSlot != null)
            timeSlot.IsAvailable = true;

            await _context.SaveChangesAsync();

            // Send cancellation email to user
            await SendBookingCancellationEmailAsync(email, booking);

            return Ok(new { message = "Booking canceled successfully." });
        }

        // Helper method to send cancellation email
        private async Task SendBookingCancellationEmailAsync(string toEmail, Booking booking)
        {
            using var message = new MailMessage
            {
                From = new MailAddress("hello@jibna.live", "UTM Booking System"),
                Subject = "Your Booking Has Been Cancelled",
                IsBodyHtml = true,
                Body = $@"
                  <div style='font-family:Arial,sans-serif;'>
                    <h2 style='color:#B22222;'>Booking Cancelled</h2>
                    <p>
                      Dear <b>{booking.UserEmail}</b>,<br/><br/>
                      Your booking for <b>{booking.TimeSlot?.VenueName}</b> on {booking.TimeSlot?.StartTime} - {booking.TimeSlot?.EndTime} has been <span style='color:red;font-weight:bold;'>cancelled</span> as per your request.<br/>
                      If this was a mistake, please make a new booking.<br/><br/>
                      <span style='color:#2E8B57;'><b>For refund, please contact <a href='mailto:staff@utm.my'>staff@utm.my</a> and provide them with the necessary information for your refund.</b></span><br/><br/>
                      <em>Thank you for using UTM Booking System!</em>
                    </p>
                  </div>"
            };
            message.To.Add(toEmail);

            using var smtp = new SmtpClient("live.smtp.mailtrap.io", 587)
            {
            Credentials = new NetworkCredential("api", "3b777591f83a047e2f6195eee833657e"),
            EnableSsl = true
            };
            await smtp.SendMailAsync(message);
        }
    }
}