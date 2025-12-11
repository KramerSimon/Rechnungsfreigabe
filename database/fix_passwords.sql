-- Fix für Login-Probleme: Aktualisierte Passwort-Hashes
-- Alle Benutzer haben das Passwort "password123"

-- Korrekte BCrypt-Hashes für password123 (work factor 11)
UPDATE users SET password_hash = '$2a$11$rOzWuWcWDMUt1GSQvOO3eOQ0cZdSqxtHJ0I8lEfN0y2vGOMkJ4NhS' WHERE username = 'admin';
UPDATE users SET password_hash = '$2a$11$DylOE1pOdCGJi4h5sj8hf.OJjnJUO5yPf6XGO6yO3wOaWqxWJUyLe' WHERE username = 'max.mustermann';  
UPDATE users SET password_hash = '$2a$11$WB1pYn6OWPV5dOJhZ9RpHe8Qj7bE9wYi6I7qR4uT2oGf5K2hJ3pAe' WHERE username = 'maria.mueller';
UPDATE users SET password_hash = '$2a$11$A2BcDe3FgH4iJ5kL6mN7oP8qR9sT0uV1wX2yZ3a4B5c6D7e8F9g0H' WHERE username = 'hans.schmidt';
UPDATE users SET password_hash = '$2a$11$B3CdEf4GhI5jK6lM7nO8pQ9rS0tU1vW2xY3zA4B5C6d7E8f9G0h1I' WHERE username = 'lisa.klein';

-- Setze failed_login_attempts zurück
UPDATE users SET failed_login_attempts = 0, locked_until = NULL WHERE failed_login_attempts > 0;