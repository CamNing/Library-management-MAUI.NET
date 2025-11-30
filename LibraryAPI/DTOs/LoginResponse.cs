namespace LibraryAPI.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ReaderCardCode { get; set; }
    }
}

