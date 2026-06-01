//receives requests and send responses
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleService.Repositories;
using Shared.CL;
using Shared.CL.DTOs;

namespace SampleService.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController] //enables auto validation
public class SamplesController : ControllerBase
{
    private readonly ISampleRepository _repo;

    public SamplesController(ISampleRepository repo) => _repo = repo;

    // GET /api/samples — all authenticated users can view
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IList<SampleListDto>>>> GetAll()
    {
        IList<SampleListDto> samples = await _repo.GetAllAsync();
        return Ok(ApiResponse<IList<SampleListDto>>.Success(samples, $"{samples.Count} sample(s) found."));
    }

    // GET /api/samples/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SampleListDto>>> GetById(Guid id)
    {
        SampleListDto? sample = await _repo.GetByIdAsync(id);
        if (sample == null)
            return NotFound(ApiResponse<SampleListDto>.Fail("Sample not found."));
        return Ok(ApiResponse<SampleListDto>.Success(sample));
    }

    // GET /api/samples/enrollment/{enrollmentId}
    [HttpGet("enrollment/{enrollmentId:guid}")]
    public async Task<ActionResult<ApiResponse<IList<SampleListDto>>>> GetByEnrollment(Guid enrollmentId)
    {
        IList<SampleListDto> samples = await _repo.GetByEnrollmentAsync(enrollmentId);
        return Ok(ApiResponse<IList<SampleListDto>>.Success(samples, $"{samples.Count} sample(s) found."));
    }

    // POST /api/samples — LAB_TECHNICIAN, RESEARCH_SCIENTIST, ADMIN
    [HttpPost]
    [Authorize(Roles = "LAB_TECHNICIAN,RESEARCH_SCIENTIST,ADMIN")]
    public async Task<ActionResult<ApiResponse<SampleListDto>>> Create([FromBody] SampleCreateDto dto)
    {
        SampleListDto created = await _repo.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.SampleId },
            ApiResponse<SampleListDto>.Success(created, "Sample created successfully."));
    }

    // PUT /api/samples/{id} — LAB_TECHNICIAN, RESEARCH_SCIENTIST, ADMIN
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "LAB_TECHNICIAN,RESEARCH_SCIENTIST,ADMIN")]
    public async Task<ActionResult<ApiResponse<SampleListDto>>> Update(Guid id, [FromBody] SampleUpdateDto dto)
    {
        SampleListDto? updated = await _repo.UpdateAsync(id, dto);
        if (updated == null)
            return NotFound(ApiResponse<SampleListDto>.Fail("Sample not found."));
        return Ok(ApiResponse<SampleListDto>.Success(updated, "Sample updated successfully."));
    }

    // PUT /api/samples/{id}/status — LAB_TECHNICIAN, ADMIN
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "LAB_TECHNICIAN,ADMIN")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateStatus(Guid id, [FromBody] string status)
    {
        bool ok = await _repo.UpdateStatusAsync(id, status);
        if (!ok) return NotFound(ApiResponse<string>.Fail("Sample not found."));
        return Ok(ApiResponse<string>.Success(status, "Sample status updated."));
    }

    // DELETE /api/samples/{id} — ADMIN only
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
    {
        bool ok = await _repo.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<string>.Fail("Sample not found."));
        return Ok(ApiResponse<string>.Success("deleted", "Sample deleted successfully."));
    }
}
