namespace Shared.DTOs;

public record NotificationDto(
    int NotificationID,
    int UserID,
    string Message,
    string Category,
    string Status,
    DateTime CreatedDate
);

public record CreateNotificationDto(
    int UserID,
    string Message,
    string Category,
    List<string> Channels   // InApp | Email | SMS
);

public record UpdateNotificationStatusDto(string Status);

public record NotificationHistoryDto(
    int UserId,
    DateTime From,
    DateTime To,
    int TotalCount,
    int ReadCount,
    int UnreadCount,
    List<CategorySummary> ByCategory
);

public record CategorySummary(string Category, int Count);