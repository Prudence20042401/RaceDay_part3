using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private const string SessionUserIdKey = "UserID";
        private const string SessionRoleKey = "Role";

        private readonly RaceDayContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _hasher = new();
        private readonly IDataProtector _resetProtector;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthController> _logger;

        // A password reset link stops working after this long.
        private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

        public AuthController(
            RaceDayContext context,
            IConfiguration configuration,
            IDataProtectionProvider dataProtectionProvider,
            IEmailSender emailSender,
            ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _resetProtector = dataProtectionProvider.CreateProtector("RaceDay.PasswordReset.v1");
            _emailSender = emailSender;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new account as an Organiser or a Participant.
        /// Participants must supply a date of birth in the past.
        /// Organisers must supply an organisation name, a phone number and the
        /// organiser invite code (so that not just anyone can become an Organiser).
        /// The password is hashed before it is stored.
        /// <response code="200">Account created.</response>
        /// <response code="400">Missing or invalid details.</response>
        /// <response code="403">Organiser invite code missing or wrong.</response>
        /// <response code="409">Email address already registered.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UserSummaryDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterDTO dto)
        {
            var email = dto.Email.Trim().ToLower();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return Conflict(new MessageDTO { Message = "Email is already registered." });

            var role = dto.Role.Trim();

            if (role != "Organiser" && role != "Participant")
                return BadRequest(new MessageDTO { Message = "Invalid role." });

            if (role == "Organiser")
            {
                if (!InviteCodeIsValid(dto.InviteCode))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new MessageDTO
                    {
                        Message = "A valid organiser invite code is required to register as an Organiser."
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.OrganisationName) ||
                    string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    return BadRequest(new MessageDTO
                    {
                        Message = "Organisers must provide an organisation name and phone number."
                    });
                }
            }

            if (role == "Participant" &&
                (!dto.DateOfBirth.HasValue ||
                 dto.DateOfBirth.Value.Date >= DateTime.UtcNow.Date))
            {
                return BadRequest(new MessageDTO
                {
                    Message = "Participants must provide a valid date of birth in the past."
                });
            }

            var user = new User
            {
                Email = email,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Role = role
            };

            user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (role == "Organiser")
            {
                _context.Organisers.Add(new Organiser
                {
                    UserID = user.UserID,
                    OrganisationName = dto.OrganisationName!.Trim(),
                    PhoneNumber = dto.PhoneNumber!.Trim()
                });
            }
            else
            {
                _context.Participants.Add(new Participant
                {
                    UserID = user.UserID,
                    DateOfBirth = dto.DateOfBirth!.Value.Date
                });
            }

            await _context.SaveChangesAsync();

            return Ok(ToSummary(user, "Registration successful."));
        }

        /// <summary>
        /// Logs in and starts a server-side session holding the user's ID and role.
        ///
        /// </summary>
        /// <response code="200">Logged in. The session cookie is set.</response>
        /// <response code="401">Email or password is wrong.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(UserSummaryDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            var email = dto.Email.Trim().ToLower();

            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return Unauthorized(new MessageDTO { Message = "Invalid email or password." });

            var result = _hasher.VerifyHashedPassword(
                user, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(new MessageDTO { Message = "Invalid email or password." });

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Name, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            // Server-side session: user ID and role are kept on the server.
            HttpContext.Session.SetInt32(SessionUserIdKey, user.UserID);
            HttpContext.Session.SetString(SessionRoleKey, user.Role);

            return Ok(ToSummary(user, "Login successful."));
        }

        /// <summary>
        /// Returns the user ID and role stored in the current server-side session.
        /// </summary>
        /// <response code="200">The session details.</response>
        /// <response code="401">Not logged in, or the session has expired.</response>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(SessionInfoDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Me()
        {
            var userId = HttpContext.Session.GetInt32(SessionUserIdKey);
            var role = HttpContext.Session.GetString(SessionRoleKey);

            if (userId == null || string.IsNullOrEmpty(role))
                return Unauthorized(new MessageDTO { Message = "Session expired. Please log in again." });

            return Ok(new SessionInfoDTO { UserID = userId.Value, Role = role });
        }

        /// <summary>
        /// Logs out and clears the server-side session.
        /// </summary>
        /// <response code="200">Logged out.</response>
        [HttpPost("logout")]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new MessageDTO { Message = "Logged out successfully." });
        }

        /// <summary>
        /// Starts a password reset. If an account exists for the email, a reset link
        /// that is valid for one hour and can be used once is emailed to it.
        /// The response is identical whether or not the email is registered, so the
        /// endpoint cannot be used to find out who has an account.
        /// </summary>
        /// <response code="200">Request accepted (same message every time).</response>
        /// <response code="400">The email address is missing or not a valid email.</response>
        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ForgotPasswordResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO dto)
        {
            var response = new ForgotPasswordResponseDTO
            {
                Message = "If an account exists for that email address, a password reset link has been sent."
            };

            var email = dto.Email.Trim().ToLower();
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return Ok(response);

            var link = BuildResetLink(CreateResetToken(user));

            var sent = await _emailSender.SendAsync(
                user.Email,
                "Reset your RaceDay password",
                $"Hi {user.FirstName},\n\n" +
                "We received a request to reset your RaceDay password. " +
                $"Use the link below within 1 hour:\n\n{link}\n\n" +
                "If you did not ask for this, you can safely ignore this email.");

            if (!sent)
                _logger.LogWarning("No SMTP server configured. Password reset link for {Email}: {Link}", user.Email, link);

            // Demo mode only: lets the app show the link when there is no email server.
            if (_configuration.GetValue<bool>("PasswordReset:ShowLinkInResponse"))
                response.ResetLink = link;

            return Ok(response);
        }

        /// <summary>
        /// Sets a new password using the token from the emailed reset link.
        /// The token expires after one hour and stops working as soon as the
        /// password has been changed. The new password is hashed before it is stored.
        /// </summary>
        /// <response code="200">Password changed. The user can now log in.</response>
        /// <response code="400">The link is invalid or expired, or the new password is too short.</response>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO dto)
        {
            var user = await ReadResetTokenAsync(dto.Token);

            if (user == null)
                return BadRequest(new MessageDTO
                {
                    Message = "This password reset link is invalid or has expired. Please request a new one."
                });

            user.PasswordHash = _hasher.HashPassword(user, dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new MessageDTO { Message = "Your password has been reset. You can now log in." });
        }

        /// <summary>
        /// Token = protected "userId|expiryTicks|fingerprint". The fingerprint is a hash of the
        /// current password hash, so the token becomes useless once the password changes.
        /// Nothing is stored in the database.
        /// </summary>
        private string CreateResetToken(User user)
        {
            var expires = DateTime.UtcNow.Add(ResetTokenLifetime).Ticks;
            return _resetProtector.Protect($"{user.UserID}|{expires}|{Fingerprint(user)}");
        }

        private async Task<User?> ReadResetTokenAsync(string token)
        {
            try
            {
                var parts = _resetProtector.Unprotect(token.Trim()).Split('|');
                if (parts.Length != 3 ||
                    !int.TryParse(parts[0], out var userId) ||
                    !long.TryParse(parts[1], out var expiryTicks))
                    return null;

                if (DateTime.UtcNow.Ticks > expiryTicks)
                    return null;

                var user = await _context.Users.FindAsync(userId);
                if (user == null || Fingerprint(user) != parts[2])
                    return null;

                return user;
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                // Tampered, malformed or made by a different key.
                return null;
            }
        }

        private static string Fingerprint(User user) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(user.PasswordHash)))[..16];

        private string BuildResetLink(string token)
        {
            var baseUrl = _configuration["PasswordReset:ResetUrlBase"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                baseUrl = "http://localhost:5200/Account/ResetPassword";

            return $"{baseUrl}?token={Uri.EscapeDataString(token)}";
        }

        private bool InviteCodeIsValid(string? supplied)
        {
            var expected = _configuration["OrganiserInviteCode"];

            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(supplied))
                return false;

            // Fixed-time comparison so the code cannot be guessed by timing.
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(supplied.Trim()),
                Encoding.UTF8.GetBytes(expected.Trim()));
        }

        private static UserSummaryDTO ToSummary(User user, string message) => new()
        {
            Message = message,
            UserID = user.UserID,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role
        };
    }
}
