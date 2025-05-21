namespace AuthClient.Models
{
    public class SignupRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string Email { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public DateTime Expiry { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
    }
}
