using System.ComponentModel.DataAnnotations;

namespace Gregs_Auto.ViewModels;

// One row in the Services listing — public price list and staff catalog alike.
public class ServiceRowViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public decimal Price { get; set; }

    // Always true on the public list; the staff catalog shows archived ones too.
    public bool IsActive { get; set; } = true;
}

public class ServiceCatalogViewModel
{
    public List<ServiceRowViewModel> Services { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

// Create and Edit share a form — the only difference is whether ServiceId is set.
public class EditServiceViewModel
{
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Give the service a name.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description (optional)")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "How long does it take?")]
    [Range(5, 480, ErrorMessage = "Between 5 minutes and 8 hours.")]
    [Display(Name = "How long it takes (minutes)")]
    public int EstimatedDurationMinutes { get; set; } = 30;

    [Required(ErrorMessage = "Set a price. Use 0 for something you don't charge for.")]
    [Range(0, 100000, ErrorMessage = "Price can't be negative.")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    public string? ErrorMessage { get; set; }

    public bool IsNew => ServiceId == 0;
}
