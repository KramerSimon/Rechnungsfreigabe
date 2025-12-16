-- PDF Upload Feature - Database Migration
-- Adds PDF file support to the invoices table
-- Date: 2024-12-16

-- Schritt 1: Neue Spalten zur invoices-Tabelle hinzufügen
ALTER TABLE invoices
ADD COLUMN IF NOT EXISTS pdf_file_path VARCHAR(500),
ADD COLUMN IF NOT EXISTS pdf_file_size BIGINT,
ADD COLUMN IF NOT EXISTS original_filename VARCHAR(255);

-- Schritt 2: Indizes erstellen für bessere Performance
CREATE INDEX IF NOT EXISTS idx_invoices_pdf_file_path ON invoices(pdf_file_path);
CREATE INDEX IF NOT EXISTS idx_invoices_original_filename ON invoices(original_filename);

-- Schritt 3: Kommentar hinzufügen (optional)
ALTER TABLE invoices
MODIFY COLUMN pdf_file_path VARCHAR(500) COMMENT 'Path to the uploaded PDF file in the file system',
MODIFY COLUMN pdf_file_size BIGINT COMMENT 'Size of the PDF file in bytes',
MODIFY COLUMN original_filename VARCHAR(255) COMMENT 'Original filename as provided by the user';

-- Schritt 4: Berechtigungen überprüfen (falls nötig)
-- GRANT SELECT, INSERT, UPDATE, DELETE ON invoices TO 'app_user'@'localhost';

-- Schritt 5: Backup erstellen (empfohlen vor der Migration)
-- Führe diesen Befehl aus BEVOR du die obigen Änderungen durchführst:
-- BACKUP DATABASE rechnungsfreigabe TO DISK = '/path/to/backup';

-- Schritt 6: Daten-Validierungsprüfung
-- Überprüfe ob alle Rechnungen korrekt sind
SELECT COUNT(*) as total_invoices, 
       COUNT(pdf_file_path) as invoices_with_pdf,
       SUM(pdf_file_size) / (1024*1024) as total_pdf_size_mb
FROM invoices;

-- Schritt 7: Cleanup (Optional - alte Daten löschen, falls nötig)
-- UPDATE invoices SET pdf_file_path = NULL WHERE pdf_file_path = '';
-- DELETE FROM invoices WHERE pdf_file_path IS NULL AND created_at < DATE_SUB(NOW(), INTERVAL 90 DAY);

-- Schritt 8: Tabellenspeicher optimieren
ANALYZE TABLE invoices;
OPTIMIZE TABLE invoices;

-- Schritt 9: Verifizierung
-- Überprüfe die neue Struktur
DESCRIBE invoices;

-- Schritt 10: Versionskontrolle
-- Füge einen Versionshinweis hinzu (falls existierende Versionstabelle vorhanden)
-- INSERT INTO schema_versions (version, description, applied_at) 
-- VALUES ('002_add_pdf_support', 'Add PDF file storage to invoices table', NOW());
