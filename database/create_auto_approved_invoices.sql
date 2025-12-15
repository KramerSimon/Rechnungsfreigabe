-- Rechnungen erstellen, die automatisch freigegeben werden
-- Diese Rechnungen haben bereits den Status "Freigegeben" und auto_approved = 1
-- OHNE project_id und purchase_order_id um Foreign Key Fehler zu vermeiden

INSERT INTO invoices (
    invoice_number, 
    supplier_id, 
    invoice_date, 
    due_date, 
    total_amount, 
    net_amount,
    tax_amount,
    currency,
    description,
    status,
    auto_approved,
    requires_approval,
    cost_center_id,
    created_by,
    created_at
) VALUES 
-- Rechnung 1: Kleine Büromaterial-Rechnung (unter 100 EUR)
(
    'INV-2025-0020',
    1,  -- Supplier ID
    '2025-01-10',
    '2025-02-10',
    85.00,
    71.43,
    13.57,
    'EUR',
    'Büromaterial - automatisch freigegeben (unter 100 EUR)',
    'Freigegeben',
    1,
    0,
    'IT',
    1,  -- created_by user ID
    NOW()
),
-- Rechnung 2: Kleine Dienstleistungsrechnung
(
    'INV-2025-0021',
    1,
    '2025-01-11',
    '2025-02-11',
    75.50,
    63.45,
    12.05,
    'EUR',
    'Wartungsarbeiten - automatisch freigegeben',
    'Freigegeben',
    1,
    0,
    'IT',
    1,
    NOW()
),
-- Rechnung 3: Express-Lieferung (niedrig)
(
    'INV-2025-0022',
    1,
    '2025-01-12',
    '2025-02-12',
    95.00,
    79.83,
    15.17,
    'EUR',
    'Express-Lieferung Hardware - automatisch freigegeben',
    'Freigegeben',
    1,
    0,
    'IT',
    1,
    NOW()
),
-- Rechnung 4: Verbrauchsmaterial
(
    'INV-2025-0023',
    1,
    '2025-01-13',
    '2025-02-13',
    65.00,
    54.62,
    10.38,
    'EUR',
    'Verbrauchsmaterial Drucker - automatisch freigegeben',
    'Freigegeben',
    1,
    0,
    'FINANCE',
    1,
    NOW()
),
-- Rechnung 5: Kleiner Servicebetrag
(
    'INV-2025-0024',
    1,
    '2025-01-14',
    '2025-02-14',
    89.99,
    75.62,
    14.37,
    'EUR',
    'Monatliche Wartung - automatisch freigegeben',
    'Freigegeben',
    1,
    0,
    'IT',
    1,
    NOW()
);

-- Historie-Einträge für die automatisch freigegebenen Rechnungen erstellen
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
    CONCAT('Rechnung wurde automatisch freigegeben: ', i.description),
    i.created_by,
    i.created_at
FROM invoices i
WHERE i.invoice_number IN ('INV-2025-0020', 'INV-2025-0021', 'INV-2025-0022', 'INV-2025-0023', 'INV-2025-0024');

-- Prüfen, welche Rechnungen erstellt wurden
SELECT 
    id,
    invoice_number,
    total_amount,
    status,
    auto_approved,
    description,
    created_at
FROM invoices 
WHERE invoice_number LIKE 'INV-2025-002%'
ORDER BY id DESC;
