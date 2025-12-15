-- Alle Rechnungen mit ihren Approval-Status prüfen
SELECT 
    i.id,
    i.invoice_number,
    i.status AS invoice_status,
    i.requires_approval,
    aw.id AS workflow_id,
    aw.status AS workflow_status,
    aw.approval_level,
    aw.step_number,
    aw.approver_id,
    aw.approved_at,
    u.username AS approver_name
FROM invoices i
LEFT JOIN approval_workflows aw ON i.id = aw.invoice_id
LEFT JOIN users u ON aw.approver_id = u.id
WHERE i.id IN (10, 12, 13, 14)
ORDER BY i.id, aw.step_number;

-- Zähle ausstehende Approvals pro Rechnung
SELECT 
    invoice_id,
    COUNT(*) AS total_workflows,
    SUM(CASE WHEN status = 'Genehmigt' THEN 1 ELSE 0 END) AS approved_count,
    SUM(CASE WHEN status IN ('Ausstehend', 'In_Bearbeitung') THEN 1 ELSE 0 END) AS pending_count
FROM approval_workflows
WHERE invoice_id IN (10, 12, 13, 14)
GROUP BY invoice_id;
