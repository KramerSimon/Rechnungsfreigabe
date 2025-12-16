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
        // ...existing code...
        
        for (int i = 1; i <= 5; i++)
        {
            string hash = BC.HashPassword(password, 11);
            // ...existing code...
        }
        
        // ...existing code...
        
        // Generiere SQL Updates für alle Testbenutzer
        string[] usernames = { "admin", "max.mustermann", "maria.mueller", "hans.schmidt", "lisa.klein" };
        
        foreach (string username in usernames)
        {
            string hash = BC.HashPassword(password, 11);
            // ...existing code...
        }
    }
}