namespace Shared.CL;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T data, string message = "Success") =>
        new() { IsSuccess = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message) =>
        new() { IsSuccess = false, Message = message };
}
