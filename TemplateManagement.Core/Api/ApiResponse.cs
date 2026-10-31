namespace TemplateManagement.Core.Api
{
    public sealed class ApiResponse<T>
    {
        public int Status { get; init; }
        public T? Data { get; init; }
        public string? Message { get; init; }

        /// <summary>Tạo phản hồi API thống nhất với trạng thái, dữ liệu và thông điệp.</summary>
        public static ApiResponse<T> Create(int status, T? data, string? message) =>
            new() { Status = status, Data = data, Message = message };
    }
}
