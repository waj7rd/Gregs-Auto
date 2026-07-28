using System.ComponentModel.DataAnnotations;
using Gregs_Auto.ViewModels.Validation;

namespace Gregs_Auto.ViewModels;

// The public booking form. A visitor has no account, so they describe
// themselves and their car rather than picking from the shop's records —
// which is what stops the page having to show anyone else's details.
public class GuestBookingViewModel
{
    [Required(ErrorMessage = "We need a name to put on the job.")]
    [StringLength(100)]
    [Display(Name = "Your name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "We need a phone number to confirm your appointment.")]
    [StringLength(30)]
    [RegularExpression(ValidationPatterns.Phone, ErrorMessage = ValidationPatterns.PhoneMessage)]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(255)]
    [EmailAddress]
    [Display(Name = "Email (optional)")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "What year is the vehicle?")]
    [VehicleYear]
    [Display(Name = "Year")]
    public short VehicleYear { get; set; } = (short)DateTime.Now.Year;

    [Required(ErrorMessage = "What make is it?")]
    [StringLength(50)]
    [Display(Name = "Make")]
    public string VehicleMake { get; set; } = string.Empty;

    [Required(ErrorMessage = "What model is it?")]
    [StringLength(50)]
    [Display(Name = "Model")]
    public string VehicleModel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Pick the service you need.")]
    [Display(Name = "Service")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Pick a date and time.")]
    [Display(Name = "Preferred date & time")]
    public DateTime RequestedAt { get; set; }

    [StringLength(500)]
    [Display(Name = "Anything we should know?")]
    public string? Notes { get; set; }

    // Honeypot. Hidden from people by CSS and skipped by keyboard navigation, so
    // a human never fills it in. Bots fill every field they find, which makes a
    // non-empty value here a reliable "this wasn't a person" signal.
    //
    // Named to look worth filling in — "Website" is a field a scraper expects.
    public string? Website { get; set; }
}

// One row in the staff queue of incoming requests.
public class BookingRequestRowViewModel
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string VehicleDescription { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = string.Empty;
    public string? HandledByName { get; set; }
    public DateTime? HandledAt { get; set; }
}

public class BookingRequestsViewModel
{
    public List<BookingRequestRowViewModel> Pending { get; set; } = new();
    public List<BookingRequestRowViewModel> RecentlyHandled { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}
