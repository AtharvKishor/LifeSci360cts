using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.CL;
using Shared.CL.DTOs;
using ProtocolService.Services;

namespace ProtocolService.Controllers;

[ApiController]
[Route("api/sites")]
public class SitesController : ControllerBase
{
    private readonly ISiteService svc;
    private readonly IProtocolSiteService protocolSiteSvc;

    public SitesController(ISiteService svc, IProtocolSiteService protocolSiteSvc)
    {
        this.svc = svc;
        this.protocolSiteSvc = protocolSiteSvc;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SiteResponseDto>>> Create([FromBody] CreateSiteDto dto)
    {
        try
        {
            var created = await svc.CreateAsync(dto);
            return StatusCode(201, ApiResponse<SiteResponseDto>.Ok(created, "Site registered successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SiteResponseDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SiteResponseDto>.Fail(ex.Message));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SiteResponseDto>>>> GetAll(
        [FromQuery] string? name,
        [FromQuery] string? location)
    {
        var list = await svc.GetAllAsync(name, location);
        return Ok(ApiResponse<List<SiteResponseDto>>.Ok(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SiteResponseDto>>> GetById(Guid id)
    {
        var result = await svc.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<SiteResponseDto>.Fail("Site not found."));

        return Ok(ApiResponse<SiteResponseDto>.Ok(result));
    }

    [HttpGet("{siteId:guid}/protocols")]
    public async Task<ActionResult<ApiResponse<List<ProtocolSiteResponseDto>>>> GetProtocols(Guid siteId)
    {
        var list = await protocolSiteSvc.GetBySiteAsync(siteId);
        return Ok(ApiResponse<List<ProtocolSiteResponseDto>>.Ok(list));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SiteResponseDto>>> Update(Guid id, [FromBody] UpdateSiteDto dto)
    {
        try
        {
            await svc.UpdateAsync(id, dto);
            return Ok(ApiResponse<SiteResponseDto>.OkOnly("Site updated successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SiteResponseDto>.Fail(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SiteResponseDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SiteResponseDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SiteResponseDto>>> Delete(Guid id)
    {
        try
        {
            await svc.SoftDeleteAsync(id);
            return Ok(ApiResponse<SiteResponseDto>.OkOnly("Site deleted successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SiteResponseDto>.Fail(ex.Message));
        }
    }
}