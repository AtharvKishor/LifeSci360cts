using Microsoft.EntityFrameworkCore;
using ProtocolService.Data;
using ProtocolService.Data.Entities;
using Shared.CL.DTOs;

namespace ProtocolService.Services;

public class ProtocolServiceImpl : IProtocolService
{
    private readonly ProtocolDbContext db;

    public ProtocolServiceImpl(ProtocolDbContext db)
    {
        this.db = db;
    }

    public async Task<ProtocolResponseDto> CreateAsync(CreateProtocolDto dto, Guid createdByUserId)
    {
        var title = dto.Title.Trim();
        var phase = dto.Phase.Trim();

        var exists = await db.Protocols
            .AnyAsync(p => p.Title == title && p.Phase == phase);

        if (exists)
        {
            throw new InvalidOperationException(
                "A protocol with the same title and phase already exists.");
        }

        var protocol = new Protocol
        {
            Title = title,
            Phase = phase,
            Status = dto.Status.ToUpperInvariant(),
            Description = dto.Description?.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            CreatedByUserId = createdByUserId
        };

        db.Protocols.Add(protocol);
        await db.SaveChangesAsync();

        return MapToResponse(protocol);
    }

    public async Task<ProtocolResponseDto?> GetByIdAsync(Guid id)
    {
        var protocol = await db.Protocols
            .Include(p => p.ProtocolSites)
            .FirstOrDefaultAsync(p => p.ProtocolId == id && p.Status != ProtocolStatus.Deleted);

        if (protocol == null)
        {
            return null;
        }

        return MapToResponse(protocol);
    }

    public async Task<List<ProtocolResponseDto>> GetAllAsync(string? status, string? phase, string? title)
    {
        var query = db.Protocols
            .Include(p => p.ProtocolSites)
            .Where(p => p.Status != ProtocolStatus.Deleted);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status.ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(phase))
        {
            query = query.Where(p => EF.Functions.Like(p.Phase, "%" + phase + "%"));
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(p => EF.Functions.Like(p.Title, "%" + title + "%"));
        }

        var list = await query.OrderByDescending(p => p.ProtocolId).ToListAsync();

        var result = new List<ProtocolResponseDto>();
        foreach (var p in list)
        {
            result.Add(MapToResponse(p));
        }
        return result;
    }

    public async Task<ProtocolResponseDto> UpdateAsync(Guid id, UpdateProtocolDto dto)
    {
        var protocol = await db.Protocols
            .Include(p => p.ProtocolSites)
            .FirstOrDefaultAsync(p => p.ProtocolId == id && p.Status != ProtocolStatus.Deleted);

        if (protocol == null)
        {
            throw new KeyNotFoundException("Protocol not found.");
        }

        var title = dto.Title.Trim();
        var phase = dto.Phase.Trim();

        var duplicate = await db.Protocols
            .AnyAsync(p => p.Title == title && p.Phase == phase && p.ProtocolId != id);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "A protocol with the same title and phase already exists.");
        }

        protocol.Title = title;
        protocol.Phase = phase;
        protocol.Description = dto.Description?.Trim();
        protocol.StartDate = dto.StartDate;
        protocol.EndDate = dto.EndDate;

        await db.SaveChangesAsync();
        return MapToResponse(protocol);
    }

    public async Task<ProtocolResponseDto> UpdateStatusAsync(Guid id, UpdateProtocolStatusDto dto)
    {
        var protocol = await db.Protocols
            .Include(p => p.ProtocolSites)
            .FirstOrDefaultAsync(p => p.ProtocolId == id && p.Status != ProtocolStatus.Deleted);

        if (protocol == null)
        {
            throw new KeyNotFoundException("Protocol not found.");
        }

        var newStatus = dto.Status.ToUpperInvariant();

        if (!IsValidTransition(protocol.Status, newStatus))
        {
            throw new InvalidOperationException(
                "Cannot transition from '" + protocol.Status + "' to '" + newStatus + "'.");
        }

        protocol.Status = newStatus;

        if (newStatus == ProtocolStatus.Completed)
        {
            foreach (var ps in protocol.ProtocolSites)
            {
                if (ps.Status != AssignmentStatus.Closed)
                {
                    ps.Status = AssignmentStatus.Closed;
                }
            }
        }

        await db.SaveChangesAsync();
        return MapToResponse(protocol);
    }

    public async Task<ProtocolResponseDto> SoftDeleteAsync(Guid id)
    {
        var protocol = await db.Protocols
            .Include(p => p.ProtocolSites)
            .FirstOrDefaultAsync(p => p.ProtocolId == id && p.Status != ProtocolStatus.Deleted);

        if (protocol == null)
        {
            throw new KeyNotFoundException("Protocol not found.");
        }

        protocol.Status = ProtocolStatus.Deleted;

        foreach (var ps in protocol.ProtocolSites)
        {
            if (ps.Status != AssignmentStatus.Closed)
            {
                ps.Status = AssignmentStatus.Closed;
            }
        }

        await db.SaveChangesAsync();
        return MapToResponse(protocol);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static bool IsValidTransition(string current, string next)
    {
        if (current == ProtocolStatus.Upcoming)
        {
            return next == ProtocolStatus.Ongoing || next == ProtocolStatus.Completed;
        }
        if (current == ProtocolStatus.Ongoing)
        {
            return next == ProtocolStatus.Completed;
        }
        if (current == ProtocolStatus.Completed)
        {
            return next == ProtocolStatus.Ongoing;
        }
        return false;
    }

    private static string? GetSuggestion(Protocol p)
    {
        if (p.StartDate == null || p.EndDate == null)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        if (p.Status == ProtocolStatus.Upcoming && p.StartDate.Value <= now)
        {
            return ProtocolStatus.Ongoing;
        }

        if (p.Status == ProtocolStatus.Ongoing && p.EndDate.Value <= now)
        {
            return ProtocolStatus.Completed;
        }

        return null;
    }

    private static ProtocolResponseDto MapToResponse(Protocol p)
    {
        int siteCount = 0;
        foreach (var ps in p.ProtocolSites)
        {
            if (ps.Status != AssignmentStatus.Closed)
            {
                siteCount++;
            }
        }

        return new ProtocolResponseDto
        {
            ProtocolId = p.ProtocolId,
            Title = p.Title,
            Phase = p.Phase,
            Status = p.Status,
            SuggestedStatus = GetSuggestion(p),
            Description = p.Description,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            CreatedByUserId = p.CreatedByUserId,
            SiteCount = siteCount
        };
    }
}