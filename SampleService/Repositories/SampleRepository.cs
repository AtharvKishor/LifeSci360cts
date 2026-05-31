using Microsoft.EntityFrameworkCore;
using SampleService.Data;
using SampleService.Data.Entities;
using Shared.CL.DTOs;

namespace SampleService.Repositories;

public class SampleRepository : ISampleRepository
{
    private readonly ServicesDbContext _db;

    public SampleRepository(ServicesDbContext db) => _db = db;

    public async Task<IList<SampleListDto>> GetAllAsync()
    {
        return await _db.Samples
            .AsNoTracking()
            .Include(s => s.CollectedByUser)
            .OrderByDescending(s => s.CollectedDate)
            .Select(s => ToDto(s))
            .ToListAsync();//execute query n return as list
    }

    public async Task<SampleListDto?> GetByIdAsync(Guid id)
    {
        Sample? sample = await _db.Samples
            .AsNoTracking()
            .Include(s => s.CollectedByUser)
            .FirstOrDefaultAsync(s => s.SampleId == id);

        return sample == null ? null : ToDto(sample);
    }

    public async Task<IList<SampleListDto>> GetByEnrollmentAsync(Guid enrollmentId)
    {
        return await _db.Samples
            .AsNoTracking()
            .Include(s => s.CollectedByUser)
            .Where(s => s.EnrollmentId == enrollmentId)
            .OrderByDescending(s => s.CollectedDate)
            .Select(s => ToDto(s))
            .ToListAsync();
    }

    public async Task<SampleListDto> CreateAsync(SampleCreateDto dto)
    {
        Sample sample = new()
        {
            EnrollmentId = dto.EnrollmentId,
            CollectedByUserId = dto.CollectedByUserId,
            SampleType = dto.SampleType,
            CollectedDate = dto.CollectedDate,
            Status = "COLLECTED"
        };

        _db.Samples.Add(sample);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(sample.SampleId))!;
    }

    public async Task<SampleListDto?> UpdateAsync(Guid id, SampleUpdateDto dto)
    {
        Sample? sample = await _db.Samples.FindAsync(id);
        if (sample == null) return null;

        if (dto.SampleType != null) sample.SampleType = dto.SampleType;
        if (dto.Status != null) sample.Status = dto.Status;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Sample? sample = await _db.Samples.FindAsync(id);
        if (sample == null) return false;

        _db.Samples.Remove(sample);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        Sample? sample = await _db.Samples.FindAsync(id);
        if (sample == null) return false;

        sample.Status = status;
        await _db.SaveChangesAsync();
        return true;
    }

    private static SampleListDto ToDto(Sample s) => new() //converts db entity to dto
    {
        SampleId = s.SampleId,
        EnrollmentId = s.EnrollmentId,
        CollectedByUserId = s.CollectedByUserId,
        CollectedByUserName = s.CollectedByUser?.Name ?? string.Empty,
        SampleType = s.SampleType,
        CollectedDate = s.CollectedDate,
        Status = s.Status
    };
}
