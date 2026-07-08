namespace demo1.DTOs
{
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; }
        public LoginResponse? Response { get; set; }

        public static AuthResult Success(LoginResponse response)
        {
            return new AuthResult { IsSuccess = true, StatusCode = 200, Response = response };
        }

        public static AuthResult Fail(int statusCode, string errorMessage)
        {
            return new AuthResult { IsSuccess = false, StatusCode = statusCode, ErrorMessage = errorMessage };
        }
    }
}
