using BC = BCrypt.Net.BCrypt;

namespace RechnungsfreigabeAPI.Tools;

/// <summary>
/// Tool zum Generieren von Passwort-Hashes für die Datenbank
/// </summary>
public static class PasswordHashGenerator
{
    public static void GenerateHashes()
    {
        string password = "password123";
        
        // Generiere mehrere Hashes für verschiedene Benutzer
        Console.WriteLine("=== Passwort-Hashes für Testbenutzer ===");
        Console.WriteLine($"Passwort: {password}");
        Console.WriteLine();
        
        for (int i = 1; i <= 5; i++)
        {
            string hash = BC.HashPassword(password, 11);
            Console.WriteLine($"Hash {i}: {hash}");
        }
        
        Console.WriteLine();
        Console.WriteLine("=== SQL Update Statements ===");
        
        // Generiere SQL Updates für alle Testbenutzer
        string[] usernames = { "admin", "max.mustermann", "maria.mueller", "hans.schmidt", "lisa.klein" };
        
        foreach (string username in usernames)
        {
            string hash = BC.HashPassword(password, 11);
            Console.WriteLine($"UPDATE users SET password_hash = '{hash}' WHERE username = '{username}';");
        }
    }
}