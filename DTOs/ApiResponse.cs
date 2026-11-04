namespace EverestRemit.Api.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Item { get; set; }

        public static ApiResponse<T> Ok(
            T item,
            string message = "Request successful")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Item = item
            };
        }

        public static ApiResponse<T> Fail(
            string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Item = default
            };
        }
    }
}
