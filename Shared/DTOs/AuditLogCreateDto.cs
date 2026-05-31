namespace Shared.CL.DTOs;

public class AuditLogCreateDto
{
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
}
