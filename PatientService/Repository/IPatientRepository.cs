using PatientService.Data.Entities;

namespace PatientService.Repository;

public interface IPatientRepository
{
    Task<IEnumerable<Patient>> GetAllAsync();
    Task<Patient?> GetByIdAsync(Guid id);
    Task<Patient> CreateAsync(Patient patient);
    Task UpdateAsync(Patient patient);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
}