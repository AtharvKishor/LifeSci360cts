using Microsoft.AspNetCore.Mvc;
using PatientService.Services;
using Shared.CL;

namespace PatientService.Controllers;

[ApiController]
[Route("api/visit-review")]
public class VisitReviewController : ControllerBase
{
    private readonly IVisitService _service;

    public VisitReviewController(IVisitService service)
    {
        _service = service;
    }

    [HttpPost("submit")]
    public async Task<ActionResult<ApiResponse<string>>> Submit(
        [FromBody] VisitReviewSubmitRequest req)
    {
        var message = await _service.SubmitReviewAsync(
            req.Date, req.ProtocolSiteId, req.AttendedVisitIds);

        return Ok(ApiResponse<string>.Success("Saved", message));
    }
}

public record VisitReviewSubmitRequest(
    DateTime Date,
    Guid? ProtocolSiteId,
    List<Guid> AttendedVisitIds);