using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.CL;
using Shared.CL.DTOs;
using ProtocolService.DTOs;
using ProtocolService.Services;

namespace ProtocolService.Controllers;

// ── Investigators lookup (standalone route) ──────────────────────────────────
[ApiController]
[Route("api/investigators")]
public class InvestigatorsController : ControllerBase
{
    private readonly IProtocolSiteService svc;

    public InvestigatorsController(IProtocolSiteService svc) => this.svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InvestigatorDto>>>> GetAll()
    {
        var list = await svc.GetInvestigatorsAsync();
        return Ok(ApiResponse<List<InvestigatorDto>>.Ok(list));
    }
}

// ── Protocol-Site assignments ────────────────────────────────────────────────
[ApiController]
[Route("api/protocols/{protocolId:guid}/sites")]
public class ProtocolSitesController : ControllerBase
{
    private readonly IProtocolSiteService svc;

    public ProtocolSitesController(IProtocolSiteService svc)
    {
        this.svc = svc;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProtocolSiteResponseDto>>> Assign(
        Guid protocolId, [FromBody] AssignSiteDto dto)
    {
        try
        {
            var assigned = await svc.AssignAsync(protocolId, dto);
            return StatusCode(201, ApiResponse<ProtocolSiteResponseDto>.Ok(assigned, "Site assigned successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<ProtocolSiteResponseDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<ProtocolSiteResponseDto>.Fail(ex.Message));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProtocolSiteResponseDto>>>> GetByProtocol(Guid protocolId)
    {
        var list = await svc.GetByProtocolAsync(protocolId);
        return Ok(ApiResponse<List<ProtocolSiteResponseDto>>.Ok(list));
    }

    [HttpPatch("{assignmentId:guid}/status")]
    public async Task<ActionResult<ApiResponse<ProtocolSiteResponseDto>>> UpdateStatus(
        Guid protocolId, Guid assignmentId,
        [FromBody] UpdateProtocolSiteStatusDto dto)
    {
        try
        {
            await svc.UpdateStatusAsync(protocolId, assignmentId, dto);
            return Ok(ApiResponse<ProtocolSiteResponseDto>.OkOnly("Assignment status updated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<ProtocolSiteResponseDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<ProtocolSiteResponseDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("{assignmentId:guid}")]
    public async Task<ActionResult<ApiResponse<ProtocolSiteResponseDto>>> Remove(
        Guid protocolId, Guid assignmentId)
    {
        try
        {
            await svc.RemoveAsync(protocolId, assignmentId);
            return Ok(ApiResponse<ProtocolSiteResponseDto>.OkOnly("Assignment closed."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<ProtocolSiteResponseDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<ProtocolSiteResponseDto>.Fail(ex.Message));
        }
    }
}