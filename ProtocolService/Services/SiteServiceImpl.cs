using Microsoft.EntityFrameworkCore;
using ProtocolService.Data;
using ProtocolService.Data.Entities;
using Shared.CL.DTOs;

namespace ProtocolService.Services;

public class SiteServiceImpl : ISiteService
{
    private readonly ProtocolDbContext db;

    public SiteServiceImpl(ProtocolDbContext db)
    {
        this.db = db;
    }

    public async Task<SiteResponseDto> CreateAsync(CreateSiteDto dto)
    {
        var name = dto.Name.Trim();
        var location = dto.Location?.Trim();

        var exists = await db.Sites
            .AnyAsync(s => s.IsActive
                        && s.Name == name
                        && (s.Location ?? "") == (location ?? ""));

        if (exists)
        {
            throw new InvalidOperationException(
                "A site with the same name and location already exists.");
        }

        var site = new Site
        {
            Name = name,
            Location = location,
            IsActive = true
        };

        db.Sites.Add(site);
        await db.SaveChangesAsync();
        return MapToResponse(site);
    }

    public async Task<SiteResponseDto?> GetByIdAsync(Guid id)
    {
        var site = await db.Sites
            .Include(s => s.ProtocolSites)
            .FirstOrDefaultAsync(s => s.SiteId == id && s.IsActive);

        if (site == null)
        {
            return null;
        }
        return MapToResponse(site);
    }

    public async Task<List<SiteResponseDto>> GetAllAsync(string? name, string? location)
    {
        var query = db.Sites
            .Include(s => s.ProtocolSites)
            .Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(s => EF.Functions.Like(s.Name, "%" + name + "%"));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(s => EF.Functions.Like(s.Location!, "%" + location + "%"));
        }

        var sites = await query.OrderBy(s => s.Name).ToListAsync();

        var result = new List<SiteResponseDto>();
        foreach (var s in sites)
        {
            result.Add(MapToResponse(s));
        }
        return result;
    }

    public async Task<SiteResponseDto> UpdateAsync(Guid id, UpdateSiteDto dto)
    {
        var site = await db.Sites
            .Include(s => s.ProtocolSites)
            .FirstOrDefaultAsync(s => s.SiteId == id && s.IsActive);

        if (site == null)
        {
            throw new KeyNotFoundException("Site not found.");
        }

        var name = dto.Name.Trim();
        var location = dto.Location?.Trim();

        var duplicate = await db.Sites
            .AnyAsync(s => s.IsActive
                        && s.Name == name
                        && (s.Location ?? "") == (location ?? "")
                        && s.SiteId != id);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "A site with the same name and location already exists.");
        }

        site.Name = name;
        site.Location = location;
        await db.SaveChangesAsync();

        return MapToResponse(site);
    }

    public async Task<SiteResponseDto> SoftDeleteAsync(Guid id)
    {
        var site = await db.Sites
            .Include(s => s.ProtocolSites)
            .FirstOrDefaultAsync(s => s.SiteId == id && s.IsActive);

        if (site == null)
        {
            throw new KeyNotFoundException("Site not found.");
        }

        site.IsActive = false;

        foreach (var ps in site.ProtocolSites)
        {
            if (ps.Status != AssignmentStatus.Closed)
            {
                ps.Status = AssignmentStatus.Closed;
            }
        }

        await db.SaveChangesAsync();
        return MapToResponse(site);
    }

    private static SiteResponseDto MapToResponse(Site s)
    {
        int count = 0;
        foreach (var ps in s.ProtocolSites)
        {
            if (ps.Status != AssignmentStatus.Closed)
            {
                count++;
            }
        }

        return new SiteResponseDto
        {
            SiteId = s.SiteId,
            Name = s.Name,
            Location = s.Location,
            ProtocolCount = count
        };
    }
}