-- Alle Workflows als "Genehmigt" markieren und Rechnungen freigeben

-- Für Rechnung 13:
UPDATE approval_workflows 
SET status = 'Genehmigt', 
    approved_at = NOW() 
WHERE invoice_id = 13 
AND status IN ('Ausstehend', 'In_Bearbeitung');

UPDATE invoices 
SET status = 'Freigegeben',
    updated_at = NOW()
WHERE id = 13;

-- Prüfen:
SELECT i.id, i.invoice_number, i.status, 
       COUNT(aw.id) as total_workflows,
       SUM(CASE WHEN aw.status = 'Genehmigt' THEN 1 ELSE 0 END) as approved_workflows
FROM invoices i
LEFT JOIN approval_workflows aw ON i.id = aw.invoice_id
WHERE i.id = 13
GROUP BY i.id, i.invoice_number, i.status;
