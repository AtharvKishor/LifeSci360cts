using Microsoft.AspNetCore.Mvc;
using PatientService.Services;
using Shared;
using Shared.DTOs;

namespace PatientService.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _service;

    public PatientController(IPatientService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PatientDto>>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<PatientDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null)
            return NotFound(ApiResponse<PatientDto>.Fail("Patient not found."));
        return Ok(ApiResponse<PatientDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PatientDto>>> Create(
        [FromBody] CreatePatientRequest req)
    {
        var (success, error, data) = await _service.CreateAsync(
            req.Name, req.DateOfBirth, req.ContactInfo);

        if (!success)
            return BadRequest(ApiResponse<PatientDto>.Fail(error!));

        return CreatedAtAction(nameof(GetById),
            new { id = data!.PatientId },
            ApiResponse<PatientDto>.Ok(data, "Patient created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> Update(
        Guid id, [FromBody] UpdatePatientRequest req)
    {
        var (success, error, data) = await _service.UpdateAsync(
            id, req.Name, req.DateOfBirth, req.ContactInfo);

        if (!success)
            return error == "Patient not found."
                ? NotFound(ApiResponse<PatientDto>.Fail(error!))
                : BadRequest(ApiResponse<PatientDto>.Fail(error!));

        return Ok(ApiResponse<PatientDto>.Ok(data!, "Patient updated."));
    }

    // ✅ NEW: deactivate patient
    [HttpPut("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<string>>> Deactivate(
        Guid id, [FromBody] DeactivatePatientRequest req)
    {
        var (success, error) = await _service.DeactivateAsync(id, req.Reason);

        if (!success)
            return error == "Patient not found."
                ? NotFound(ApiResponse<string>.Fail(error!))
                : BadRequest(ApiResponse<string>.Fail(error!));

        return Ok(ApiResponse<string>.Ok("INACTIVE", "Patient deactivated."));
    }
}

public record CreatePatientRequest(
    string Name, DateOnly DateOfBirth, string? ContactInfo);

public record UpdatePatientRequest(
    string Name, DateOnly DateOfBirth, string? ContactInfo);

// ✅ NEW
public record DeactivatePatientRequest(string Reason);