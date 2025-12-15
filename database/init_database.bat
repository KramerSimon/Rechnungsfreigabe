@echo off
echo Initializing Rechnungsfreigabe Database...
echo.
echo This will:
echo 1. Create basic users (admin, max.mustermann, maria.mueller, hans.schmidt, lisa.klein)
echo 2. Create roles
echo 3. Create cost centers
echo.
echo All users will have password: Password123!
echo.
pause

mysql -u root rechnungsfreigabe < init_basic_data.sql

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ===================================
    echo Database initialized successfully!
    echo ===================================
    echo.
    echo You can now login with:
    echo Username: admin
    echo Password: Password123!
    echo.
) else (
    echo.
    echo ERROR: Failed to initialize database!
    echo Please check if MySQL is running and you have the correct credentials.
    echo.
)

pause
