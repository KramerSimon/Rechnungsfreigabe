using BC = BCrypt.Net.BCrypt;
Console.WriteLine(BC.HashPassword("password123", 11));
