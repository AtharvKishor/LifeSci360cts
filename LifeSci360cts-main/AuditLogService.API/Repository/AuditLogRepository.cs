using AuditLogService.API.Data;
using AuditLogService.API.Models;
using Microsoft.EntityFrameworkCore;
using Shared.CL.DTOs;

namespace AuditLogService.API.Repository
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AuditLogDbContext _context;

        public AuditLogRepository(AuditLogDbContext context)
        {
            _context = context;
        }

        // Called by ActivityLogFilter and GlobalExceptionFilter
        // from ProtocolService and SiteService
        public async Task<int> CreateLogAsync(AuditLogCreateDto dto)
        {
            try
            {
                AuditLog log = new AuditLog
                {
                    UserId = dto.UserId,
                    UserEmail = dto.UserEmail,
                    Action = dto.Action,
                    ServiceName = dto.ServiceName,
                    IsError = dto.IsError,
                    ErrorMessage = dto.ErrorMessage,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.AuditLogs.AddAsync(log);
                await _context.SaveChangesAsync();
                return log.Id;
            }
            catch (Exception ex)
            {
                // Log to console but don't crash the service
                Console.WriteLine($"[AuditLog ERROR] Failed to save log: {ex.Message}");
                return -1;
            }
        }

        // Get all logs — newest first
        public async Task<IList<AuditLogListDto>> GetAllLogsAsync()
        {
            return await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => ToDto(l))
                .ToListAsync();
        }

        // Get logs for one service e.g. "ProtocolService"
        public async Task<IList<AuditLogListDto>> GetLogsByServiceAsync(string serviceName)
        {
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(l => l.ServiceName == serviceName)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => ToDto(l))
                .ToListAsync();
        }

        // Get only error logs
        public async Task<IList<AuditLogListDto>> GetErrorLogsAsync()
        {
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(l => l.IsError == true)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => ToDto(l))
                .ToListAsync();
        }

        // Get logs for one specific user
        public async Task<IList<AuditLogListDto>> GetLogsByUserAsync(int userId)
        {
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => ToDto(l))
                .ToListAsync();
        }

        private static AuditLogListDto ToDto(AuditLog l) => new AuditLogListDto
        {
            UserId = l.UserId,
            UserEmail = l.UserEmail,
            Action = l.Action,
            ServiceName = l.ServiceName,
            IsError = l.IsError,
            ErrorMessage = l.ErrorMessage,
            CreatedAt = l.CreatedAt
        };
    }
}