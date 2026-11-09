using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class LoginViewModel { [Required, EmailAddress] public string Email { get; set; } = ""; [Required] public string Password { get; set; } = ""; }
public class RegisterViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
    public DateTime? DateOfBirth { get; set; }
    public string? OrganisationName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? InviteCode { get; set; }
}
public class ForgotPasswordViewModel { [Required, EmailAddress] public string Email { get; set; } = ""; public string? Message { get; set; } public string? ResetLink { get; set; } }
public class ForgotPasswordResponse { public string Message { get; set; } = ""; public string? ResetLink { get; set; } }
public class ResetPasswordViewModel
{
    [Required] public string Token { get; set; } = "";
    [Required, MinLength(8)] [Display(Name = "New password (minimum 8 characters)")] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")] [Display(Name = "Confirm new password")] public string ConfirmPassword { get; set; } = "";
}
public class UserSummary { public string Message { get; set; } = ""; public int UserID { get; set; } public string Email { get; set; } = ""; public string FirstName { get; set; } = ""; public string LastName { get; set; } = ""; public string Role { get; set; } = ""; }
public class EventDto { public int EventID { get; set; } public int OrganiserID { get; set; } [Required] public string Name { get; set; } = ""; [Required] public string Description { get; set; } = ""; [Required, DataType(DataType.Date)] public DateTime? EventDate { get; set; } [Required] public string Location { get; set; } = ""; [Range(.1,1000)] public decimal Distance { get; set; } [Required] public string EventType { get; set; } = "Run"; public int EnrolmentCount { get; set; } public string? ImageURL { get; set; } }
public class CategoryDto { public int CategoryID { get; set; } public int EventID { get; set; } public string Name { get; set; } = ""; public string? Description { get; set; } }
public class EnrolmentDto { public int EnrolmentID { get; set; } public int EventID { get; set; } public string EventName { get; set; } = ""; public DateTime EventDate { get; set; } public int CategoryID { get; set; } public string CategoryName { get; set; } = ""; public int ParticipantID { get; set; } public string ParticipantName { get; set; } = ""; public string Status { get; set; } = "Pending"; public DateTime EnrolmentDate { get; set; } }
public class ResultDto { public int ResultID { get; set; } public int EnrolmentID { get; set; } public int EventID { get; set; } public string EventName { get; set; } = ""; public DateTime EventDate { get; set; } public string CategoryName { get; set; } = ""; public string ParticipantName { get; set; } = ""; public TimeSpan? FinishTime { get; set; } public int? FinishingPosition { get; set; } public int TotalFinishers { get; set; } public DateTime RecordedAt { get; set; } }
public class ProfileDto { public int UserID { get; set; } public string Email { get; set; } = ""; public string FirstName { get; set; } = ""; public string LastName { get; set; } = ""; public string Role { get; set; } = ""; public string? OrganisationName { get; set; } public string? PhoneNumber { get; set; } public DateTime? DateOfBirth { get; set; } public string? ProfilePictureURL { get; set; } }
public class EnrolViewModel { public int EventID { get; set; } public int CategoryID { get; set; } }
public class ResultEntryViewModel { public int EnrolmentID { get; set; } [Required] public TimeSpan? FinishTime { get; set; } [Required, Range(1,1000000)] public int? FinishingPosition { get; set; } }
public class ProfileEditViewModel { [Required] public string FirstName { get; set; } = ""; [Required] public string LastName { get; set; } = ""; public string? OrganisationName { get; set; } public string? PhoneNumber { get; set; } public DateTime? DateOfBirth { get; set; } }
