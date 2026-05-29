using Microsoft.EntityFrameworkCore;
using PatientService.Data;
using PatientService.Data.Entities;
using PatientService.Repository;
using Shared.DTOs;

namespace PatientService.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _repo;
    private readonly ServicesDbContext _ctx;

    public PatientService(IPatientRepository repo, ServicesDbContext ctx)
    {
        _repo = repo;
        _ctx = ctx;
    }

    public async Task<IEnumerable<PatientDto>> GetAllAsync()
    {
        var patients = await _repo.GetAllAsync();
        return patients.Select(MapToDto);
    }

    public async Task<PatientDto?> GetByIdAsync(Guid id)
    {
        var patient = await _repo.GetByIdAsync(id);
        return patient is null ? null : MapToDto(patient);
    }

    public async Task<(bool Success, string? Error, PatientDto? Data)> CreateAsync(
        string name, DateOnly dateOfBirth, string? contactInfo)
    {
        if (!string.IsNullOrWhiteSpace(contactInfo) &&
            await _repo.EmailExistsAsync(contactInfo))
            return (false, "A patient with this email already exists.", null);

        var patient = new Patient
        {
            Name = name,
            DateOfBirth = dateOfBirth,
            ContactInfo = contactInfo,
            PatientStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(patient);
        return (true, null, MapToDto(created));
    }

    public async Task<(bool Success, string? Error, PatientDto? Data)> UpdateAsync(
        Guid id, string name, DateOnly dateOfBirth, string? contactInfo)
    {
        var patient = await _repo.GetByIdAsync(id);
        if (patient is null)
            return (false, "Patient not found.", null);

        if (!string.IsNullOrWhiteSpace(contactInfo) &&
            await _repo.EmailExistsAsync(contactInfo, id))
            return (false, "Email already used by another patient.", null);

        patient.Name = name;
        patient.DateOfBirth = dateOfBirth;
        patient.ContactInfo = contactInfo;

        await _repo.UpdateAsync(patient);
        return (true, null, MapToDto(patient));
    }

    public async Task<(bool Success, string? Error)> DeactivateAsync(
        Guid id, string reason)
    {
        var patient = await _repo.GetByIdAsync(id);
        if (patient is null)
            return (false, "Patient not found.");

        if (patient.PatientStatus == "INACTIVE")
            return (false, "Patient is already inactive.");

        var activeEnrollment = await _ctx.PatientEnrollments
            .FirstOrDefaultAsync(e =>
                e.PatientId == id &&
                e.EnrollmentStatus == "ACTIVE");

        if (activeEnrollment is not null)
        {
            // ✅ Cancel all scheduled/rescheduled visits
            var visits = await _ctx.Visits
                .Where(v => v.EnrollmentId == activeEnrollment.EnrollmentId &&
                       (v.VisitStatus == "SCHEDULED" || v.VisitStatus == "RESCHEDULED"))
                .ToListAsync();

            foreach (var v in visits)
                v.VisitStatus = "CANCELLED";

            if (visits.Any())
                _ctx.Visits.UpdateRange(visits);

            activeEnrollment.EnrollmentStatus = "WITHDRAWN";
            _ctx.PatientEnrollments.Update(activeEnrollment);
        }

        patient.PatientStatus = "INACTIVE";
        await _repo.UpdateAsync(patient);
        await _ctx.SaveChangesAsync();

        return (true, null);
    }

    private static PatientDto MapToDto(Patient p)
    {
        var enrollment = p.PatientEnrollments
            .OrderByDescending(e => e.EnrolledAt)
            .FirstOrDefault();

        string status = p.PatientStatus == "INACTIVE"
            ? "Inactive"
            : enrollment?.EnrollmentStatus switch
            {
                "ACTIVE" => "Active",
                "COMPLETED" => "Completed",
                "WITHDRAWN" => "Withdrawn",
                _ => "Not enrolled"
            };

        var previousProtocols = p.PatientEnrollments
            .Select(e => e.ProtocolSite?.Protocol?.Title ?? "")
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .ToList();

        return new PatientDto
        {
            PatientId = p.PatientId,
            Name = p.Name,
            DateOfBirth = p.DateOfBirth,
            ContactInfo = p.ContactInfo,
            PatientStatus = p.PatientStatus,
            CreatedAt = p.CreatedAt,
            EnrollmentStatus = status,
            EnrollmentId = enrollment?.EnrollmentId,
            ProtocolTitle = enrollment?.ProtocolSite?.Protocol?.Title,
            SiteName = enrollment?.ProtocolSite?.Site?.Name,
            EnrolledAt = enrollment?.EnrolledAt,
            PreviousProtocols = previousProtocols
        };
    }
}