using BC = BCrypt.Net.BCrypt;

namespace RechnungsfreigabeAPI.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    bool IsPasswordValid(string password);
    string GenerateRandomPassword(int length = 12);
}

public class PasswordService : IPasswordService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordService> _logger;
    
    public PasswordService(IConfiguration configuration, ILogger<PasswordService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        // Use BCrypt with work factor 11 (secure but not too slow)
        return BC.HashPassword(password, 11);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BC.Verify(password, hash);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error verifying password hash");
            return false;
        }
    }

    public bool IsPasswordValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        // Minimum password requirements
        if (password.Length < 6)
            return false;

        // Optional: Add more complex validation rules
        // - Must contain uppercase letter
        // - Must contain lowercase letter  
        // - Must contain number
        // - Must contain special character
        
        return true;
    }

    public string GenerateRandomPassword(int length = 12)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}