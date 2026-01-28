-- MySQL dump 10.13  Distrib 8.0.40, for Win64 (x86_64)
--
-- Host: localhost    Database: rechnungsfreigabe
-- ------------------------------------------------------
-- Server version	8.0.40

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Current Database: `rechnungsfreigabe`
--

/*!40000 DROP DATABASE IF EXISTS `rechnungsfreigabe`*/;

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `rechnungsfreigabe` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `rechnungsfreigabe`;

--
-- Table structure for table `approval_rules`
--

DROP TABLE IF EXISTS `approval_rules`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `approval_rules` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `rule_type` enum('automatic','manual') COLLATE utf8mb4_unicode_ci NOT NULL,
  `priority` int NOT NULL DEFAULT '10',
  `is_active` tinyint(1) DEFAULT '1',
  `conditions` json NOT NULL,
  `actions` json NOT NULL,
  `created_by` int NOT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `created_by` (`created_by`),
  CONSTRAINT `approval_rules_ibfk_1` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `approval_rules`
--

LOCK TABLES `approval_rules` WRITE;
/*!40000 ALTER TABLE `approval_rules` DISABLE KEYS */;
INSERT INTO `approval_rules` VALUES (6,'Auto-Freigabe','','automatic',10,1,'[{\"field\": \"amount\", \"value\": \"500\", \"operator\": \"<=\"}]','[{\"type\": \"auto_approve\", \"value\": \"approved\", \"description\": \"Automatisch freigeben\"}]',1,'2025-12-18 14:27:26','2026-01-28 08:20:12'),(7,'Standard-Freigabe','','manual',999,1,'[{\"field\": \"amount\", \"value\": \"500\", \"operator\": \">\"}]','[{\"type\": \"assign_to\", \"value\": \"1\", \"description\": \"Zuweisen an Systemadministrator\"}]',1,'2025-12-20 11:48:34','2025-12-20 11:48:34'),(8,'Mehrstufige Freigabe','','manual',10,1,'[{\"field\": \"amount\", \"value\": \"1000\", \"operator\": \">\"}]','[{\"type\": \"require_approval\", \"value\": \"\", \"stages\": [{\"userId\": 5, \"stepNumber\": 1, \"approvalLevel\": 1}, {\"userId\": 1, \"stepNumber\": 2, \"approvalLevel\": 2}], \"description\": \"Mehrstufige Freigabe\"}]',1,'2025-12-21 12:45:11','2025-12-21 12:45:11'),(9,'Test','','automatic',10,1,'[{\"field\": \"costCenter\", \"value\": \"OFFICE\", \"operator\": \"=\"}]','[{\"type\": \"auto_approve\", \"value\": \"\", \"description\": \"Automatisch freigeben\"}]',1,'2026-01-07 09:48:50','2026-01-07 09:48:50');
/*!40000 ALTER TABLE `approval_rules` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `approval_workflows`
--

DROP TABLE IF EXISTS `approval_workflows`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `approval_workflows` (
  `id` int NOT NULL AUTO_INCREMENT,
  `invoice_id` int NOT NULL,
  `rule_id` int DEFAULT NULL,
  `step_number` int NOT NULL,
  `approver_id` int NOT NULL,
  `approval_level` int NOT NULL,
  `status_backup` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `status_id` int DEFAULT NULL,
  `comments` text COLLATE utf8mb4_unicode_ci,
  `approved_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `rule_id` (`rule_id`),
  KEY `idx_invoice_id` (`invoice_id`),
  KEY `idx_approver_id` (`approver_id`),
  KEY `idx_approval_workflows_status_id` (`status_id`),
  CONSTRAINT `approval_workflows_ibfk_1` FOREIGN KEY (`invoice_id`) REFERENCES `invoices` (`id`) ON DELETE CASCADE,
  CONSTRAINT `approval_workflows_ibfk_2` FOREIGN KEY (`rule_id`) REFERENCES `approval_rules` (`id`),
  CONSTRAINT `approval_workflows_ibfk_3` FOREIGN KEY (`approver_id`) REFERENCES `users` (`id`),
  CONSTRAINT `FK_approval_workflows_status` FOREIGN KEY (`status_id`) REFERENCES `statuses` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=72 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `approval_workflows`
--

LOCK TABLES `approval_workflows` WRITE;
/*!40000 ALTER TABLE `approval_workflows` DISABLE KEYS */;
/*!40000 ALTER TABLE `approval_workflows` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cost_centers`
--

DROP TABLE IF EXISTS `cost_centers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cost_centers` (
  `id` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `manager_id` int DEFAULT NULL,
  `budget` decimal(12,2) DEFAULT '0.00',
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `manager_id` (`manager_id`),
  CONSTRAINT `cost_centers_ibfk_1` FOREIGN KEY (`manager_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cost_centers`
--

LOCK TABLES `cost_centers` WRITE;
/*!40000 ALTER TABLE `cost_centers` DISABLE KEYS */;
INSERT INTO `cost_centers` VALUES ('FACILITY','Facility Management','Gebäude, Reinigung, Sicherheit',NULL,80000.00,1,'2025-12-12 09:31:16'),('FINANCE','Finanzen','Buchhaltung, Controlling und Finanzen',5,100000.00,1,'2025-12-12 09:31:16'),('FLEET','Fuhrpark','',NULL,100000.00,1,'2025-12-20 14:16:26'),('HR','Personalabteilung','Human Resources und Personalentwicklung',3,150000.00,1,'2025-12-12 09:31:16'),('IT','IT-Abteilung','Informationstechnologie und Digitalisierung',4,250000.00,1,'2025-12-12 09:31:16'),('OFFICE','Büromaterial','Allgemeine Büroausstattung und Verbrauchsmaterial',1,25000.00,1,'2025-12-12 09:31:16'),('SALES','Vertrieb','Verkauf und Marketing',2,300000.00,1,'2025-12-12 09:31:16');
/*!40000 ALTER TABLE `cost_centers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `escalation_logs`
--

DROP TABLE IF EXISTS `escalation_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `escalation_logs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `invoice_id` int NOT NULL,
  `escalation_rule_id` int NOT NULL,
  `recipient_user_id` int DEFAULT NULL,
  `sent_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `status` varchar(50) NOT NULL DEFAULT 'Sent',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_escalation_logs_user` (`recipient_user_id`),
  KEY `ix_escalation_logs_invoice` (`invoice_id`),
  KEY `ix_escalation_logs_rule` (`escalation_rule_id`),
  KEY `ix_escalation_logs_sent_at` (`sent_at`),
  CONSTRAINT `fk_escalation_logs_invoice` FOREIGN KEY (`invoice_id`) REFERENCES `invoices` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_escalation_logs_rule` FOREIGN KEY (`escalation_rule_id`) REFERENCES `escalation_rules` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_escalation_logs_user` FOREIGN KEY (`recipient_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `escalation_logs`
--

LOCK TABLES `escalation_logs` WRITE;
/*!40000 ALTER TABLE `escalation_logs` DISABLE KEYS */;
/*!40000 ALTER TABLE `escalation_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `escalation_rule_notify_roles`
--

DROP TABLE IF EXISTS `escalation_rule_notify_roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `escalation_rule_notify_roles` (
  `escalation_rule_id` int NOT NULL,
  `role_id` int NOT NULL,
  `added_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`escalation_rule_id`,`role_id`),
  KEY `fk_role_id` (`role_id`),
  CONSTRAINT `fk_rul_role_id` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_rul_rule_id` FOREIGN KEY (`escalation_rule_id`) REFERENCES `escalation_rules` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `escalation_rule_notify_roles`
--

LOCK TABLES `escalation_rule_notify_roles` WRITE;
/*!40000 ALTER TABLE `escalation_rule_notify_roles` DISABLE KEYS */;
/*!40000 ALTER TABLE `escalation_rule_notify_roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `escalation_rule_notify_users`
--

DROP TABLE IF EXISTS `escalation_rule_notify_users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `escalation_rule_notify_users` (
  `escalation_rule_id` int NOT NULL,
  `user_id` int NOT NULL,
  `added_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`escalation_rule_id`,`user_id`),
  KEY `fk_user_id` (`user_id`),
  CONSTRAINT `fk_ruu_rule_id` FOREIGN KEY (`escalation_rule_id`) REFERENCES `escalation_rules` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_ruu_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `escalation_rule_notify_users`
--

LOCK TABLES `escalation_rule_notify_users` WRITE;
/*!40000 ALTER TABLE `escalation_rule_notify_users` DISABLE KEYS */;
INSERT INTO `escalation_rule_notify_users` VALUES (3,1,'2026-01-28 09:32:21');
/*!40000 ALTER TABLE `escalation_rule_notify_users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `escalation_rule_trigger_statuses`
--

DROP TABLE IF EXISTS `escalation_rule_trigger_statuses`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `escalation_rule_trigger_statuses` (
  `escalation_rule_id` int NOT NULL,
  `status_id` int NOT NULL,
  `added_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`escalation_rule_id`,`status_id`),
  KEY `fk_status_id` (`status_id`),
  CONSTRAINT `fk_rule_id` FOREIGN KEY (`escalation_rule_id`) REFERENCES `escalation_rules` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_status_id` FOREIGN KEY (`status_id`) REFERENCES `statuses` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `escalation_rule_trigger_statuses`
--

LOCK TABLES `escalation_rule_trigger_statuses` WRITE;
/*!40000 ALTER TABLE `escalation_rule_trigger_statuses` DISABLE KEYS */;
INSERT INTO `escalation_rule_trigger_statuses` VALUES (3,1,'2026-01-28 09:32:21'),(3,2,'2026-01-28 09:32:21'),(3,3,'2026-01-28 09:32:21'),(3,4,'2026-01-28 09:32:21'),(3,5,'2026-01-28 09:32:21'),(3,6,'2026-01-28 09:32:21'),(3,7,'2026-01-28 09:32:21'),(3,8,'2026-01-28 09:32:21');
/*!40000 ALTER TABLE `escalation_rule_trigger_statuses` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `escalation_rules`
--

DROP TABLE IF EXISTS `escalation_rules`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `escalation_rules` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `trigger_after_minutes` int NOT NULL DEFAULT '2880' COMMENT 'Trigger time in minutes (default 48 hours = 2880 minutes)',
  `repeat_interval_hours` int DEFAULT NULL,
  `max_escalations` int DEFAULT '3',
  `message_template` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `ix_escalation_rules_active` (`is_active`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `escalation_rules`
--

LOCK TABLES `escalation_rules` WRITE;
/*!40000 ALTER TABLE `escalation_rules` DISABLE KEYS */;
INSERT INTO `escalation_rules` VALUES (3,'Test','',1,NULL,3,'',1,'2026-01-28 09:32:21','2026-01-28 09:32:21');
/*!40000 ALTER TABLE `escalation_rules` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `invoice_history`
--

DROP TABLE IF EXISTS `invoice_history`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `invoice_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `invoice_id` int NOT NULL,
  `action` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `action_type` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Manual',
  `action_source` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'User',
  `old_status` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `new_status` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `field_changes` json DEFAULT NULL,
  `comments` text COLLATE utf8mb4_unicode_ci,
  `policy_reference` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `system_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `import_channel` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `changed_by` int DEFAULT NULL,
  `changed_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `changed_by` (`changed_by`),
  KEY `idx_invoice_id` (`invoice_id`),
  KEY `idx_changed_at` (`changed_at`),
  KEY `idx_action_type` (`action_type`),
  KEY `idx_action_source` (`action_source`),
  CONSTRAINT `invoice_history_ibfk_1` FOREIGN KEY (`invoice_id`) REFERENCES `invoices` (`id`) ON DELETE CASCADE,
  CONSTRAINT `invoice_history_ibfk_2` FOREIGN KEY (`changed_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=151 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invoice_history`
--

LOCK TABLES `invoice_history` WRITE;
/*!40000 ALTER TABLE `invoice_history` DISABLE KEYS */;
/*!40000 ALTER TABLE `invoice_history` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `invoices`
--

DROP TABLE IF EXISTS `invoices`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `invoices` (
  `id` int NOT NULL AUTO_INCREMENT,
  `invoice_number` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `supplier_id` int NOT NULL,
  `purchase_order_id` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `cost_center_id` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `project_id` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `net_amount` decimal(12,2) NOT NULL,
  `tax_amount` decimal(12,2) NOT NULL DEFAULT '0.00',
  `total_amount` decimal(12,2) NOT NULL,
  `currency` varchar(3) COLLATE utf8mb4_unicode_ci DEFAULT 'EUR',
  `invoice_date` date NOT NULL,
  `due_date` date NOT NULL,
  `received_date` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `status_backup` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `status_id` int DEFAULT NULL,
  `requires_approval` tinyint(1) DEFAULT '1',
  `approval_level` int DEFAULT '1',
  `auto_approved` tinyint(1) DEFAULT '0',
  `pdf_file_path` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `pdf_content` longblob COMMENT 'PDF-Dateiinhalt als BLOB',
  `pdf_file_size` bigint DEFAULT NULL,
  `original_filename` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `internal_notes` text COLLATE utf8mb4_unicode_ci,
  `created_by` int DEFAULT NULL,
  `processed_by` int DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `purchase_order_id` (`purchase_order_id`),
  KEY `created_by` (`created_by`),
  KEY `processed_by` (`processed_by`),
  KEY `idx_invoice_number` (`invoice_number`),
  KEY `idx_supplier_id` (`supplier_id`),
  KEY `idx_cost_center_id` (`cost_center_id`),
  KEY `idx_project_id` (`project_id`),
  KEY `idx_invoice_date` (`invoice_date`),
  KEY `idx_due_date` (`due_date`),
  KEY `idx_received_date` (`received_date`),
  KEY `idx_pdf_content` (`pdf_file_size`),
  KEY `idx_invoices_status_id` (`status_id`),
  FULLTEXT KEY `description` (`description`,`internal_notes`),
  CONSTRAINT `FK_invoices_status` FOREIGN KEY (`status_id`) REFERENCES `statuses` (`id`),
  CONSTRAINT `invoices_ibfk_1` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`),
  CONSTRAINT `invoices_ibfk_2` FOREIGN KEY (`purchase_order_id`) REFERENCES `purchase_orders` (`id`),
  CONSTRAINT `invoices_ibfk_3` FOREIGN KEY (`cost_center_id`) REFERENCES `cost_centers` (`id`),
  CONSTRAINT `invoices_ibfk_4` FOREIGN KEY (`project_id`) REFERENCES `projects` (`id`),
  CONSTRAINT `invoices_ibfk_5` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`),
  CONSTRAINT `invoices_ibfk_6` FOREIGN KEY (`processed_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=120 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invoices`
--

LOCK TABLES `invoices` WRITE;
/*!40000 ALTER TABLE `invoices` DISABLE KEYS */;
/*!40000 ALTER TABLE `invoices` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `tr_update_overdue_status` BEFORE UPDATE ON `invoices` FOR EACH ROW BEGIN
    DECLARE overdue_status_id INT;
    DECLARE paid_status_id INT;
    DECLARE cancelled_status_id INT;
    
    SELECT id INTO overdue_status_id FROM statuses WHERE code = 'Ueberfaellig' AND entity_type = 'Invoice' LIMIT 1;
    SELECT id INTO paid_status_id FROM statuses WHERE code = 'Bezahlt' AND entity_type = 'Invoice' LIMIT 1;
    SELECT id INTO cancelled_status_id FROM statuses WHERE code = 'Storniert' AND entity_type = 'Invoice' LIMIT 1;
    
    IF NEW.due_date < CURDATE() AND NEW.status_id NOT IN (paid_status_id, cancelled_status_id, overdue_status_id) THEN
        SET NEW.status_id = overdue_status_id;
        SET NEW.status_backup = 'Ueberfaellig';
    END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `notifications`
--

DROP TABLE IF EXISTS `notifications`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `notifications` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `invoice_id` int DEFAULT NULL,
  `type` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `title` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `message` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `is_read` tinyint(1) DEFAULT '0',
  `priority` enum('low','normal','high','urgent') COLLATE utf8mb4_unicode_ci DEFAULT 'normal',
  `action_url` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `read_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `invoice_id` (`invoice_id`),
  KEY `idx_user_id` (`user_id`),
  KEY `idx_is_read` (`is_read`),
  CONSTRAINT `notifications_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `notifications_ibfk_2` FOREIGN KEY (`invoice_id`) REFERENCES `invoices` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=224 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `notifications`
--

LOCK TABLES `notifications` WRITE;
/*!40000 ALTER TABLE `notifications` DISABLE KEYS */;
/*!40000 ALTER TABLE `notifications` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `permissions`
--

DROP TABLE IF EXISTS `permissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `permissions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` longtext COLLATE utf8mb4_unicode_ci,
  `code` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `category` longtext COLLATE utf8mb4_unicode_ci,
  `is_system_permission` tinyint(1) NOT NULL DEFAULT '0',
  `created_at` datetime(6) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UK_permission_code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `permissions`
--

LOCK TABLES `permissions` WRITE;
/*!40000 ALTER TABLE `permissions` DISABLE KEYS */;
INSERT INTO `permissions` VALUES (1,'Alle Rechnungen anzeigen',NULL,'invoices.view_all','invoices',1,'2026-01-26 13:01:41.000000'),(2,'Rechnung erstellen',NULL,'invoices.create','invoices',1,'2026-01-26 13:01:41.000000'),(3,'Rechnung bearbeiten',NULL,'invoices.edit','invoices',1,'2026-01-26 13:01:41.000000'),(4,'Rechnung freigeben',NULL,'invoices.approve','invoices',1,'2026-01-26 13:01:41.000000'),(5,'Eigene Rechnungen anzeigen',NULL,'invoices.view_own','invoices',0,'2026-01-26 13:01:41.000000'),(6,'Zahlungen verarbeiten',NULL,'payments.process','payments',1,'2026-01-26 13:01:41.000000'),(7,'Reports anzeigen',NULL,'reports.view','reports',1,'2026-01-26 13:01:41.000000'),(8,'Team-Rechnungen anzeigen',NULL,'invoices.view_team','invoices',0,'2026-01-26 13:01:41.000000'),(9,'Kostenstellen-Rechnungen freigeben',NULL,'invoices.approve_cost_center','invoices',0,'2026-01-26 13:01:41.000000'),(10,'Benutzer verwalten',NULL,'users.manage','users',1,'2026-01-26 13:01:41.000000'),(11,'Benutzer bearbeiten',NULL,'users.edit','users',1,'2026-01-26 13:01:41.000000'),(12,'Benutzer anzeigen',NULL,'users.view','users',1,'2026-01-26 13:01:41.000000'),(13,'Rollen verwalten',NULL,'roles.manage','roles',1,'2026-01-26 13:01:41.000000'),(14,'Berechtigungen verwalten',NULL,'permissions.manage','permissions',1,'2026-01-26 13:01:41.000000'),(15,'Dashboard anzeigen',NULL,'dashboard.view','dashboard',1,'2026-01-26 13:01:41.000000'),(16,'Daten exportieren',NULL,'export.data','export',0,'2026-01-26 13:01:41.000000');
/*!40000 ALTER TABLE `permissions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `projects`
--

DROP TABLE IF EXISTS `projects`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `projects` (
  `id` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `cost_center_id` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `budget` decimal(12,2) DEFAULT '0.00',
  `spent_amount` decimal(12,2) DEFAULT '0.00',
  `status_backup` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `status_id` int DEFAULT NULL,
  `start_date` date DEFAULT NULL,
  `end_date` date DEFAULT NULL,
  `project_manager_id` int DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `cost_center_id` (`cost_center_id`),
  KEY `project_manager_id` (`project_manager_id`),
  KEY `idx_projects_status_id` (`status_id`),
  CONSTRAINT `FK_projects_status` FOREIGN KEY (`status_id`) REFERENCES `statuses` (`id`),
  CONSTRAINT `projects_ibfk_1` FOREIGN KEY (`cost_center_id`) REFERENCES `cost_centers` (`id`),
  CONSTRAINT `projects_ibfk_2` FOREIGN KEY (`project_manager_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `projects`
--

LOCK TABLES `projects` WRITE;
/*!40000 ALTER TABLE `projects` DISABLE KEYS */;
INSERT INTO `projects` VALUES ('10000','Fuhrpark-Reparaturen','','FLEET',0.00,0.00,'Aktiv',14,NULL,NULL,NULL,'2025-12-20 14:24:13'),('HR002','Mitarbeiter-Portal','Entwicklung eines Self-Service Portals für Mitarbeiter','HR',15000.00,0.00,'Geplant',13,NULL,NULL,3,'2025-12-12 09:31:17'),('OFF004','Büroausstattung 2024','Modernisierung der Büroausstattung','OFFICE',5000.00,0.00,'Aktiv',14,NULL,NULL,1,'2025-12-12 09:31:17'),('SALES003','CRM System','Einführung eines neuen Customer Relationship Management Systems','SALES',40000.00,0.00,'Aktiv',14,NULL,NULL,2,'2025-12-12 09:31:17'),('WEB001','Website Relaunch','Neugestaltung der Unternehmenswebsite','IT',25000.00,0.00,'Aktiv',14,NULL,NULL,4,'2025-12-12 09:31:17');
/*!40000 ALTER TABLE `projects` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `purchase_orders`
--

DROP TABLE IF EXISTS `purchase_orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `purchase_orders` (
  `id` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `title` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `supplier_id` int DEFAULT NULL,
  `cost_center_id` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `project_id` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `total_amount` decimal(12,2) NOT NULL,
  `currency` varchar(3) COLLATE utf8mb4_unicode_ci DEFAULT 'EUR',
  `status_backup` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `status_id` int DEFAULT NULL,
  `created_by` int NOT NULL,
  `approved_by` int DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `approved_at` timestamp NULL DEFAULT NULL,
  `pdf_content` longblob COMMENT 'PDF file content as BLOB',
  `pdf_file_size` bigint DEFAULT NULL,
  `original_filename` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `cost_center_id` (`cost_center_id`),
  KEY `project_id` (`project_id`),
  KEY `created_by` (`created_by`),
  KEY `approved_by` (`approved_by`),
  KEY `idx_supplier_id` (`supplier_id`),
  KEY `idx_pdf_file_size` (`pdf_file_size`),
  KEY `idx_purchase_orders_status_id` (`status_id`),
  CONSTRAINT `FK_purchase_orders_status` FOREIGN KEY (`status_id`) REFERENCES `statuses` (`id`),
  CONSTRAINT `purchase_orders_ibfk_1` FOREIGN KEY (`cost_center_id`) REFERENCES `cost_centers` (`id`),
  CONSTRAINT `purchase_orders_ibfk_2` FOREIGN KEY (`project_id`) REFERENCES `projects` (`id`),
  CONSTRAINT `purchase_orders_ibfk_3` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`),
  CONSTRAINT `purchase_orders_ibfk_4` FOREIGN KEY (`approved_by`) REFERENCES `users` (`id`),
  CONSTRAINT `purchase_orders_ibfk_5` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `purchase_orders`
--

LOCK TABLES `purchase_orders` WRITE;
/*!40000 ALTER TABLE `purchase_orders` DISABLE KEYS */;
/*!40000 ALTER TABLE `purchase_orders` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `role_permissions`
--

DROP TABLE IF EXISTS `role_permissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `role_permissions` (
  `role_id` int NOT NULL,
  `permission_id` int NOT NULL,
  `assigned_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`role_id`,`permission_id`),
  KEY `FK_role_permissions_permission_id` (`permission_id`),
  CONSTRAINT `FK_role_permissions_permission_id` FOREIGN KEY (`permission_id`) REFERENCES `permissions` (`id`) ON DELETE CASCADE,
  CONSTRAINT `FK_role_permissions_role_id` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `role_permissions`
--

LOCK TABLES `role_permissions` WRITE;
/*!40000 ALTER TABLE `role_permissions` DISABLE KEYS */;
INSERT INTO `role_permissions` VALUES (1,1,'2026-01-26 13:01:41.000000'),(1,2,'2026-01-26 13:01:41.000000'),(1,3,'2026-01-26 13:01:41.000000'),(1,4,'2026-01-26 13:01:41.000000'),(1,5,'2026-01-26 13:01:41.000000'),(1,6,'2026-01-26 13:01:41.000000'),(1,7,'2026-01-26 13:01:41.000000'),(1,8,'2026-01-26 13:01:41.000000'),(1,9,'2026-01-26 13:01:41.000000'),(1,10,'2026-01-26 13:01:41.000000'),(1,11,'2026-01-26 13:01:41.000000'),(1,12,'2026-01-26 13:01:41.000000'),(1,13,'2026-01-26 13:01:41.000000'),(1,14,'2026-01-26 13:01:41.000000'),(1,15,'2026-01-26 13:01:41.000000'),(1,16,'2026-01-26 13:01:41.000000'),(2,4,'2026-01-27 07:59:37.252031'),(2,5,'2026-01-27 07:59:37.251661'),(2,9,'2026-01-27 07:59:37.252006'),(2,15,'2026-01-27 07:59:37.240670'),(3,1,'2026-01-27 08:36:22.096097'),(3,3,'2026-01-27 08:36:22.106131'),(3,4,'2026-01-27 08:36:22.106474'),(3,5,'2026-01-27 08:36:22.106531'),(3,6,'2026-01-27 08:36:22.106558'),(3,7,'2026-01-27 08:36:22.106580'),(3,9,'2026-01-27 08:36:22.106618'),(3,15,'2026-01-27 08:36:22.106637'),(3,16,'2026-01-27 08:36:22.106656'),(4,2,'2026-01-26 13:01:41.000000'),(4,5,'2026-01-26 13:01:41.000000'),(4,15,'2026-01-26 13:01:41.000000'),(5,1,'2026-01-27 08:37:00.166521'),(5,7,'2026-01-27 08:37:00.166643'),(5,15,'2026-01-27 08:37:00.166656'),(6,1,'2026-01-26 13:01:41.000000'),(6,8,'2026-01-26 13:01:41.000000'),(6,9,'2026-01-26 13:01:41.000000'),(6,15,'2026-01-26 13:01:41.000000');
/*!40000 ALTER TABLE `role_permissions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `roles`
--

DROP TABLE IF EXISTS `roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text COLLATE utf8mb4_unicode_ci,
  `color` varchar(7) COLLATE utf8mb4_unicode_ci DEFAULT '#ff9800',
  `is_system_role` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roles`
--

LOCK TABLES `roles` WRITE;
/*!40000 ALTER TABLE `roles` DISABLE KEYS */;
INSERT INTO `roles` VALUES (1,'Administrator','System Administrator mit allen Rechten','#F44336',1),(2,'Freigeber','Kann Rechnungen freigeben und ablehnen','#00bfff',0),(3,'Buchhaltung','Kann alle Rechnungen einsehen und bearbeiten','#4CAF50',1),(4,'Mitarbeiter','Kann eigene Rechnungen einsehen','#795548',0),(5,'Controller','Kann Reports erstellen und Budgets überwachen','#fbff00',0),(6,'Manager','Kann Kostenstellen-bezogene Rechnungen freigeben','#FF9800',0);
/*!40000 ALTER TABLE `roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `statuses`
--

DROP TABLE IF EXISTS `statuses`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `statuses` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `display_name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `entity_type` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Invoice, PurchaseOrder, Project, ApprovalWorkflow',
  `sort_order` int NOT NULL DEFAULT '0',
  `is_active` tinyint(1) DEFAULT '1',
  `color` varchar(7) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT '#808080' COMMENT 'Hex color code for UI display',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UK_status_code_entity` (`code`,`entity_type`),
  KEY `idx_entity_type` (`entity_type`),
  KEY `idx_is_active` (`is_active`)
) ENGINE=InnoDB AUTO_INCREMENT=23 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `statuses`
--

LOCK TABLES `statuses` WRITE;
/*!40000 ALTER TABLE `statuses` DISABLE KEYS */;
INSERT INTO `statuses` VALUES (1,'Received','Received','Invoice has been received and awaits review','Invoice',1,1,'#2196F3','2026-01-27 10:00:02'),(2,'Under_Review','Under Review','Invoice is currently being reviewed','Invoice',2,1,'#FF9800','2026-01-27 10:00:02'),(3,'Approval_Required','Approval Required','Invoice requires approval','Invoice',3,1,'#FFC107','2026-01-27 10:00:02'),(4,'Approved','Approved','Invoice has been approved','Invoice',4,1,'#4CAF50','2026-01-27 10:00:02'),(5,'Rejected','Rejected','Invoice has been rejected','Invoice',5,1,'#F44336','2026-01-27 10:00:02'),(6,'Paid','Paid','Invoice has been paid','Invoice',6,1,'#8BC34A','2026-01-27 10:00:02'),(7,'Overdue','Overdue','Payment is overdue','Invoice',7,1,'#E91E63','2026-01-27 10:00:02'),(8,'Cancelled','Cancelled','Invoice has been cancelled','Invoice',8,1,'#9E9E9E','2026-01-27 10:00:02'),(9,'Open','Open','Purchase order is open','PurchaseOrder',1,1,'#2196F3','2026-01-27 10:00:02'),(10,'Partially_Fulfilled','Partially Fulfilled','Purchase order has been partially fulfilled','PurchaseOrder',2,1,'#FF9800','2026-01-27 10:00:02'),(11,'Fulfilled','Fulfilled','Purchase order has been fully fulfilled','PurchaseOrder',3,1,'#4CAF50','2026-01-27 10:00:02'),(12,'Cancelled','Cancelled','Purchase order has been cancelled','PurchaseOrder',4,1,'#9E9E9E','2026-01-27 10:00:02'),(13,'Planned','Planned','Project is in planning','Project',1,1,'#2196F3','2026-01-27 10:00:02'),(14,'Active','Active','Project is active','Project',2,1,'#4CAF50','2026-01-27 10:00:02'),(15,'On_Hold','On Hold','Project is on hold','Project',3,1,'#FF9800','2026-01-27 10:00:02'),(16,'Completed','Completed','Project has been completed','Project',4,1,'#8BC34A','2026-01-27 10:00:02'),(17,'Cancelled','Cancelled','Project has been cancelled','Project',5,1,'#F44336','2026-01-27 10:00:02'),(18,'Pending','Pending','Approval is pending','ApprovalWorkflow',1,1,'#FFC107','2026-01-27 10:00:02'),(19,'Approved','Approved','Approval has been granted','ApprovalWorkflow',2,1,'#4CAF50','2026-01-27 10:00:02'),(20,'Rejected','Rejected','Approval has been rejected','ApprovalWorkflow',3,1,'#F44336','2026-01-27 10:00:02'),(21,'Skipped','Skipped','Approval step was skipped','ApprovalWorkflow',4,1,'#9E9E9E','2026-01-27 10:00:02'),(22,'Waiting','Waiting','Waiting for previous approval','ApprovalWorkflow',5,1,'#03A9F4','2026-01-27 10:00:02');
/*!40000 ALTER TABLE `statuses` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `suppliers`
--

DROP TABLE IF EXISTS `suppliers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `suppliers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `legal_name` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `tax_number` varchar(30) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `vat_number` varchar(30) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `address_line1` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `address_line2` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `postal_code` varchar(10) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `city` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `country` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT 'Deutschland',
  `email` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `phone` varchar(30) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `bank_name` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `iban` varchar(34) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `bic` varchar(11) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `payment_terms_days` int DEFAULT '30',
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  FULLTEXT KEY `name` (`name`,`legal_name`)
) ENGINE=InnoDB AUTO_INCREMENT=62 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `suppliers`
--

LOCK TABLES `suppliers` WRITE;
/*!40000 ALTER TABLE `suppliers` DISABLE KEYS */;
INSERT INTO `suppliers` VALUES (61,'Azienda Demo SRL','Azienda Demo SRL','IT01234567890','IT01234567890','Via Roma 1 00100',NULL,'00100','Roma','Italien',NULL,NULL,NULL,NULL,NULL,30,1,'2026-01-22 13:17:14','2026-01-22 13:17:14');
/*!40000 ALTER TABLE `suppliers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `system_config`
--

DROP TABLE IF EXISTS `system_config`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `system_config` (
  `id` int NOT NULL AUTO_INCREMENT,
  `config_key` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `config_value` text COLLATE utf8mb4_unicode_ci,
  `data_type` enum('string','number','boolean','json') COLLATE utf8mb4_unicode_ci DEFAULT 'string',
  `description` text COLLATE utf8mb4_unicode_ci,
  `is_editable` tinyint(1) DEFAULT '1',
  `updated_by` int DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `config_key` (`config_key`),
  KEY `updated_by` (`updated_by`),
  CONSTRAINT `system_config_ibfk_1` FOREIGN KEY (`updated_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `system_config`
--

LOCK TABLES `system_config` WRITE;
/*!40000 ALTER TABLE `system_config` DISABLE KEYS */;
INSERT INTO `system_config` VALUES (1,'company_name','Musterfirma GmbH','string','Name der Firma',1,NULL,'2025-12-12 09:31:17'),(2,'default_currency','EUR','string','Standard-Währung',1,NULL,'2025-12-12 09:31:17'),(3,'auto_approval_limit','50','number','Automatische Freigabe-Grenze in EUR',1,NULL,'2025-12-12 09:31:17'),(4,'payment_terms_days','30','number','Standard-Zahlungsziel in Tagen',1,NULL,'2025-12-12 09:31:17'),(5,'notification_email_enabled','true','boolean','E-Mail-Benachrichtigungen aktiviert',1,NULL,'2025-12-12 09:31:17'),(6,'pdf_storage_path','/uploads/invoices/','string','Pfad für PDF-Dateien',1,NULL,'2025-12-12 09:31:17'),(7,'max_file_size_mb','10','number','Maximale Dateigröße in MB',1,NULL,'2025-12-12 09:31:17'),(8,'ad_integration_enabled','true','boolean','Active Directory Integration aktiviert',1,NULL,'2025-12-12 09:31:17'),(9,'backup_retention_days','365','number','Aufbewahrungszeit für Backups in Tagen',1,NULL,'2025-12-12 09:31:17');
/*!40000 ALTER TABLE `system_config` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_roles`
--

DROP TABLE IF EXISTS `user_roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_roles` (
  `user_id` int NOT NULL,
  `role_id` int NOT NULL,
  `assigned_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`user_id`,`role_id`),
  KEY `role_id` (`role_id`),
  CONSTRAINT `user_roles_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `user_roles_ibfk_2` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_roles`
--

LOCK TABLES `user_roles` WRITE;
/*!40000 ALTER TABLE `user_roles` DISABLE KEYS */;
INSERT INTO `user_roles` VALUES (1,1,'2026-01-27 08:16:20'),(2,2,'2026-01-27 07:57:16'),(3,3,'2025-12-12 09:31:16'),(4,6,'2025-12-12 09:31:16'),(5,2,'2025-12-21 15:18:51'),(5,4,'2025-12-21 15:18:51');
/*!40000 ALTER TABLE `user_roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `password_hash` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `email` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `first_name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `last_name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `active_directory_sid` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `password_changed_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `failed_login_attempts` int DEFAULT '0',
  `locked_until` timestamp NULL DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `last_login` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `username` (`username`),
  UNIQUE KEY `email` (`email`),
  UNIQUE KEY `active_directory_sid` (`active_directory_sid`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'admin','$2a$11$OsB0yW6RMM/44lEKraS1gOdLpCqz.s6j2oIQH1x6/x0VOTbi3ly5.','simon.kramer05@gmail.com','System','Administrator',NULL,'2025-12-12 09:31:16',0,NULL,1,'2025-12-12 09:31:16','2026-01-28 09:31:02','2026-01-28 09:31:02'),(2,'max.mustermann','$2a$11$mAAO7nsfAEm8DkyXs.2Bmey1OYdC7pNza3wFjyZuF/MTtEPriiuFO','max.mustermann@firma.de','Max','Mustermann',NULL,'2025-12-12 09:31:16',0,NULL,1,'2025-12-12 09:31:16','2026-01-27 07:57:16',NULL),(3,'maria.mueller','$2a$11$eGjbz1.YRSQ5tcJWXwmeZu5S/cCDqg2PUG68XthzP5NwOiG61oaRK','maria.mueller@firma.de','Maria','Müller',NULL,'2025-12-12 09:31:16',0,NULL,1,'2025-12-12 09:31:16','2025-12-12 08:39:48',NULL),(4,'hans.schmidt','$2a$11$0.kYmWmgLz8OcAou3aysd.snnN/IqV3u/b9n3EtqX.gVe0OhPIL8W','hans.schmidt@firma.de','Hans','Schmidt',NULL,'2025-12-12 09:31:16',0,NULL,1,'2025-12-12 09:31:16','2025-12-12 08:39:48',NULL),(5,'lisa.klein','$2a$11$eIKzA9xWOS.XpIv3P83lOu7wLQNEJeCjYO2LFd18BLrtx0QIdTv.6','lisa.klein@firma.de','Lisa','Klein',NULL,'2025-12-12 09:31:16',0,NULL,1,'2025-12-12 09:31:16','2026-01-21 13:36:17',NULL);
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping events for database 'rechnungsfreigabe'
--

--
-- Dumping routines for database 'rechnungsfreigabe'
--
/*!50003 DROP PROCEDURE IF EXISTS `sp_evaluate_approval_rules` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_evaluate_approval_rules`(IN p_invoice_id INT)
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
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-01-28 11:32:50
