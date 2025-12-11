-- Rechnungsfreigabe Database Schema für MySQL
-- Erstellt für die Invoice Approval System Anwendung

-- Datenbank erstellen
CREATE DATABASE IF NOT EXISTS rechnungsfreigabe 
DEFAULT CHARACTER SET utf8mb4 
DEFAULT COLLATE utf8mb4_unicode_ci;

USE rechnungsfreigabe;

-- 1. Benutzer und Rollen
CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    active_directory_sid VARCHAR(255) UNIQUE,
    password_changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    failed_login_attempts INT DEFAULT 0,
    locked_until TIMESTAMP NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE roles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    permissions JSON -- JSON-Array mit Berechtigungen
);

CREATE TABLE user_roles (
    user_id INT NOT NULL,
    role_id INT NOT NULL,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, role_id),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
);

-- 2. Kostenstellen und Projekte
CREATE TABLE cost_centers (
    id VARCHAR(20) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    manager_id INT,
    budget DECIMAL(12,2) DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (manager_id) REFERENCES users(id)
);

CREATE TABLE projects (
    id VARCHAR(20) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    cost_center_id VARCHAR(20) NOT NULL,
    budget DECIMAL(12,2) DEFAULT 0,
    spent_amount DECIMAL(12,2) DEFAULT 0,
    status ENUM('Geplant', 'Aktiv', 'Pausiert', 'Abgeschlossen') DEFAULT 'Geplant',
    start_date DATE,
    end_date DATE,
    project_manager_id INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (cost_center_id) REFERENCES cost_centers(id),
    FOREIGN KEY (project_manager_id) REFERENCES users(id)
);

