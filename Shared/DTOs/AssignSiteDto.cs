using System.ComponentModel.DataAnnotations;

namespace Shared.CL.DTOs;

public class AssignSiteDto
{
    [Required(ErrorMessage = "Site ID is required.")]
    public Guid SiteId { get; set; }

    [Required(ErrorMessage = "Investigator user ID is required.")]
    public Guid InvestigatorUserId { get; set; }
}
