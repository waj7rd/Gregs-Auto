using System.ComponentModel.DataAnnotations;

namespace Gregs_Auto.ViewModels;

// One row in the staff list.
public class StaffRowViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // True only while a lockout is still in force.
    public bool IsLockedOut { get; set; }

    // True for the row representing whoever is looking at the page — used to
    // stop someone deactivating themselves by accident.
    public bool IsCurrentUser { get; set; }
}

public class StaffListViewModel
{
    public List<StaffRowViewModel> Staff { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public class CreateStaffViewModel
{
    [Required(ErrorMessage = "Enter their name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter an email address — it's what they sign in with.")]
    [StringLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = Gregs_Auto.Domain.Identity.Roles.Technician;

    [Required(ErrorMessage = "Set a starting password.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Use at least 10 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Starting password")]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

public class EditStaffViewModel
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Enter their name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter an email address.")]
    [StringLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

public class ResetStaffPasswordViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a new password.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Use at least 10 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm the new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "The two passwords don't match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

// One row on the login activity page.
public class LoginActivityRowViewModel
{
    public DateTime OccurredAt { get; set; }
    public string EmailAttempted { get; set; } = string.Empty;

    // Null when the attempt was against an address with no account.
    public string? UserFullName { get; set; }

    public string Event { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}
