using BC = BCrypt.Net.BCrypt;


namespace RechnungsfreigabeAPI.Services.Interfaces;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    bool IsPasswordValid(string password);
    string GenerateRandomPassword(int length = 12);
}
