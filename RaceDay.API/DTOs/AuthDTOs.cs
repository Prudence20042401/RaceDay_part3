using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    /// <summary>Details needed to create a new account.</summary>
    public class RegisterDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        /// <summary>Either "Organiser" or "Participant".</summary>
        [Required]
        [RegularExpression("^(Organiser|Participant)$",
            ErrorMessage = "Role must be Organiser or Participant.")]
        public string Role { get; set; } = "Participant";

        /// <summary>Required for Organisers only.</summary>
        public string? OrganisationName { get; set; }

        /// <summary>Required for Organisers only.</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>Required for Participants only. Must be in the past.</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Required for Organisers only. Secret code given out by the RaceDay
        /// administrators so that not just anyone can create an Organiser account.
        /// </summary>
        public string? InviteCode { get; set; }
    }

    /// <summary>Credentials used to log in.</summary>
    public class LoginDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>The session held on the server for the logged-in user.</summary>
    public class SessionInfoDTO
    {
        public int UserID { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>Basic details of a user, returned after register and login.</summary>
    public class UserSummaryDTO
    {
        public string Message { get; set; } = string.Empty;
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>The email address of the account that forgot its password.</summary>
    public class ForgotPasswordDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>Response to a forgot-password request. Always the same message, whether or not the email exists.</summary>
    public class ForgotPasswordResponseDTO
    {
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The reset link. Only filled in when PasswordReset:ShowLinkInResponse is true
        /// (demo / development mode with no email server). Null in production.
        /// </summary>
        public string? ResetLink { get; set; }
    }

    /// <summary>The reset token from the emailed link and the new password.</summary>
    public class ResetPasswordDTO
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>A simple message returned by the API.</summary>
    public class MessageDTO
    {
        public string Message { get; set; } = string.Empty;
    }
}
