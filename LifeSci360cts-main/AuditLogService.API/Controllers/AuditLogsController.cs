using AuditLogService.API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.CL;
using Shared.CL.DTOs;

[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _repo;

    public AuditLogsController(IAuditLogRepository repo)
    {
        _repo = repo;
    }

    // This endpoint is called by ActivityLogFilter and GlobalExceptionFilter
    // from ProtocolService and SiteService (fire and forget)
    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Create(
     [FromBody] AuditLogCreateDto dto)
    {
        try
        {
            int id = await _repo.CreateLogAsync(dto);
            return id > 0
                ? Ok(ApiResponse<int>.Success(id, "Log recorded."))
                : Ok(ApiResponse<int>.Fail("Log could not be saved but request is acknowledged."));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditLog Controller ERROR] {ex.Message}");
            return Ok(ApiResponse<int>.Fail("AuditLog service error. Main request not affected."));
        }
    }

    // Get all logs — for RegulatoryOfficer dashboard
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IList<AuditLogListDto>>>> GetAll()
    {
        IList<AuditLogListDto> logs = await _repo.GetAllLogsAsync();
        return Ok(ApiResponse<IList<AuditLogListDto>>
            .Success(logs, $"{logs.Count} log(s) found."));
    }

    // Get logs for a specific service e.g. /api/auditlogs/service/ProtocolService
    [HttpGet("service/{serviceName}")]
    public async Task<ActionResult<ApiResponse<IList<AuditLogListDto>>>> GetByService(
        string serviceName)
    {
        IList<AuditLogListDto> logs = await _repo.GetLogsByServiceAsync(serviceName);
        return Ok(ApiResponse<IList<AuditLogListDto>>
            .Success(logs, $"{logs.Count} log(s) for {serviceName}."));
    }

    // Get only error logs — for monitoring
    [HttpGet("errors")]
    public async Task<ActionResult<ApiResponse<IList<AuditLogListDto>>>> GetErrors()
    {
        IList<AuditLogListDto> logs = await _repo.GetErrorLogsAsync();
        return Ok(ApiResponse<IList<AuditLogListDto>>
            .Success(logs, $"{logs.Count} error(s) found."));
    }

    // Get logs for a specific user
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<ApiResponse<IList<AuditLogListDto>>>> GetByUser(
        int userId)
    {
        IList<AuditLogListDto> logs = await _repo.GetLogsByUserAsync(userId);
        return Ok(ApiResponse<IList<AuditLogListDto>>
            .Success(logs, $"{logs.Count} log(s) for user {userId}."));
    }
}