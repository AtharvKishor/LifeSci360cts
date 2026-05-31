using System.Text.Json.Serialization;

namespace Shared.CL;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "Success")
        => new ApiResponse<T> { Success = true, Message = message, Data = data };

    public static ApiResponse<T> OkOnly(string message = "Success")
        => new ApiResponse<T> { Success = true, Message = message };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null)
        => new ApiResponse<T> { Success = false, Message = message, Errors = errors };
}
