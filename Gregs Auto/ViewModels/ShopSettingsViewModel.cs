using System.ComponentModel.DataAnnotations;
using Gregs_Auto.ViewModels.Validation;

namespace Gregs_Auto.ViewModels;

// The shop's settings form.
//
// There is deliberately no Tier property here. This is the one form a shop can
// post that reaches the shop record, so the safest way to stop a shop upgrading
// itself is for the binding target to have nowhere to put a tier — not a check
// somewhere that could be forgotten.
public class ShopSettingsViewModel
{
    [Required(ErrorMessage = "The shop needs a name.")]
    [StringLength(100)]
    [Display(Name = "Shop name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(30)]
    [RegularExpression(ValidationPatterns.Phone, ErrorMessage = ValidationPatterns.PhoneMessage)]
    public string? Phone { get; set; }

    [StringLength(200)]
    [Display(Name = "Street address")]
    public string? AddressLine { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [StringLength(20)]
    [Display(Name = "ZIP")]
    public string? PostalCode { get; set; }

    [Required]
    [Display(Name = "Timezone")]
    public string TimeZoneId { get; set; } = "America/Chicago";

    [Range(1, 50, ErrorMessage = "Between 1 and 50.")]
    [Display(Name = "Bays — how many jobs at once")]
    public int BayCount { get; set; } = 3;

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Opens")]
    public TimeOnly OpensAt { get; set; } = new(8, 0);

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Closes")]
    public TimeOnly ClosesAt { get; set; } = new(17, 0);

    [Display(Name = "Closed on")]
    public List<DayOfWeek> ClosedDays { get; set; } = new();

    // Shown, never posted — so the shop can see what they're on without the
    // form being able to change it.
    public string CurrentTier { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    // A short list beats every zone on the machine; these cover the continental US.
    public static readonly (string Id, string Label)[] TimeZoneChoices =
    {
        ("America/New_York", "Eastern"),
        ("America/Chicago", "Central"),
        ("America/Denver", "Mountain"),
        ("America/Phoenix", "Arizona (no daylight saving)"),
        ("America/Los_Angeles", "Pacific"),
        ("America/Anchorage", "Alaska"),
        ("Pacific/Honolulu", "Hawaii"),
    };

    public static readonly DayOfWeek[] WeekDays =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    };
}
