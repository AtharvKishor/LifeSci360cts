using System.ComponentModel.DataAnnotations;

namespace Shared.CL.DTOs;

public class UpdateSiteDto
{
    [Required(ErrorMessage = "Site name is required.")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Location { get; set; }
}
