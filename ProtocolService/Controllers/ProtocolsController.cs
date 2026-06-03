using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.CL;
using Shared.CL.DTOs;
using ProtocolService.Services;

namespace ProtocolService.Controllers;

[ApiController]
[Route("api/protocols")]
public class ProtocolsController : ControllerBase
{
    private readonly IProtocolService svc;

    // TODO: Replace with JWT claims once [Authorize] is wired up
    // User.FindFirst(ClaimTypes.NameIdentifier) → Guid
    private static readonly Guid TestUserId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");

    public ProtocolsController(IProtocolService svc)
    {
        this.svc = svc;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProtocolResponseDto>>> Create([FromBody] CreateProtocolDto dto)
    {
        try
        {
            var created = await svc.CreateAsync(dto, TestUserId);
            return StatusCode(201, ApiResponse<ProtocolResponseDto>.Ok(created, "Protocol created successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ProtocolResponseDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<ProtocolResponseDto>.Fail(ex.Message));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProtocolResponseDto>>>> GetAll(
        [FromQuery] string? title,
        [FromQuery] string? phase,
        [FromQuery] string? status)
    {
        var list = await svc.GetAllAsync(status, phase, title);
        return Ok(ApiResponse<List<ProtocolResponseDto>>.Ok(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProtocolResponseDto>>> GetById(Guid id)
    {
        var result = await svc.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<ProtocolResponseDto>.Fail("Protocol not found."));

        return Ok(ApiResponse<ProtocolResponseDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProtocolResponseDto>>> Update(
        Guid id, [FromBody] UpdateProtocolDto dto)
    {
        try
        {
            await svc.UpdateAsync(id, dto);
            return Ok(ApiResponse<ProtocolResponseDto>.OkOnly("Protocol updated successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ProtocolResponseDto>.Fail(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<ProtocolResponseDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<ProtocolResponseDto>.Fail(ex.Message));
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<ProtocolResponseDto>>> UpdateStatus(
        Guid id, [FromBody] UpdateProtocolStatusDto dto)
    {
        try
        {
            await svc.UpdateStatusAsync(id, dto);
            return Ok(ApiResponse<ProtocolResponseDto>.OkOnly("Protocol status updated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<ProtocolResponseDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<ProtocolResponseDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProtocolResponseDto>>> Delete(Guid id)
    {
        try
        {
            await svc.SoftDeleteAsync(id);
            return Ok(ApiResponse<ProtocolResponseDto>.OkOnly("Protocol deleted successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<ProtocolResponseDto>.Fail(ex.Message));
        }
    }
}