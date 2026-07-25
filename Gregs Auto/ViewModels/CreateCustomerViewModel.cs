using System.ComponentModel.DataAnnotations;

namespace Gregs_Auto.ViewModels;

public class CreateCustomerViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(255)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }
}
