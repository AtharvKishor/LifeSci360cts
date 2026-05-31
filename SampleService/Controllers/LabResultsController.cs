using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleService.Repositories;
using Shared.CL;
using Shared.CL.DTOs;

namespace SampleService.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class LabResultsController : ControllerBase
{
    private readonly ILabResultRepository _repo;

    public LabResultsController(ILabResultRepository repo) => _repo = repo;

    // GET /api/labresults/sample/{sampleId} — all authenticated users
    [HttpGet("sample/{sampleId:guid}")]
    public async Task<ActionResult<ApiResponse<IList<LabResultListDto>>>> GetBySample(Guid sampleId)
    {
        IList<LabResultListDto> results = await _repo.GetBySampleAsync(sampleId);
        return Ok(ApiResponse<IList<LabResultListDto>>.Success(results, $"{results.Count} result(s) found."));
    }

    // GET /api/labresults/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LabResultListDto>>> GetById(Guid id)
    {
        LabResultListDto? result = await _repo.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<LabResultListDto>.Fail("Lab result not found."));
        return Ok(ApiResponse<LabResultListDto>.Success(result));
    }

    // POST /api/labresults — LAB_TECHNICIAN, ADMIN
    [HttpPost]
    [Authorize(Roles = "LAB_TECHNICIAN,ADMIN")]
    public async Task<ActionResult<ApiResponse<LabResultListDto>>> Create([FromBody] LabResultCreateDto dto)
    {
        LabResultListDto created = await _repo.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.ResultId },
            ApiResponse<LabResultListDto>.Success(created, "Lab result recorded successfully."));
    }

    // PUT /api/labresults/{id} — LAB_TECHNICIAN, ADMIN
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "LAB_TECHNICIAN,ADMIN")]
    public async Task<ActionResult<ApiResponse<LabResultListDto>>> Update(Guid id, [FromBody] LabResultUpdateDto dto)
    {
        LabResultListDto? updated = await _repo.UpdateAsync(id, dto);
        if (updated == null)
            return NotFound(ApiResponse<LabResultListDto>.Fail("Lab result not found."));
        return Ok(ApiResponse<LabResultListDto>.Success(updated, "Lab result updated successfully."));
    }

    // DELETE /api/labresults/{id} — ADMIN only
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
    {
        bool ok = await _repo.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<string>.Fail("Lab result not found."));
        return Ok(ApiResponse<string>.Success("deleted", "Lab result deleted successfully."));
    }
}