-- 3. Lieferanten
CREATE TABLE suppliers (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    legal_name VARCHAR(150),
    tax_number VARCHAR(30),
    vat_number VARCHAR(30),
    address_line1 VARCHAR(100),
    address_line2 VARCHAR(100),
    postal_code VARCHAR(10),
    city VARCHAR(50),
    country VARCHAR(50) DEFAULT 'Deutschland',
    email VARCHAR(255),
    phone VARCHAR(30),
    bank_name VARCHAR(100),
    iban VARCHAR(34),
    bic VARCHAR(11),
    payment_terms_days INT DEFAULT 30,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- 4. Bestellungen (Purchase Orders)
CREATE TABLE purchase_orders (
    id VARCHAR(20) PRIMARY KEY,
    title VARCHAR(100) NOT NULL,
    description TEXT,
    cost_center_id VARCHAR(20),
    project_id VARCHAR(20),
    total_amount DECIMAL(12,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'EUR',
    status ENUM('Offen', 'Teilweise_Erfuellt', 'Erfuellt', 'Storniert') DEFAULT 'Offen',
    created_by INT NOT NULL,
    approved_by INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    approved_at TIMESTAMP NULL,
    FOREIGN KEY (cost_center_id) REFERENCES cost_centers(id),
    FOREIGN KEY (project_id) REFERENCES projects(id),
    FOREIGN KEY (created_by) REFERENCES users(id),
    FOREIGN KEY (approved_by) REFERENCES users(id)
);

-- 5. Rechnungen (Invoices)
CREATE TABLE invoices (
    id INT AUTO_INCREMENT PRIMARY KEY,
    invoice_number VARCHAR(50) NOT NULL,
    supplier_id INT NOT NULL,
    purchase_order_id VARCHAR(20),
    cost_center_id VARCHAR(20),
    project_id VARCHAR(20),
    
    -- Beträge
    net_amount DECIMAL(12,2) NOT NULL,
    tax_amount DECIMAL(12,2) NOT NULL DEFAULT 0,
    total_amount DECIMAL(12,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'EUR',
    
    -- Daten
    invoice_date DATE NOT NULL,
    due_date DATE NOT NULL,
    received_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- Status und Workflow
    status ENUM('Eingegangen', 'In_Pruefung', 'Freigabe_Erforderlich', 'Freigegeben', 
                'Abgelehnt', 'Bezahlt', 'Ueberfaellig', 'Storniert') DEFAULT 'Eingegangen',
    
    -- Freigabe-Informationen
    requires_approval BOOLEAN DEFAULT TRUE,
    approval_level INT DEFAULT 1,
    auto_approved BOOLEAN DEFAULT FALSE,
    
    -- Dokumente
    pdf_file_path VARCHAR(500),
    pdf_file_size BIGINT,
    original_filename VARCHAR(255),
    
    -- Notizen und Kommentare
    description TEXT,
    internal_notes TEXT,
    
    -- Audit-Felder
    created_by INT,
    processed_by INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (supplier_id) REFERENCES suppliers(id),
    FOREIGN KEY (purchase_order_id) REFERENCES purchase_orders(id),
    FOREIGN KEY (cost_center_id) REFERENCES cost_centers(id),
    FOREIGN KEY (project_id) REFERENCES projects(id),
    FOREIGN KEY (created_by) REFERENCES users(id),
    FOREIGN KEY (processed_by) REFERENCES users(id),
    
    INDEX idx_invoice_number (invoice_number),
    INDEX idx_status (status),
    INDEX idx_supplier_id (supplier_id),
    INDEX idx_cost_center_id (cost_center_id),
    INDEX idx_project_id (project_id),
    INDEX idx_invoice_date (invoice_date),
    INDEX idx_due_date (due_date),
    INDEX idx_received_date (received_date)
);

-- 6. Freigabe-Regeln (Approval Rules)
CREATE TABLE approval_rules (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    rule_type ENUM('automatic', 'manual') NOT NULL,
    priority INT NOT NULL DEFAULT 10,
    is_active BOOLEAN DEFAULT TRUE,
    conditions JSON NOT NULL, -- JSON-Array mit Bedingungen
    actions JSON NOT NULL,    -- JSON-Array mit Aktionen
    created_by INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (created_by) REFERENCES users(id)
);

-- 7. Freigabe-Workflows
CREATE TABLE approval_workflows (
    id INT AUTO_INCREMENT PRIMARY KEY,
    invoice_id INT NOT NULL,
    rule_id INT,
    step_number INT NOT NULL,
    approver_id INT NOT NULL,
    approval_level INT NOT NULL,
    status ENUM('Pending', 'Approved', 'Rejected', 'Skipped') DEFAULT 'Pending',
    comments TEXT,
    approved_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE CASCADE,
    FOREIGN KEY (rule_id) REFERENCES approval_rules(id),
    FOREIGN KEY (approver_id) REFERENCES users(id),
    
    INDEX idx_invoice_id (invoice_id),
    INDEX idx_approver_id (approver_id),
    INDEX idx_status (status)
);

-- 8. Rechnungshistorie / Audit Trail
CREATE TABLE invoice_history (
    id INT AUTO_INCREMENT PRIMARY KEY,
    invoice_id INT NOT NULL,
    action VARCHAR(50) NOT NULL,
    old_status VARCHAR(20),
    new_status VARCHAR(20),
    field_changes JSON, -- JSON mit geänderten Feldern
    comments TEXT,
    changed_by INT NOT NULL,
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE CASCADE,
    FOREIGN KEY (changed_by) REFERENCES users(id),
    
    INDEX idx_invoice_id (invoice_id),
    INDEX idx_changed_at (changed_at)
);

-- 9. Benachrichtigungen
CREATE TABLE notifications (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    invoice_id INT,
    type VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    is_read BOOLEAN DEFAULT FALSE,
    priority ENUM('low', 'normal', 'high', 'urgent') DEFAULT 'normal',
    action_url VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    read_at TIMESTAMP NULL,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (invoice_id) REFERENCES invoices(id),
    
    INDEX idx_user_id (user_id),
    INDEX idx_is_read (is_read)
);

-- 10. System-Konfiguration
CREATE TABLE system_config (
    id INT AUTO_INCREMENT PRIMARY KEY,
    config_key VARCHAR(100) NOT NULL UNIQUE,
    config_value TEXT,
    data_type ENUM('string', 'number', 'boolean', 'json') DEFAULT 'string',
    description TEXT,
    is_editable BOOLEAN DEFAULT TRUE,
    updated_by INT,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (updated_by) REFERENCES users(id)
);

-- INITIAL DATA / Stammdaten
INSERT INTO roles (name, description, permissions) VALUES
('Administrator', 'System Administrator mit allen Rechten', '["all"]'),
('Freigeber', 'Kann Rechnungen freigeben und ablehnen', '["approve_invoices", "view_all_invoices"]'),
('Buchhaltung', 'Kann alle Rechnungen einsehen und bearbeiten', '["view_all_invoices", "edit_invoices", "process_payments"]'),
('Mitarbeiter', 'Kann eigene Rechnungen einsehen', '["view_own_invoices"]'),
('Controller', 'Kann Reports erstellen und Budgets überwachen', '["view_reports", "view_budgets"]'),
('Manager', 'Kann Kostenstellen-bezogene Rechnungen freigeben', '["approve_cost_center_invoices", "view_team_invoices"]');

INSERT INTO cost_centers (id, name, description, budget) VALUES
('IT', 'IT-Abteilung', 'Informationstechnologie und Digitalisierung', 250000.00),
('HR', 'Personalabteilung', 'Human Resources und Personalentwicklung', 150000.00),
('SALES', 'Vertrieb', 'Verkauf und Marketing', 300000.00),
('FINANCE', 'Finanzen', 'Buchhaltung, Controlling und Finanzen', 100000.00),
('OFFICE', 'Büromaterial', 'Allgemeine Büroausstattung und Verbrauchsmaterial', 25000.00),
('FACILITY', 'Facility Management', 'Gebäude, Reinigung, Sicherheit', 80000.00);

-- Beispiel-Benutzer mit gehashten Passwörtern (alle verwenden "password123" als Passwort)
INSERT INTO users (username, password_hash, email, first_name, last_name, password_changed_at) VALUES
('admin', '$2a$11$n6VYQ8YgJ5J5mRy5PXjEleM4lH8mAz7PdX8fMJHmRJYgJ5J5mRy5Pe', 'admin@firma.de', 'System', 'Administrator', NOW()),
('max.mustermann', '$2a$11$n6VYQ8YgJ5J5mRy5PXjEleM4lH8mAz7PdX8fMJHmRJYgJ5J5mRy5Pe', 'max.mustermann@firma.de', 'Max', 'Mustermann', NOW()),
('maria.mueller', '$2a$11$n6VYQ8YgJ5J5mRy5PXjEleM4lH8mAz7PdX8fMJHmRJYgJ5J5mRy5Pe', 'maria.mueller@firma.de', 'Maria', 'Müller', NOW()),
('hans.schmidt', '$2a$11$n6VYQ8YgJ5J5mRy5PXjEleM4lH8mAz7PdX8fMJHmRJYgJ5J5mRy5Pe', 'hans.schmidt@firma.de', 'Hans', 'Schmidt', NOW()),
('lisa.klein', '$2a$11$n6VYQ8YgJ5J5mRy5PXjEleM4lH8mAz7PdX8fMJHmRJYgJ5J5mRy5Pe', 'lisa.klein@firma.de', 'Lisa', 'Klein', NOW());

-- Benutzerrollen zuweisen
INSERT INTO user_roles (user_id, role_id) VALUES
(1, 1), -- admin -> Administrator
(2, 2), -- max.mustermann -> Freigeber
(3, 3), -- maria.mueller -> Buchhaltung
(4, 6), -- hans.schmidt -> Manager
(5, 5); -- lisa.klein -> Controller

-- Manager für Kostenstellen setzen
UPDATE cost_centers SET manager_id = 4 WHERE id = 'IT';
UPDATE cost_centers SET manager_id = 3 WHERE id = 'HR';
UPDATE cost_centers SET manager_id = 2 WHERE id = 'SALES';
UPDATE cost_centers SET manager_id = 5 WHERE id = 'FINANCE';
UPDATE cost_centers SET manager_id = 1 WHERE id = 'OFFICE';

-- Beispiel-Projekte
INSERT INTO projects (id, name, description, cost_center_id, budget, project_manager_id, status) VALUES
('WEB001', 'Website Relaunch', 'Neugestaltung der Unternehmenswebsite', 'IT', 25000.00, 4, 'Aktiv'),
('HR002', 'Mitarbeiter-Portal', 'Entwicklung eines Self-Service Portals für Mitarbeiter', 'HR', 15000.00, 3, 'Geplant'),
('SALES003', 'CRM System', 'Einführung eines neuen Customer Relationship Management Systems', 'SALES', 40000.00, 2, 'Aktiv'),
('OFF004', 'Büroausstattung 2024', 'Modernisierung der Büroausstattung', 'OFFICE', 5000.00, 1, 'Aktiv');

-- Beispiel-Lieferanten
INSERT INTO suppliers (name, legal_name, email, phone, city, payment_terms_days) VALUES
('Microsoft Deutschland', 'Microsoft Deutschland GmbH', 'rechnungen@microsoft.de', '+49 89 31760', 'München', 30),
('Amazon Business', 'Amazon EU S.à.r.l.', 'business@amazon.de', '+49 800 000000', 'München', 14),
('Büro-Express', 'Büro-Express GmbH & Co. KG', 'service@buero-express.de', '+49 40 123456', 'Hamburg', 30),
('IT-Solutions AG', 'IT-Solutions Aktiengesellschaft', 'billing@it-solutions.de', '+49 30 987654', 'Berlin', 30),
('Office-World', 'Office-World Handels GmbH', 'rechnung@office-world.de', '+49 69 555666', 'Frankfurt', 21);

INSERT INTO approval_rules (name, description, rule_type, priority, conditions, actions, created_by) VALUES
('Kleinstbeträge Auto-Freigabe', 'Automatische Freigabe für Beträge unter 50 EUR bei Büromaterial', 'automatic', 1, 
 '[{"field":"total_amount","operator":"<","value":50},{"field":"cost_center_id","operator":"=","value":"OFFICE","logicalOperator":"AND"}]',
 '[{"type":"auto_approve","value":"approved","description":"Automatisch freigeben und als bezahlt markieren"}]', 1),
 
('IT-Investitionen Freigabe', 'Manuelle Freigabe für IT-Kostenstelle oder Beträge über 500 EUR', 'manual', 2,
 '[{"field":"cost_center_id","operator":"=","value":"IT"},{"field":"total_amount","operator":">","value":500,"logicalOperator":"OR"}]',
 '[{"type":"require_approval","value":"manager","description":"Freigabe durch Kostenstellen-Manager erforderlich"}]', 1),

('Hohe Beträge Doppel-Freigabe', 'Doppelte Freigabe für Beträge über 5000 EUR', 'manual', 3,
 '[{"field":"total_amount","operator":">","value":5000}]',
 '[{"type":"require_approval","value":"double","description":"Freigabe durch Manager und Geschäftsführung erforderlich"}]', 1),

('Standard Freigabeprozess', 'Standard-Workflow für alle anderen Rechnungen', 'manual', 999,
 '[]',
 '[{"type":"require_approval","value":"standard","description":"Standard-Freigabeprozess durch zuständigen Manager"}]', 1);

INSERT INTO system_config (config_key, config_value, data_type, description) VALUES
('company_name', 'Musterfirma GmbH', 'string', 'Name der Firma'),
('default_currency', 'EUR', 'string', 'Standard-Währung'),
('auto_approval_limit', '50', 'number', 'Automatische Freigabe-Grenze in EUR'),
('payment_terms_days', '30', 'number', 'Standard-Zahlungsziel in Tagen'),
('notification_email_enabled', 'true', 'boolean', 'E-Mail-Benachrichtigungen aktiviert'),
('pdf_storage_path', '/uploads/invoices/', 'string', 'Pfad für PDF-Dateien'),
('max_file_size_mb', '10', 'number', 'Maximale Dateigröße in MB'),
('ad_integration_enabled', 'true', 'boolean', 'Active Directory Integration aktiviert'),
('backup_retention_days', '365', 'number', 'Aufbewahrungszeit für Backups in Tagen');

-- Beispiel-Rechnungen
INSERT INTO invoices (invoice_number, supplier_id, cost_center_id, project_id, net_amount, tax_amount, total_amount, 
                     invoice_date, due_date, description, status, created_by) VALUES
('MS-2024-001', 1, 'IT', 'WEB001', 840.34, 159.66, 1000.00, '2024-11-15', '2024-12-15', 
 'Microsoft Office 365 Lizenzen für 12 Monate', 'Freigabe_Erforderlich', 2),
 
('AMZ-2024-078', 2, 'OFFICE', 'OFF004', 42.02, 7.98, 50.00, '2024-11-20', '2024-12-04', 
 'Büromaterial: Druckerpapier, Stifte, Ordner', 'Eingegangen', 3),
 
('BE-2024-456', 3, 'OFFICE', NULL, 25.21, 4.79, 30.00, '2024-11-18', '2024-12-18', 
 'Reinigungsmittel für Büroküche', 'Freigegeben', 1),
 
('ITS-2024-789', 4, 'IT', 'WEB001', 4201.68, 798.32, 5000.00, '2024-11-10', '2024-12-10', 
 'Webentwicklung und Design Services', 'Freigabe_Erforderlich', 4),
 
('OW-2024-123', 5, 'HR', 'HR002', 168.07, 31.93, 200.00, '2024-11-22', '2024-12-13', 
 'Bürostühle für neue Mitarbeiter', 'Eingegangen', 3);

-- Beispiel-Freigabe-Workflows
INSERT INTO approval_workflows (invoice_id, rule_id, step_number, approver_id, approval_level, status) VALUES
(1, 2, 1, 4, 1, 'Pending'),    -- Microsoft Rechnung wartet auf IT-Manager Freigabe
(4, 3, 1, 4, 1, 'Pending'),    -- IT-Solutions Rechnung wartet auf Manager
(4, 3, 2, 1, 2, 'Pending');    -- IT-Solutions Rechnung wartet auch auf Admin (Doppel-Freigabe)

-- VIEW für Dashboard-Statistiken
CREATE VIEW v_dashboard_stats AS
SELECT 
    (SELECT COUNT(*) FROM invoices WHERE status = 'Eingegangen') as new_invoices,
    (SELECT COUNT(*) FROM invoices WHERE status = 'Freigabe_Erforderlich') as pending_approval,
    (SELECT COUNT(*) FROM invoices WHERE status = 'Freigegeben') as approved_invoices,
    (SELECT COUNT(*) FROM invoices WHERE status = 'Ueberfaellig') as overdue_invoices,
    (SELECT COALESCE(SUM(total_amount), 0) FROM invoices 
     WHERE status IN ('Freigegeben', 'Bezahlt') 
     AND MONTH(invoice_date) = MONTH(CURDATE()) 
     AND YEAR(invoice_date) = YEAR(CURDATE())) as monthly_approved_amount,
    (SELECT COALESCE(SUM(total_amount), 0) FROM invoices WHERE status = 'Freigabe_Erforderlich') as pending_approval_amount;

-- VIEW für erweiterte Rechnungsdetails
CREATE VIEW v_invoice_details AS
SELECT 
    i.id,
    i.invoice_number,
    i.total_amount,
    i.currency,
    i.status,
    i.invoice_date,
    i.due_date,
    i.description,
    s.name as supplier_name,
    s.email as supplier_email,
    cc.name as cost_center_name,
    cc.manager_id as cost_center_manager_id,
    p.name as project_name,
    po.title as purchase_order_title,
    CASE 
        WHEN i.due_date < CURDATE() AND i.status NOT IN ('Bezahlt', 'Storniert') 
        THEN TRUE
        ELSE FALSE
    END as is_overdue,
    DATEDIFF(CURDATE(), i.due_date) as days_overdue,
    CONCAT(creator.first_name, ' ', creator.last_name) as created_by_name,
    CONCAT(processor.first_name, ' ', processor.last_name) as processed_by_name
FROM invoices i
LEFT JOIN suppliers s ON i.supplier_id = s.id
LEFT JOIN cost_centers cc ON i.cost_center_id = cc.id
LEFT JOIN projects p ON i.project_id = p.id
LEFT JOIN purchase_orders po ON i.purchase_order_id = po.id
LEFT JOIN users creator ON i.created_by = creator.id
LEFT JOIN users processor ON i.processed_by = processor.id;

-- STORED PROCEDURE für automatische Regel-Auswertung
DELIMITER //

CREATE PROCEDURE sp_evaluate_approval_rules(IN p_invoice_id INT)
BEGIN
    DECLARE done INT DEFAULT FALSE;
    DECLARE v_rule_id INT;
    DECLARE v_rule_type VARCHAR(20);
    DECLARE v_conditions JSON;
    DECLARE v_actions JSON;
    DECLARE v_match_found BOOLEAN DEFAULT FALSE;
    
    -- Cursor für aktive Regeln (nach Priorität sortiert)
    DECLARE rule_cursor CURSOR FOR
        SELECT id, rule_type, conditions, actions
        FROM approval_rules 
        WHERE is_active = TRUE
        ORDER BY priority ASC;
    
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
    
    OPEN rule_cursor;
    read_loop: LOOP
        FETCH rule_cursor INTO v_rule_id, v_rule_type, v_conditions, v_actions;
        IF done THEN
            LEAVE read_loop;
        END IF;
        
        -- Hier würde die Regel-Engine die JSON-Bedingungen evaluieren
        -- Für dieses Beispiel vereinfacht dargestellt
        
        -- Bei Match: Workflow-Einträge erstellen
        -- (Die eigentliche Implementierung würde die JSON-Bedingungen parsen und evaluieren)
        
        IF v_match_found THEN
            LEAVE read_loop;
        END IF;
    END LOOP;
    
    CLOSE rule_cursor;
END//

-- TRIGGER für automatische Überfälligkeits-Prüfung
CREATE TRIGGER tr_update_overdue_status 
BEFORE UPDATE ON invoices
FOR EACH ROW
BEGIN
    IF NEW.due_date < CURDATE() AND NEW.status NOT IN ('Bezahlt', 'Storniert', 'Ueberfaellig') THEN
        SET NEW.status = 'Ueberfaellig';
    END IF;
END//

DELIMITER ;

-- Volltextsuche-Index für Rechnungen
ALTER TABLE invoices ADD FULLTEXT(description, internal_notes);
ALTER TABLE suppliers ADD FULLTEXT(name, legal_name);

-- Kommentare für erweiterte Funktionalitäten:
-- 1. Die JSON-Felder (conditions, actions, permissions, field_changes) werden nativ von MySQL 5.7+ unterstützt
-- 2. Für die Regel-Engine könnten MySQL JSON-Funktionen wie JSON_EXTRACT() verwendet werden
-- 3. File-Storage für PDFs über lokales Filesystem oder Cloud Storage
-- 4. Active Directory Integration über externe Libraries
-- 5. E-Mail-Benachrichtigungen über Queue-System (Redis, RabbitMQ)
-- 6. Für bessere Performance sollten Partitionierung und Read-Replicas implementiert werden
-- 7. Backup-Strategie mit mysqldump oder Binlog-Replikation