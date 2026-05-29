namespace Shared.DTOs;

public class VisitDto
{
    public Guid VisitId { get; set; }
    public Guid EnrollmentId { get; set; }
    public string VisitName { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string VisitStatus { get; set; } = string.Empty;
    public string? PrevVisit { get; set; }
    public string? NextVisit { get; set; }
    public string? PatientName { get; set; }
    public string? ProtocolTitle { get; set; }
    public string? SiteName { get; set; }
    // ✅ NEW
    public string? EnrollmentStatus { get; set; }
}

public record BulkVisitItem(string VisitName, DateTime VisitDate);