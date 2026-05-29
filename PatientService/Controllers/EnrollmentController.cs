using Microsoft.AspNetCore.Mvc;
using PatientService.Services;
using Shared;
using Shared.DTOs;

namespace PatientService.Controllers;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentController : ControllerBase
{
    private readonly IEnrollmentService _service;

    public EnrollmentController(IEnrollmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<EnrollmentDto>>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<EnrollmentDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EnrollmentDto>>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null)
            return NotFound(ApiResponse<EnrollmentDto>.Fail("Enrollment not found."));
        return Ok(ApiResponse<EnrollmentDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EnrollmentDto>>> Enroll(
        [FromBody] EnrollRequest req)
    {
        var (success, error, data) = await _service.EnrollAsync(
            req.PatientId, req.ProtocolSiteId);

        if (!success)
            return BadRequest(ApiResponse<EnrollmentDto>.Fail(error!));

        return CreatedAtAction(nameof(GetById),
            new { id = data!.EnrollmentId },
            ApiResponse<EnrollmentDto>.Ok(data, "Patient enrolled."));
    }

    [HttpPut("{id:guid}/withdraw")]
    public async Task<ActionResult<ApiResponse<string>>> Withdraw(Guid id)
    {
        var (success, error) = await _service.WithdrawAsync(id);

        if (!success)
            return error == "Enrollment not found."
                ? NotFound(ApiResponse<string>.Fail(error!))
                : BadRequest(ApiResponse<string>.Fail(error!));

        return Ok(ApiResponse<string>.Ok("WITHDRAWN", "Patient withdrawn."));
    }

    [HttpGet("protocols")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetProtocols()
    {
        var result = await _service.GetProtocolsAsync();
        return Ok(ApiResponse<IEnumerable<object>>.Ok(result));
    }

    [HttpGet("protocols/{protocolId:guid}/sites")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetSites(
        Guid protocolId)
    {
        var result = await _service.GetSitesByProtocolAsync(protocolId);
        return Ok(ApiResponse<IEnumerable<object>>.Ok(result));
    }

    // ✅ NEW: count of active patients enrolled in a protocol
    [HttpGet("protocols/{protocolId:guid}/active-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetActivePatientCount(
        Guid protocolId)
    {
        var count = await _service.GetActivePatientCountAsync(protocolId);
        return Ok(ApiResponse<int>.Ok(count));
    }
}

public record EnrollRequest(Guid PatientId, Guid ProtocolSiteId);