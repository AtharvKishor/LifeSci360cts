using Microsoft.EntityFrameworkCore;
using ProtocolService.Data;
using ProtocolService.Data.Entities;
using ProtocolService.Enums;

namespace ProtocolService.Repositories;

public class ProtocolRepository : IProtocolRepository
{
    private readonly ProtocolDbContext _db;

    public ProtocolRepository(ProtocolDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TitlePhaseExistsAsync(string title, string phase)
    {
        return await _db.Protocols
            .AnyAsync(p => p.Title == title && p.Phase == phase);
    }

    public async Task<bool> TitlePhaseExistsAsync(string title, string phase, Guid excludeId)
    {
        return await _db.Protocols
            .AnyAsync(p => p.Title == title && p.Phase == phase && p.ProtocolId != excludeId);
    }

    public async Task<Protocol?> GetByIdAsync(Guid id)
    {
        return await _db.Protocols
            .Include(p => p.ProtocolSites)
            .FirstOrDefaultAsync(p => p.ProtocolId == id && p.Status != ProtocolStatus.Deleted);
    }

    // Receives already-normalised values from the service layer â€” no transformation here
    public async Task<List<Protocol>> GetAllAsync(string? status, string? phase, string? title)
    {
        var query = _db.Protocols
            .Include(p => p.ProtocolSites)
            .Where(p => p.Status != ProtocolStatus.Deleted);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(phase))
            query = query.Where(p => EF.Functions.Like(p.Phase, "%" + phase + "%"));

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(p => EF.Functions.Like(p.Title, "%" + title + "%"));

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task AddAsync(Protocol protocol)
    {
        await _db.Protocols.AddAsync(protocol);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}

