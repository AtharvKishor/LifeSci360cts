//receives requests and send responses
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleService.Services;
using Shared.CL;
using Shared.CL.DTOs;

namespace SampleService.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController] //enables auto validation
public class SamplesController : ControllerBase
{
    private readonly ISampleService _service;

    public SamplesController(ISampleService service) => _service = service;

    // GET /api/samples — all authenticated users can view
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IList<SampleListDto>>>> GetAll()
    {
        IList<SampleListDto> samples = await _service.GetAllAsync();
        return Ok(ApiResponse<IList<SampleListDto>>.Success(samples, $"{samples.Count} sample(s) found."));
    }

    // GET /api/samples/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SampleListDto>>> GetById(Guid id)
    {
        SampleListDto? sample = await _service.GetByIdAsync(id);
        if (sample == null)
            return NotFound(ApiResponse<SampleListDto>.Fail("Sample not found."));
        return Ok(ApiResponse<SampleListDto>.Success(sample));
    }

    // GET /api/samples/enrollment/{enrollmentId}
    [HttpGet("enrollment/{enrollmentId:guid}")]
    public async Task<ActionResult<ApiResponse<IList<SampleListDto>>>> GetByEnrollment(Guid enrollmentId)
    {
        IList<SampleListDto> samples = await _service.GetByEnrollmentAsync(enrollmentId);
        return Ok(ApiResponse<IList<SampleListDto>>.Success(samples, $"{samples.Count} sample(s) found."));
    }

    // POST /api/samples — LAB_TECHNICIAN, ADMIN, SYSTEM_ADMIN
    [HttpPost]
    [Authorize(Roles = "LAB_TECHNICIAN,ADMIN,SYSTEM_ADMIN")]
    public async Task<ActionResult<ApiResponse<SampleListDto>>> Create([FromBody] SampleCreateDto dto)
    {
        SampleListDto created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.SampleId },
            ApiResponse<SampleListDto>.Success(created, "Sample created successfully."));
    }

    // PUT /api/samples/{id} — LAB_TECHNICIAN, ADMIN, SYSTEM_ADMIN, RESEARCH_SCIENTIST
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "LAB_TECHNICIAN,ADMIN,SYSTEM_ADMIN,RESEARCH_SCIENTIST")]
    public async Task<ActionResult<ApiResponse<SampleListDto>>> Update(Guid id, [FromBody] SampleUpdateDto dto)
    {
        SampleListDto? updated = await _service.UpdateAsync(id, dto);
        if (updated == null)
            return NotFound(ApiResponse<SampleListDto>.Fail("Sample not found."));
        return Ok(ApiResponse<SampleListDto>.Success(updated, "Sample updated successfully."));
    }

    // PUT /api/samples/{id}/status — LAB_TECHNICIAN, RESEARCH_SCIENTIST, ADMIN, SYSTEM_ADMIN
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "LAB_TECHNICIAN,RESEARCH_SCIENTIST,ADMIN,SYSTEM_ADMIN")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateStatus(Guid id, [FromBody] string status)
    {
        bool ok = await _service.UpdateStatusAsync(id, status);
        if (!ok) return NotFound(ApiResponse<string>.Fail("Sample not found."));
        return Ok(ApiResponse<string>.Success(status, "Sample status updated."));
    }

    // DELETE /api/samples/{id} — ADMIN, SYSTEM_ADMIN
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN,SYSTEM_ADMIN")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
    {
        bool ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<string>.Fail("Sample not found."));
        return Ok(ApiResponse<string>.Success("deleted", "Sample deleted successfully."));
    }
}
