using System.Text.Json.Serialization;

namespace RecruitmentSystem.Shared.DTOs.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Errors { get; set; } = new List<string>();

        public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message ?? "Operation completed successfully.",
                Data = data,
                Errors = null
            };
        }

        public static ApiResponse<T> FailureResponse (List<string> errors, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message ?? "Operation failed.",
                Data = default,
                Errors = errors ?? ["An unknown error occurred."]
            };
        }
    }

    public class ApiResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public List<string>? Errors { get; set; } = new List<string>();

            public static ApiResponse SuccessResponse(string? message = null)
            {
                return new ApiResponse
                {
                    Success = true,
                    Message = message ?? "Operation completed successfully.",
                    Errors = null
                };
            }

            public static ApiResponse FailureResponse (List<string> errors, string? message = null)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = message ?? "Operation failed.",
                    Errors = errors ?? ["An unknown error occurred."]
                };
            }
        }
}