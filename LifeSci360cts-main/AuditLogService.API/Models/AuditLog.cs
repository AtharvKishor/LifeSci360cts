namespace AuditLogService.API.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string Action { get; set; } = null!;
        public string ServiceName { get; set; } = null!;
        public bool IsError { get; set; } = false;  // explicit default
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}