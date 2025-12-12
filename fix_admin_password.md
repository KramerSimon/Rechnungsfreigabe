# Fix Admin Login Issue

## Problem
Cannot login as admin with password `password123`

## Solution

### Option 1: Use DevTools API (Recommended if backend is running)
1. Make sure the backend is running
2. Call the password fix endpoint:
   ```bash
   curl -X POST http://localhost:5068/api/DevTools/fix-passwords
   ```

### Option 2: Direct MySQL Update
Run this SQL in MySQL:
```sql
USE rechnungsfreigabe;

-- Update admin password hash for "password123"
UPDATE users 
SET password_hash = '$2a$11$rOzWuWcWDMUt1GSQvOO3eOQ0cZdSqxtHJ0I8lEfN0y2vGOMkJ4NhS',
    failed_login_attempts = 0,
    locked_until = NULL
WHERE username = 'admin';

-- Verify the update
SELECT username, password_hash, failed_login_attempts, locked_until, is_active 
FROM users 
WHERE username = 'admin';
```

### Option 3: Run the fix_passwords.sql script
```bash
mysql -u root -p rechnungsfreigabe < database/fix_passwords.sql
```

## After Fixing
Try logging in with:
- **Username:** `admin`
- **Password:** `password123`

## Additional Checks
If login still fails, verify:
1. Backend is running and accessible
2. Database connection is working
3. User is active: `SELECT is_active FROM users WHERE username = 'admin';`
4. No account lockout: `SELECT locked_until FROM users WHERE username = 'admin';`
