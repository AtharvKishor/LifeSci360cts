using Microsoft.EntityFrameworkCore;
using PatientService.Data;
using PatientService.Data.Entities;

namespace PatientService.Repository;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly ServicesDbContext _ctx;
    public EnrollmentRepository(ServicesDbContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<PatientEnrollment>> GetAllAsync()
        => await _ctx.PatientEnrollments
               .Include(e => e.Patient)
               .Include(e => e.ProtocolSite).ThenInclude(ps => ps.Protocol)
               .Include(e => e.ProtocolSite).ThenInclude(ps => ps.Site)
               .OrderByDescending(e => e.EnrolledAt)
               .ToListAsync();

    public async Task<PatientEnrollment?> GetByIdAsync(Guid id)
        => await _ctx.PatientEnrollments
               .Include(e => e.Patient)
               .Include(e => e.ProtocolSite).ThenInclude(ps => ps.Protocol)
               .Include(e => e.ProtocolSite).ThenInclude(ps => ps.Site)
               .Include(e => e.Visits)
               .FirstOrDefaultAsync(e => e.EnrollmentId == id);

    public async Task<bool> HasActiveEnrollmentAsync(Guid patientId)
        => await _ctx.PatientEnrollments.AnyAsync(e =>
               e.PatientId == patientId &&
               e.EnrollmentStatus == "ACTIVE");

    public async Task<PatientEnrollment> CreateAsync(PatientEnrollment enrollment)
    {
        _ctx.PatientEnrollments.Add(enrollment);
        await _ctx.SaveChangesAsync();
        return enrollment;
    }

    public async Task UpdateAsync(PatientEnrollment enrollment)
    {
        _ctx.PatientEnrollments.Update(enrollment);
        await _ctx.SaveChangesAsync();
    }
}