using System.Linq;
using System.Text;
using api.Data;
using api.Dtos;
using api.Dtos.Users;
using api.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Net.Mail;
using System.Net;

namespace api.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDBcontext _context;
        private static bool IsMaintenanceModeEnabled = false; // Maintenance mode flag

        public UsersController(ApplicationDBcontext context)
        {
            _context = context;
        }

        // Admin endpoint to toggle maintenance mode
        [Authorize(Roles = "admin")]
        [HttpPost("toggle-maintenance-mode")]
        public IActionResult ToggleMaintenanceMode([FromBody] bool enable)
        {
            IsMaintenanceModeEnabled = enable;
            return Ok(new { maintenanceModeEnabled = IsMaintenanceModeEnabled });
        }

        // Endpoint to check maintenance mode status (optional, for frontend)
        [Authorize(Roles = "admin")]
        [HttpGet("maintenance-mode-status")]
        public IActionResult GetMaintenanceModeStatus()
        {
            return Ok(new { maintenanceModeEnabled = IsMaintenanceModeEnabled });
        }

        [HttpGet]
        public async Task<IActionResult> GetALL()
        {
            var users = await _context.Users.ToListAsync();
            var userDtos = users.Select(s => s.ToUserDtos());
            
            return Ok(userDtos);
        }

        [HttpGet("{email}")]
        public async Task<IActionResult> GetByEmail([FromRoute] string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user.ToUserDtos());
        }
        
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDtos UsersDtos)
        {
            var userModel = UsersDtos.ToCreateUserDtos();
            userModel.Rolebase = "student";

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();
            userModel.RegistrationOtp = otp;
            userModel.RegistrationOtpExpiry = DateTime.UtcNow.AddMinutes(10);
            userModel.IsEmailVerified = false;

            await _context.Users.AddAsync(userModel);
            await _context.SaveChangesAsync();

            // Send OTP email
            using var message = new MailMessage
            {
                From = new MailAddress("hello@demomailtrap.co", "UTM Booking System"),
                Subject = "Your Registration OTP",
                Body = $"Your OTP for registration is: {otp}",
                IsBodyHtml = false
            };
            message.To.Add(userModel.Email);

            using var smtp = new SmtpClient("live.smtp.mailtrap.io", 587)
            {
                Credentials = new NetworkCredential("api", "49e7a9091f9f7bf35da2118d87f761e7"),
                EnableSsl = true
            };
            await smtp.SendMailAsync(message);

            return CreatedAtAction(nameof(GetByEmail), new { email = userModel.Email }, userModel.ToUserDtos());
        }
    
        [HttpPut("{Email}")]
        public async Task<IActionResult> Update([FromRoute] string Email, [FromBody] UpdateUserDtos userDtos)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == Email);

            if (user == null)
            {
                return NotFound("User not found");
            }

            user.Name = userDtos.Name;
            user.Password = userDtos.Password;
            user.Rolebase = userDtos.Rolebase;

            await _context.SaveChangesAsync();
            return Ok(user.ToUserDtos());
        }

        [HttpDelete("{Email}")]
        public async Task<IActionResult> Delete([FromRoute] string Email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == Email);

            if (user == null)
            {
                return NotFound("User not found");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Login endpoint with maintenance mode check
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDtos loginUser)
        {
            // Check if maintenance mode is enabled
            if (IsMaintenanceModeEnabled && loginUser.Rolebase != "admin")
            {
                return StatusCode(503, "The system is currently under maintenance. Only admins can log in.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.Email == loginUser.Email &&
                x.Rolebase == loginUser.Rolebase);

            if (user == null)
            {
                return Unauthorized("Invalid email or role");
            }

            // Check if lockout is enabled and user is locked out
            if (user.IsLoginLockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                return Unauthorized($"Account locked. Try again at {user.LockoutEnd.Value.ToLocalTime()}.");
            }

            // Check password
            if (user.Password != loginUser.Password)
            {
                if (user.IsLoginLockoutEnabled)
                {
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddHours(1);
                        await _context.SaveChangesAsync();
                        return Unauthorized("Too many failed attempts. Account locked for 1 hour.");
                    }
                }
                await _context.SaveChangesAsync();
                return Unauthorized("Invalid password.");
            }

            // Successful login: reset attempts and lockout
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();

            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Rolebase)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(2)
            };

            // Sign in the user
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return Ok(new { message = "Login successful", user = user.ToUserDtos() });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully" });
        }

        [Authorize(Roles = "admin,staff")]
        [HttpGet("admin-staff-only")]
        public IActionResult AdminStaffOnlyEndpoint()
        {
            return Ok(new { message = "This is an admin/staff-only endpoint." });
        }

        [Authorize(Roles = "student")]
        [HttpGet("student-only")]
        public IActionResult StudentOnlyEndpoint()
        {
            return Ok(new { message = "This is a student-only endpoint." });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var email = User.Identity?.Name;
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return Ok(new { email, role });
        }

        [Authorize(Roles = "admin")]
        [HttpPost("toggle-login-lockout")]
        public async Task<IActionResult> ToggleLoginLockout([FromBody] bool enable)
        {
            // You can apply globally or per user
            // Example: apply to all users
            var users = await _context.Users.ToListAsync();
            foreach (var user in users)
                user.IsLoginLockoutEnabled = enable;
            await _context.SaveChangesAsync();
            return Ok(new { enabled = enable });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return NotFound("User not found.");

            if (user.RegistrationOtp != dto.Otp || user.RegistrationOtpExpiry < DateTime.UtcNow)
                return BadRequest("Invalid or expired OTP.");

            user.IsEmailVerified = true;
            user.RegistrationOtp = null;
            user.RegistrationOtpExpiry = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Email verified successfully." });
        }

        [HttpPost("request-registration-otp")]
        public async Task<IActionResult> RequestRegistrationOtp([FromBody] RequestOtpDto dto)
        {
            // Check if OTP was already sent recently (optional, to prevent spam)
            // You can use a separate table or in-memory cache for production

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            // Send OTP email
            using var message = new MailMessage
            {
                From = new MailAddress("hello@demomailtrap.co", "UTM Booking System"),
                Subject = "Your Registration OTP",
                Body = $"Your OTP for registration is: {otp}",
                IsBodyHtml = false
            };
            message.To.Add(dto.Email);

            using var smtp = new SmtpClient("live.smtp.mailtrap.io", 587)
            {
                Credentials = new NetworkCredential("api", "49e7a9091f9f7bf35da2118d87f761e7"),
                EnableSsl = true
            };
            await smtp.SendMailAsync(message);

            // Store OTP and expiry temporarily (for demo, use a static dictionary)
            // In production, use a cache or a dedicated table
            OtpStore[dto.Email] = (otp, expiry);

            return Ok(new { message = "OTP sent to your email." });
        }

        // Temporary in-memory OTP store (for demo only)
        private static Dictionary<string, (string Otp, DateTime Expiry)> OtpStore = new();

        // DTO for request
        public class RequestOtpDto
        {
            public string Email { get; set; }
        }

        public class VerifyOtpDto
        {
            public string Email { get; set; }
            public string Otp { get; set; }
        }
    }
}