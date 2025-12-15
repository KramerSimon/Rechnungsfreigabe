-- Zuerst vorhandene Projekte prüfen
SELECT id, name, cost_center_id FROM projects ORDER BY id;

-- Und vorhandene Lieferanten
SELECT id, name FROM suppliers ORDER BY id;

-- Und Cost Centers
SELECT id, name FROM cost_centers ORDER BY id;
