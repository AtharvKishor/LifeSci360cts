using System.ComponentModel.DataAnnotations;

namespace Shared.CL.DTOs;

public class UpdateProtocolStatusDto
{
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;
}
