-- Basic data initialization for Rechnungsfreigabe
USE rechnungsfreigabe;

-- Delete existing data in correct order
DELETE FROM user_roles;
DELETE FROM roles;
DELETE FROM users;
DELETE FROM cost_centers;

-- Insert Roles
INSERT INTO roles (id, name, description, permissions) VALUES
(1, 'Administrator', 'Vollzugriff auf alle Funktionen', '["all"]'),
(2, 'Freigeber', 'Kann Rechnungen freigeben', '["approve_invoices", "view_all_invoices"]'),
(3, 'Buchhaltung', 'Buchhaltungsfunktionen', '["view_all_invoices", "process_payments", "view_reports"]'),
(4, 'Sachbearbeiter', 'Grundlegende Rechnungserfassung', '["create_invoices", "view_own_invoices"]'),
(5, 'Controller', 'Kann Reports einsehen', '["view_reports", "view_all_invoices"]'),
(6, 'Manager', 'Kann Team-Rechnungen verwalten', '["approve_cost_center_invoices", "view_team_invoices"]');

-- Insert Users (all with password "Password123!")
-- Password hash for "Password123!" with BCrypt
INSERT INTO users (id, username, password_hash, email, first_name, last_name, is_active) VALUES
(1, 'admin', '$2a$11$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'admin@example.com', 'System', 'Administrator', TRUE),
(2, 'max.mustermann', '$2a$11$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'max.mustermann@example.com', 'Max', 'Mustermann', TRUE),
(3, 'maria.mueller', '$2a$11$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'maria.mueller@example.com', 'Maria', 'Müller', TRUE),
(4, 'hans.schmidt', '$2a$11$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'hans.schmidt@example.com', 'Hans', 'Schmidt', TRUE),
(5, 'lisa.klein', '$2a$11$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'lisa.klein@example.com', 'Lisa', 'Klein', TRUE);

-- Assign roles to users
INSERT INTO user_roles (user_id, role_id) VALUES
(1, 1), -- admin -> Administrator
(2, 2), -- max.mustermann -> Freigeber
(3, 3), -- maria.mueller -> Buchhaltung
(4, 6), -- hans.schmidt -> Manager
(5, 5); -- lisa.klein -> Controller

-- Insert Cost Centers
INSERT INTO cost_centers (id, name, description, budget, is_active) VALUES
('IT', 'IT & Development', 'IT-Abteilung und Softwareentwicklung', 500000.00, TRUE),
('OFFICE', 'Office & Administration', 'Büroverwaltung und allgemeine Kosten', 100000.00, TRUE),
('HR', 'Human Resources', 'Personalabteilung', 200000.00, TRUE),
('ADMIN', 'Administration', 'Verwaltung', 150000.00, TRUE),
('SALES', 'Sales & Marketing', 'Vertrieb und Marketing', 300000.00, TRUE),
('FINANCE', 'Finance & Controlling', 'Finanzabteilung', 250000.00, TRUE);

-- Set managers for cost centers
UPDATE cost_centers SET manager_id = 4 WHERE id = 'IT';
UPDATE cost_centers SET manager_id = 3 WHERE id = 'HR';
UPDATE cost_centers SET manager_id = 2 WHERE id = 'SALES';
UPDATE cost_centers SET manager_id = 5 WHERE id = 'FINANCE';
UPDATE cost_centers SET manager_id = 1 WHERE id = 'OFFICE';

SELECT 'Database initialized successfully!' as message;
SELECT COUNT(*) as user_count FROM users;
SELECT COUNT(*) as role_count FROM roles;
SELECT COUNT(*) as cost_center_count FROM cost_centers;
