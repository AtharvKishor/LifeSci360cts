using Microsoft.EntityFrameworkCore;
using PatientService.Data;
using PatientService.Data.Entities;
using PatientService.Repository;
using Shared.DTOs;

namespace PatientService.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollRepo;
    private readonly ServicesDbContext _ctx;

    public EnrollmentService(
        IEnrollmentRepository enrollRepo,
        ServicesDbContext ctx)
    {
        _enrollRepo = enrollRepo;
        _ctx = ctx;
    }

    public async Task<IEnumerable<EnrollmentDto>> GetAllAsync()
    {
        var list = await _enrollRepo.GetAllAsync();
        return list.Select(MapToDto);
    }

    public async Task<EnrollmentDto?> GetByIdAsync(Guid id)
    {
        var e = await _enrollRepo.GetByIdAsync(id);
        return e is null ? null : MapToDto(e);
    }

    public async Task<(bool Success, string? Error, EnrollmentDto? Data)> EnrollAsync(
        Guid patientId, Guid protocolSiteId)
    {
        if (await _enrollRepo.HasActiveEnrollmentAsync(patientId))
            return (false, "Patient already has an active enrollment.", null);

        var ps = await _ctx.ProtocolSites
            .Include(x => x.Protocol)
            .Include(x => x.Site)
            .FirstOrDefaultAsync(x =>
                x.ProtocolSiteId == protocolSiteId &&
                x.Status == "ACTIVE");

        if (ps is null)
            return (false, "Invalid or inactive protocol-site combination.", null);

        var enrollment = new PatientEnrollment
        {
            PatientId = patientId,
            ProtocolSiteId = protocolSiteId,
            EnrollmentStatus = "ACTIVE",
            EnrolledAt = DateTime.UtcNow
        };

        var created = await _enrollRepo.CreateAsync(enrollment);
        var full = await _enrollRepo.GetByIdAsync(created.EnrollmentId);
        return (true, null, MapToDto(full!));
    }

    public async Task<(bool Success, string? Error)> WithdrawAsync(Guid id)
    {
        var e = await _enrollRepo.GetByIdAsync(id);
        if (e is null)
            return (false, "Enrollment not found.");
        if (e.EnrollmentStatus != "ACTIVE")
            return (false, "Only ACTIVE enrollments can be withdrawn.");

        // ✅ Cancel all scheduled/rescheduled visits
        var visits = await _ctx.Visits
            .Where(v => v.EnrollmentId == id &&
                   (v.VisitStatus == "SCHEDULED" || v.VisitStatus == "RESCHEDULED"))
            .ToListAsync();

        foreach (var v in visits)
            v.VisitStatus = "CANCELLED";

        if (visits.Any())
            _ctx.Visits.UpdateRange(visits);

        e.EnrollmentStatus = "WITHDRAWN";
        await _enrollRepo.UpdateAsync(e);
        await _ctx.SaveChangesAsync();
        return (true, null);
    }

    public async Task<IEnumerable<object>> GetProtocolsAsync()
    {
        return await _ctx.Protocols
            .Where(p => p.Status == "ACTIVE")
            .Select(p => new
            {
                p.ProtocolId,
                p.Title,
                p.Phase,
                p.Status,
                p.StartDate,
                p.EndDate
            } as object)
            .ToListAsync();
    }

    public async Task<IEnumerable<object>> GetSitesByProtocolAsync(Guid protocolId)
    {
        return await _ctx.ProtocolSites
            .Include(ps => ps.Site)
            .Where(ps => ps.ProtocolId == protocolId && ps.Status == "ACTIVE")
            .Select(ps => new
            {
                ps.ProtocolSiteId,
                ps.SiteId,
                SiteName = ps.Site.Name,
                SiteLocation = ps.Site.Location,
                ps.Status
            } as object)
            .ToListAsync();
    }

    public async Task<int> GetActivePatientCountAsync(Guid protocolId)
    {
        return await _ctx.PatientEnrollments
            .Where(e =>
                e.ProtocolSite.ProtocolId == protocolId &&
                e.EnrollmentStatus == "ACTIVE")
            .CountAsync();
    }

    private static EnrollmentDto MapToDto(PatientEnrollment e) => new()
    {
        EnrollmentId = e.EnrollmentId,
        PatientId = e.PatientId,
        PatientName = e.Patient?.Name ?? string.Empty,
        ProtocolSiteId = e.ProtocolSiteId,
        ProtocolTitle = e.ProtocolSite?.Protocol?.Title ?? string.Empty,
        SiteName = e.ProtocolSite?.Site?.Name ?? string.Empty,
        EnrollmentStatus = e.EnrollmentStatus,
        EnrolledAt = e.EnrolledAt
    };
}