using Shared.CL.DTOs;

namespace AuditLogService.API.Repository
{
    public interface IAuditLogRepository
    {
        Task<int> CreateLogAsync(AuditLogCreateDto dto);
        Task<IList<AuditLogListDto>> GetAllLogsAsync();
        Task<IList<AuditLogListDto>> GetLogsByServiceAsync(string serviceName);
        Task<IList<AuditLogListDto>> GetErrorLogsAsync();
        Task<IList<AuditLogListDto>> GetLogsByUserAsync(int userId);
    }
}