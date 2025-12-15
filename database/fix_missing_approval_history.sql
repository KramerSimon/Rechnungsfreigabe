-- Historie-Eintrag für Rechnung INV-2024-001 erstellen
-- Diese Rechnung ist bereits freigegeben, hat aber keinen Historie-Eintrag dafür

-- Zuerst prüfen, ob bereits ein Freigabe-Eintrag existiert
SELECT 
    ih.id,
    ih.invoice_id,
    i.invoice_number,
    ih.action,
    ih.action_type,
    ih.changed_at
FROM invoice_history ih
JOIN invoices i ON ih.invoice_id = i.id
WHERE i.invoice_number = 'INV-2024-001'
ORDER BY ih.changed_at;

-- Freigabe-Eintrag für alle Rechnungen erstellen, die freigegeben sind aber keinen Historie-Eintrag haben
INSERT INTO invoice_history (
    invoice_id,
    action,
    action_type,
    action_source,
    old_status,
    new_status,
    comments,
    changed_by,
    changed_at
)
SELECT 
    i.id,
    CASE 
        WHEN i.auto_approved = 1 THEN 'Automatisch freigegeben'
        ELSE 'Freigabe erteilt'
    END,
    'Approved',
    CASE 
        WHEN i.auto_approved = 1 THEN 'System'
        ELSE 'User'
    END,
    'Freigabe_Erforderlich',
    'Freigegeben',
    CASE 
        WHEN i.auto_approved = 1 THEN CONCAT('Rechnung wurde automatisch freigegeben (unter Schwellenwert)')
        ELSE 'Freigabe durch Administrator erteilt'
    END,
    COALESCE(i.processed_by, i.created_by, 1),
    COALESCE(i.updated_at, i.created_at)
FROM invoices i
WHERE i.status = 'Freigegeben'
AND NOT EXISTS (
    SELECT 1 FROM invoice_history ih 
    WHERE ih.invoice_id = i.id 
    AND ih.action_type = 'Approved'
);

-- Prüfen, was erstellt wurde
SELECT 
    ih.id,
    ih.invoice_id,
    i.invoice_number,
    ih.action,
    ih.action_type,
    ih.action_source,
    ih.old_status,
    ih.new_status,
    ih.comments,
    ih.changed_at
FROM invoice_history ih
JOIN invoices i ON ih.invoice_id = i.id
WHERE i.status = 'Freigegeben'
AND ih.action_type = 'Approved'
ORDER BY i.invoice_number, ih.changed_at;
