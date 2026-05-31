using Microsoft.EntityFrameworkCore;
using ProtocolService.Data;
using ProtocolService.Data.Entities;
using Shared.CL.DTOs;

namespace ProtocolService.Services;

public class ProtocolSiteServiceImpl : IProtocolSiteService
{
    private readonly ProtocolDbContext db;

    public ProtocolSiteServiceImpl(ProtocolDbContext db)
    {
        this.db = db;
    }

    public async Task<ProtocolSiteResponseDto> AssignAsync(Guid protocolId, AssignSiteDto dto)
    {
        var protocol = await db.Protocols
            .FirstOrDefaultAsync(p => p.ProtocolId == protocolId && p.Status != ProtocolStatus.Deleted);

        if (protocol == null)
        {
            throw new KeyNotFoundException("Protocol not found.");
        }

        if (protocol.Status == ProtocolStatus.Completed)
        {
            throw new InvalidOperationException(
                "Cannot assign sites to a protocol with status '" + protocol.Status + "'.");
        }

        var site = await db.Sites
            .FirstOrDefaultAsync(s => s.SiteId == dto.SiteId && s.IsActive);

        if (site == null)
        {
            throw new KeyNotFoundException("Site not found or has been deleted.");
        }

        var investigator = await db.Users
            .FirstOrDefaultAsync(u => u.UserId == dto.InvestigatorUserId && u.IsActive);

        if (investigator == null)
        {
            throw new KeyNotFoundException("Investigator not found or is inactive.");
        }

        var existing = await db.ProtocolSites
            .Include(x => x.Protocol)
            .Include(x => x.Site)
            .FirstOrDefaultAsync(x => x.ProtocolId == protocolId && x.SiteId == dto.SiteId);

        if (existing != null)
        {
            if (existing.Status != AssignmentStatus.Closed)
            {
                throw new InvalidOperationException(
                    "This site is already assigned to the protocol.");
            }

            existing.Status = AssignmentStatus.Active;
            existing.InvestigatorUserId = dto.InvestigatorUserId;
            await db.SaveChangesAsync();

            return MapToResponse(existing, investigator.Name);
        }

        var assignment = new ProtocolSite
        {
            ProtocolId = protocolId,
            SiteId = dto.SiteId,
            InvestigatorUserId = dto.InvestigatorUserId,
            Status = AssignmentStatus.Active,
            Protocol = protocol,
            Site = site
        };

        db.ProtocolSites.Add(assignment);
        await db.SaveChangesAsync();

        return MapToResponse(assignment, investigator.Name);
    }

    public async Task<List<ProtocolSiteResponseDto>> GetByProtocolAsync(Guid protocolId)
    {
        var assignments = await db.ProtocolSites
            .Include(ps => ps.Protocol)
            .Include(ps => ps.Site)
            .Where(ps => ps.ProtocolId == protocolId && ps.Status != AssignmentStatus.Closed)
            .ToListAsync();

        return await BuildResponseList(assignments);
    }

    public async Task<List<ProtocolSiteResponseDto>> GetBySiteAsync(Guid siteId)
    {
        var assignments = await db.ProtocolSites
            .Include(ps => ps.Protocol)
            .Include(ps => ps.Site)
            .Where(ps => ps.SiteId == siteId && ps.Status != AssignmentStatus.Closed)
            .ToListAsync();

        return await BuildResponseList(assignments);
    }

    public async Task<ProtocolSiteResponseDto> UpdateStatusAsync(
        Guid protocolId, Guid assignmentId, UpdateProtocolSiteStatusDto dto)
    {
        var assignment = await db.ProtocolSites
            .Include(x => x.Protocol)
            .Include(x => x.Site)
            .FirstOrDefaultAsync(x => x.ProtocolSiteId == assignmentId && x.ProtocolId == protocolId);

        if (assignment == null)
        {
            throw new KeyNotFoundException("Assignment not found.");
        }

        var newStatus = dto.Status.ToUpperInvariant();

        if (!IsValidAssignmentTransition(assignment.Status, newStatus))
        {
            throw new InvalidOperationException(
                "Cannot transition from '" + assignment.Status + "' to '" + newStatus + "'.");
        }

        assignment.Status = newStatus;
        await db.SaveChangesAsync();

        var name = await GetInvestigatorName(assignment.InvestigatorUserId);
        return MapToResponse(assignment, name);
    }

    public async Task<ProtocolSiteResponseDto> RemoveAsync(Guid protocolId, Guid assignmentId)
    {
        var assignment = await db.ProtocolSites
            .Include(x => x.Protocol)
            .Include(x => x.Site)
            .FirstOrDefaultAsync(x => x.ProtocolSiteId == assignmentId && x.ProtocolId == protocolId);

        if (assignment == null)
        {
            throw new KeyNotFoundException("Assignment not found.");
        }

        if (assignment.Status == AssignmentStatus.Closed)
        {
            throw new InvalidOperationException("This assignment is already closed.");
        }

        assignment.Status = AssignmentStatus.Closed;
        await db.SaveChangesAsync();

        var name = await GetInvestigatorName(assignment.InvestigatorUserId);
        return MapToResponse(assignment, name);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static bool IsValidAssignmentTransition(string current, string next)
    {
        if (current == AssignmentStatus.Active)
        {
            return next == AssignmentStatus.Inactive || next == AssignmentStatus.Closed;
        }
        if (current == AssignmentStatus.Inactive)
        {
            return next == AssignmentStatus.Active || next == AssignmentStatus.Closed;
        }
        return false;
    }

    private async Task<string> GetInvestigatorName(Guid userId)
    {
        var name = await db.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync();

        if (name == null)
        {
            return userId.ToString().Substring(0, 8) + "...";
        }
        return name;
    }

    private async Task<List<ProtocolSiteResponseDto>> BuildResponseList(List<ProtocolSite> assignments)
    {
        if (assignments.Count == 0)
        {
            return new List<ProtocolSiteResponseDto>();
        }

        var userIds = new List<Guid>();
        foreach (var ps in assignments)
        {
            if (!userIds.Contains(ps.InvestigatorUserId))
            {
                userIds.Add(ps.InvestigatorUserId);
            }
        }

        var users = await db.Users
            .Where(u => userIds.Contains(u.UserId))
            .ToListAsync();

        var nameMap = new Dictionary<Guid, string>();
        foreach (var u in users)
        {
            nameMap[u.UserId] = u.Name;
        }

        var result = new List<ProtocolSiteResponseDto>();
        foreach (var ps in assignments)
        {
            string name;
            if (nameMap.ContainsKey(ps.InvestigatorUserId))
            {
                name = nameMap[ps.InvestigatorUserId];
            }
            else
            {
                name = ps.InvestigatorUserId.ToString().Substring(0, 8) + "...";
            }
            result.Add(MapToResponse(ps, name));
        }
        return result;
    }

    private static ProtocolSiteResponseDto MapToResponse(ProtocolSite ps, string investigatorName)
    {
        return new ProtocolSiteResponseDto
        {
            ProtocolSiteId = ps.ProtocolSiteId,
            ProtocolId = ps.ProtocolId,
            ProtocolTitle = ps.Protocol.Title,
            SiteId = ps.SiteId,
            SiteName = ps.Site.Name,
            SiteLocation = ps.Site.Location,
            InvestigatorUserId = ps.InvestigatorUserId,
            InvestigatorName = investigatorName,
            Status = ps.Status
        };
    }
}