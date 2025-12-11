-- Migration script to add password authentication to existing database
-- Run this if you have an existing database without password fields

USE rechnungsfreigabe;

-- First, check and rename columns to match Entity Framework naming if needed
-- If your database uses snake_case, rename to PascalCase to match the model

-- Check if we need to add the password_hash column
SET @col_exists = 0;
SELECT COUNT(*) INTO @col_exists 
FROM information_schema.columns 
WHERE table_schema = 'rechnungsfreigabe' 
  AND table_name = 'users' 
  AND column_name = 'password_hash';

-- Add password_hash column if it doesn't exist
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE users ADD COLUMN password_hash VARCHAR(255) AFTER username',
    'SELECT "password_hash column already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add other password-related columns if they don't exist
SET @col_exists = 0;
SELECT COUNT(*) INTO @col_exists 
FROM information_schema.columns 
WHERE table_schema = 'rechnungsfreigabe' 
  AND table_name = 'users' 
  AND column_name = 'password_changed_at';

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE users ADD COLUMN password_changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP',
    'SELECT "password_changed_at column already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists = 0;
SELECT COUNT(*) INTO @col_exists 
FROM information_schema.columns 
WHERE table_schema = 'rechnungsfreigabe' 
  AND table_name = 'users' 
  AND column_name = 'failed_login_attempts';

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE users ADD COLUMN failed_login_attempts INT DEFAULT 0',
    'SELECT "failed_login_attempts column already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists = 0;
SELECT COUNT(*) INTO @col_exists 
FROM information_schema.columns 
WHERE table_schema = 'rechnungsfreigabe' 
  AND table_name = 'users' 
  AND column_name = 'locked_until';

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE users ADD COLUMN locked_until TIMESTAMP NULL',
    'SELECT "locked_until column already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Update existing users with default hashed password "password123"
-- BCrypt hash of "password123" with salt rounds 11
-- Disable safe update mode temporarily
SET SQL_SAFE_UPDATES = 0;

UPDATE users SET 
    password_hash = '$2a$11$n6VYQ8YgJ5J5mRy5PXjEleM4lH8mAz7PdX8fMJHmRJYgJ5J5mRy5Pe',
    password_changed_at = NOW(),
    failed_login_attempts = 0
WHERE password_hash IS NULL OR password_hash = '';

-- Re-enable safe update mode
SET SQL_SAFE_UPDATES = 1;

-- Make password_hash required
ALTER TABLE users MODIFY COLUMN password_hash VARCHAR(255) NOT NULL;