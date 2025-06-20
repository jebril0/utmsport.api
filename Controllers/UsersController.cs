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
            userModel.IsEmailVerified = false;

            await _context.Users.AddAsync(userModel);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByEmail), new { email = userModel.Email }, userModel.ToUserDtos());
        }

        [HttpPost("create-for-admin")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create_for_admin([FromBody] CreateUserDtos UsersDtos)
        {
            var userModel = UsersDtos.ToCreateUserDtos();
            userModel.Rolebase = "student";
            userModel.IsEmailVerified = true;

            await _context.Users.AddAsync(userModel);
            await _context.SaveChangesAsync();

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

            // Check if the email is verified
            if (!user.IsEmailVerified)
            {
                return Unauthorized("Email not verified. Please verify your email before logging in.");
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

        // Add OTP generation endpoint
        [HttpPost("generate-otp/{email}")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateOtp([FromRoute] string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
            {
                return NotFound("User not found");
            }

           
            var random = new Random();
            user.RegistrationOtp = random.Next(100000, 999999); 
            user.RegistrationOtpExpiry = DateTime.UtcNow.AddMinutes(2); 

            await _context.SaveChangesAsync();

            // Send OTP via email using MailTrap SMTP
            using var message = new MailMessage
            {
                From = new MailAddress("hello@jibna.live", "UTM Booking System"),
                IsBodyHtml = true,
                Subject = "🔐 Your OTP Code for UTM Booking",
                Body = $@"
                  <div style='font-family:Arial,sans-serif;'>
                    <h2 style='color:#2E8B57;'>UTM Email Verification</h2>
                    <p>
                      Dear <b>{user.Name ?? user.Email}</b>,<br/><br/>
                      Your One-Time Password (OTP) for email verification is:<br/>
                      <span style='font-size:1.5em;color:#2E8B57;font-weight:bold;'>{user.RegistrationOtp}</span><br/><br/>
                      This code will expire in 2 minutes.<br/><br/>
                      <em>If you did not request this, please ignore this email.</em>
                    </p>
                  </div>"
            };
            message.To.Add(user.Email);

            using var smtp = new SmtpClient("live.smtp.mailtrap.io", 587)
            {
                Credentials = new NetworkCredential("api", "3b777591f83a047e2f6195eee833657e"),
                EnableSsl = true
            };
            await smtp.SendMailAsync(message);

            // Schedule deletion of unverified user after 1 minute
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(1));

                using (var scope = HttpContext.RequestServices.CreateScope())
                {
                    try
                    {
                        var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDBcontext>();
                        // Updated logic to delete all users with expired OTPs and unverified emails after 1 minute
                        var usersToDelete = await scopedContext.Users.Where(u => !u.IsEmailVerified && u.RegistrationOtpExpiry < DateTime.UtcNow).ToListAsync();
                        if (usersToDelete.Any())
                        {
                            scopedContext.Users.RemoveRange(usersToDelete);
                            await scopedContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting unverified users: {ex.Message}");
                    }
                }
            });

            return Ok(new { message = "OTP sent successfully" });
        }

        // Add OTP verification endpoint
        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            // Log email and OTP for debugging
            Console.WriteLine($"Verifying OTP for email: {request.Email}, OTP: {request.Otp}");

            // Ensure email is not null and normalize it
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email cannot be null or empty.");
            }

            var email = request.Email.Trim().ToLower();

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
            {
                return NotFound("User not found");
            }

            // Check OTP validity
            if (user.RegistrationOtp != request.Otp || user.RegistrationOtpExpiry < DateTime.UtcNow)
            {
                // Delete user if OTP is invalid or expired
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return BadRequest("Invalid or expired OTP. Your account has been deleted.");
            }

            // Mark email as verified
            user.IsEmailVerified = true;
            user.RegistrationOtp = null;
            user.RegistrationOtpExpiry = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Email verified successfully" });
        }

        // Add endpoint to request OTP for password reset
        [HttpPost("request-password-reset-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestPasswordResetOtp([FromBody] string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
            {
                return NotFound("User not found");
            }

            // Generate OTP
            var random = new Random();
            user.PasswordResetOtp = random.Next(100000, 999999); // 6-digit OTP
            user.PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(5); // OTP valid for 5 minutes

            await _context.SaveChangesAsync();

            // Send OTP via email
            using var message = new MailMessage
            {
                From = new MailAddress("hello@jibna.live", "UTM Booking System"),
                IsBodyHtml = true,
                Subject = "🔐 Your Password Reset OTP",
                Body = $@"
                  <div style='font-family:Arial,sans-serif;'>
                    <h2 style='color:#2E8B57;'>Password Reset Request</h2>
                    <p>
                      Dear <b>{user.Name ?? user.Email}</b>,<br/><br/>
                      Your One-Time Password (OTP) for password reset is:<br/>
                      <span style='font-size:1.5em;color:#2E8B57;font-weight:bold;'>{user.PasswordResetOtp}</span><br/><br/>
                      This code will expire in 5 minutes.<br/><br/>
                      <em>If you did not request this, please ignore this email.</em>
                    </p>
                  </div>"
            };
            message.To.Add(user.Email);

            using var smtp = new SmtpClient("live.smtp.mailtrap.io", 587)
            {
                Credentials = new NetworkCredential("api", "3b777591f83a047e2f6195eee833657e"),
                EnableSsl = true
            };
            await smtp.SendMailAsync(message);

            return Ok(new { message = "OTP sent successfully" });
        }

        // Add endpoint to reset password using OTP
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email);

            if (user == null)
            {
                return NotFound("User not found");
            }

            // Validate OTP
            if (user.PasswordResetOtp != request.Otp || user.PasswordResetOtpExpiry < DateTime.UtcNow)
            {
                return BadRequest("Invalid or expired OTP");
            }

            // Reset password
            user.Password = request.NewPassword;
            user.PasswordResetOtp = null;
            user.PasswordResetOtpExpiry = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully" });
        }
    }
}