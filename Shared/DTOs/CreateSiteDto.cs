using System.ComponentModel.DataAnnotations;

namespace Shared.CL.DTOs;

public class CreateSiteDto
{
    [Required(ErrorMessage = "Site name is required.")]
    [MaxLength(150, ErrorMessage = "Site name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255, ErrorMessage = "Location cannot exceed 255 characters.")]
    public string? Location { get; set; }
}
