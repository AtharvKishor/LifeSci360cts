using Microsoft.AspNetCore.Mvc;
using PatientService.Services;
using Shared;
using Shared.DTOs;

namespace PatientService.Controllers;

[ApiController]
[Route("api/visits")]
public class VisitController : ControllerBase
{
    private readonly IVisitService _service;

    public VisitController(IVisitService service)
    {
        _service = service;
    }

    [HttpGet("enrollment/{enrollmentId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetByEnrollment(
        Guid enrollmentId)
    {
        var result = await _service.GetByEnrollmentAsync(enrollmentId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<VisitDto>>>> GetFiltered(
        [FromQuery] DateTime? date,
        [FromQuery] Guid? protocolSiteId,
        [FromQuery] string? status)
    {
        var result = await _service.GetFilteredAsync(date, protocolSiteId, status);
        return Ok(ApiResponse<IEnumerable<VisitDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VisitDto>>> AddVisit(
        [FromBody] AddVisitRequest req)
    {
        var (success, error, data) = await _service.AddAsync(
            req.EnrollmentId, req.VisitName, req.VisitDate);

        if (!success)
            return NotFound(ApiResponse<VisitDto>.Fail(error!));

        return Ok(ApiResponse<VisitDto>.Ok(data!, "Visit added."));
    }

    [HttpPut("{id:guid}/reschedule")]
    public async Task<ActionResult<ApiResponse<VisitDto>>> Reschedule(
        Guid id, [FromBody] RescheduleRequest req)
    {
        var (success, error, data) = await _service.RescheduleAsync(id, req.NewDate);

        if (!success)
            return NotFound(ApiResponse<VisitDto>.Fail(error!));

        return Ok(ApiResponse<VisitDto>.Ok(data!, "Visit rescheduled."));
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<string>>> Cancel(Guid id)
    {
        var (success, error) = await _service.CancelAsync(id);

        if (!success)
            return NotFound(ApiResponse<string>.Fail(error!));

        return Ok(ApiResponse<string>.Ok("CANCELLED",
            "Visit cancelled. Record preserved in DB."));
    }

    // ✅ NEW: bulk schedule visits for all active patients in a protocol
    [HttpPost("bulk-schedule")]
    public async Task<ActionResult<ApiResponse<string>>> BulkSchedule(
        [FromBody] BulkScheduleRequest req)
    {
        var (success, error, count) = await _service.BulkScheduleAsync(
            req.ProtocolId, req.Visits);

        if (!success)
            return BadRequest(ApiResponse<string>.Fail(error!));

        return Ok(ApiResponse<string>.Ok(
            $"{count} visit records created successfully."));
    }
}

public record AddVisitRequest(Guid EnrollmentId, string VisitName, DateTime VisitDate);
public record RescheduleRequest(DateTime NewDate);

// ✅ NEW
public record BulkScheduleRequest(
    Guid ProtocolId,
    List<BulkVisitItem> Visits);

