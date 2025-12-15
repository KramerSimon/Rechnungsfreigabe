-- Historie-Einträge für bereits freigegebene Rechnungen erstellen
-- Diese sollten nach dem Einfügen der Rechnungen ausgeführt werden

-- Für jede automatisch freigegebene Rechnung einen Historie-Eintrag erstellen
INSERT INTO invoice_history (
    invoice_id,
    action,
    action_type,
    action_source,
    new_status,
    comments,
    changed_by,
    changed_at
)
SELECT 
    i.id,
    'Automatisch freigegeben',
    'Approved',
    'System',
    'Freigegeben',
    CONCAT('Rechnung automatisch freigegeben: ', i.description),
    i.created_by,
    i.created_at
FROM invoices i
WHERE i.auto_approved = 1
AND i.status = 'Freigegeben'
AND NOT EXISTS (
    SELECT 1 FROM invoice_history ih 
    WHERE ih.invoice_id = i.id 
    AND ih.action_type IN ('Approved', 'AutoApproved')
);

-- Prüfen, welche Historie-Einträge erstellt wurden
SELECT 
    ih.id,
    ih.invoice_id,
    i.invoice_number,
    ih.action,
    ih.action_type,
    ih.new_status,
    ih.changed_at
FROM invoice_history ih
JOIN invoices i ON ih.invoice_id = i.id
WHERE i.invoice_number LIKE 'INV-2025-002%'
ORDER BY ih.invoice_id, ih.changed_at;
