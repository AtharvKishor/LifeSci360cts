using System.ComponentModel.DataAnnotations;

namespace Shared.CL.DTOs;

public class UpdateProtocolDto : IValidatableObject
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phase is required.")]
    [MaxLength(50)]
    public string Phase { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    public DateTime EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (EndDate.Date < StartDate.Date)
        {
            yield return new ValidationResult(
                "End date cannot be earlier than start date.",
                new[] { nameof(EndDate) });
        }
    }
}
