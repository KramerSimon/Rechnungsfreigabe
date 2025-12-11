-- Upgrade script to enhance invoice_history table for audit trail functionality
-- Run this script to add new columns to existing invoice_history table

USE rechnungsfreigabe;

-- Add new columns to invoice_history table
ALTER TABLE invoice_history 
ADD COLUMN action_type VARCHAR(50) NOT NULL DEFAULT 'Manual' AFTER action,
ADD COLUMN action_source VARCHAR(50) NOT NULL DEFAULT 'User' AFTER action_type,
ADD COLUMN policy_reference VARCHAR(100) NULL AFTER comments,
ADD COLUMN system_reason VARCHAR(255) NULL AFTER policy_reference,
ADD COLUMN import_channel VARCHAR(50) NULL AFTER system_reason;

-- Modify existing columns
ALTER TABLE invoice_history 
MODIFY COLUMN action VARCHAR(100) NOT NULL,
MODIFY COLUMN changed_by INT NULL; -- Allow NULL for system actions

-- Add new indexes for better performance
ALTER TABLE invoice_history 
ADD INDEX idx_action_type (action_type),
ADD INDEX idx_action_source (action_source);

-- Update existing records to have default values
UPDATE invoice_history 
SET 
    action_type = 'Manual',
    action_source = 'User'
WHERE action_type = '' OR action_type IS NULL;

-- Insert some sample history data for testing
INSERT INTO invoice_history (
    invoice_id, 
    action, 
    action_type, 
    action_source, 
    comments, 
    changed_by, 
    changed_at
) VALUES
(1, 'Rechnung eingereicht', 'Submitted', 'User', 'Rechnung wurde vom Lieferanten eingereicht', 1, '2024-12-11 09:00:00'),
(1, 'Kostenstelle zugewiesen', 'FieldChange', 'User', NULL, 1, '2024-12-11 10:30:00'),
(1, 'Automatische Eskalation', 'Escalation', 'System', 'Rechnung automatisch an nächste Freigabeebene weitergeleitet', NULL, '2024-12-11 14:00:00');

SELECT 'Invoice history table upgrade completed successfully!' as result;