-- Status der Rechnung 13 prüfen und aktualisieren
-- Zuerst den aktuellen Status prüfen:
SELECT id, invoice_number, status, total_amount, cost_center_id, project_id, purchase_order_id 
FROM invoices 
WHERE id = 13;

-- Approval Workflows für Rechnung 13 prüfen:
SELECT id, invoice_id, status, approval_level, step_number, approver_id, approved_at 
FROM approval_workflows 
WHERE invoice_id = 13
ORDER BY step_number;

-- Falls alle Workflows genehmigt sind, Status auf 'Freigegeben' setzen:
UPDATE invoices 
SET status = 'Freigegeben' 
WHERE id = 13 
AND status = 'Freigabe_Erforderlich'
AND NOT EXISTS (
    SELECT 1 FROM approval_workflows 
    WHERE invoice_id = 13 
    AND status IN ('Ausstehend', 'In_Bearbeitung')
);

-- Prüfen ob Update erfolgreich war:
SELECT id, invoice_number, status FROM invoices WHERE id = 13;
