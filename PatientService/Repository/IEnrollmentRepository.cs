using PatientService.Data.Entities;

namespace PatientService.Repository;

public interface IEnrollmentRepository
{
    Task<IEnumerable<PatientEnrollment>> GetAllAsync();
    Task<PatientEnrollment?> GetByIdAsync(Guid id);
    Task<bool> HasActiveEnrollmentAsync(Guid patientId);
    Task<PatientEnrollment> CreateAsync(PatientEnrollment enrollment);
    Task UpdateAsync(PatientEnrollment enrollment);
}