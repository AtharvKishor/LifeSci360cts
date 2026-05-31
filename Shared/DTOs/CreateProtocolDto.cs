using System.ComponentModel.DataAnnotations;

namespace Shared.CL.DTOs;

public class CreateProtocolDto : IValidatableObject
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phase is required.")]
    [MaxLength(50, ErrorMessage = "Phase cannot exceed 50 characters.")]
    public string Phase { get; set; } = string.Empty;

    [MaxLength(4000, ErrorMessage = "Description cannot exceed 4000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    public DateTime EndDate { get; set; }

    public string Status { get; set; } = ProtocolStatus.Upcoming;

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (EndDate.Date < StartDate.Date)
        {
            yield return new ValidationResult(
                "End date cannot be earlier than start date.",
                new[] { nameof(EndDate) });
        }

        var upper = Status.ToUpperInvariant();
        if (upper != ProtocolStatus.Upcoming && upper != ProtocolStatus.Ongoing)
        {
            yield return new ValidationResult(
                "Status must be UPCOMING or ONGOING at creation.",
                new[] { nameof(Status) });
        }
    }
}
