using Microsoft.EntityFrameworkCore;
using PatientService.Data;
using PatientService.Data.Entities;

namespace PatientService.Repository;

public class PatientRepository : IPatientRepository
{
    private readonly ServicesDbContext _ctx;
    public PatientRepository(ServicesDbContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<Patient>> GetAllAsync()
        => await _ctx.Patients
               .Include(p => p.PatientEnrollments)
                   .ThenInclude(e => e.ProtocolSite)
                   .ThenInclude(ps => ps.Protocol)
               .Include(p => p.PatientEnrollments)
                   .ThenInclude(e => e.ProtocolSite)
                   .ThenInclude(ps => ps.Site)
               .OrderBy(p => p.CreatedAt)
               .ToListAsync();

    public async Task<Patient?> GetByIdAsync(Guid id)
        => await _ctx.Patients
               .Include(p => p.PatientEnrollments)
                   .ThenInclude(e => e.ProtocolSite)
                   .ThenInclude(ps => ps.Protocol)
               .Include(p => p.PatientEnrollments)
                   .ThenInclude(e => e.ProtocolSite)
                   .ThenInclude(ps => ps.Site)
               .FirstOrDefaultAsync(p => p.PatientId == id);

    public async Task<Patient> CreateAsync(Patient patient)
    {
        _ctx.Patients.Add(patient);
        await _ctx.SaveChangesAsync();
        return patient;
    }

    public async Task UpdateAsync(Patient patient)
    {
        _ctx.Patients.Update(patient);
        await _ctx.SaveChangesAsync();
    }

    // ✅ Updated: only block duplicate email for ACTIVE patients
    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null)
        => await _ctx.Patients.AnyAsync(p =>
               p.ContactInfo == email &&
               p.PatientStatus == "ACTIVE" &&
               (excludeId == null || p.PatientId != excludeId));
}