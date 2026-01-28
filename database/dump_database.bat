@echo off
setlocal

REM ---- Settings ----
set "MYSQLDUMP=C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe"
set "OUTDIR=C:\Users\threk\Documents\Rechnungsfreigabe\database"
set "OUTFILE=%OUTDIR%\database.sql"

set "DB=rechnungsfreigabe"
set "USER=root"
set "PASS=root"

REM ---- Ensure output directory exists ----
if not exist "%OUTDIR%" (
  mkdir "%OUTDIR%"
)

REM ---- Create dump (overwrite old file) ----
"%MYSQLDUMP%" ^
  -u%USER% -p%PASS% ^
  --databases %DB% ^
  --routines ^
  --events ^
  --triggers ^
  --single-transaction ^
  --add-drop-database ^
  --add-drop-table ^
  --create-options ^
  --set-gtid-purged=OFF ^
  --default-character-set=utf8mb4 ^
  > "%OUTFILE%"

IF %ERRORLEVEL% NEQ 0 (
  echo.
  echo Dump FAILED (errorlevel %ERRORLEVEL%).
  echo Check MYSQLDUMP path and credentials.
  exit /b %ERRORLEVEL%
)

echo.
echo Dump OK: "%OUTFILE%"
exit /b 0
