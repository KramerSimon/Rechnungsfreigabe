CREATE DATABASE  IF NOT EXISTS `rechnungsfreigabe` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `rechnungsfreigabe`;
-- MySQL dump 10.13  Distrib 8.0.40, for Win64 (x86_64)
--
-- Host: localhost    Database: rechnungsfreigabe
-- ------------------------------------------------------
-- Server version	8.0.40

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

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
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `approval_rules`
--

LOCK TABLES `approval_rules` WRITE;
/*!40000 ALTER TABLE `approval_rules` DISABLE KEYS */;
INSERT INTO `approval_rules` VALUES (1,'Kleinstbeträge Auto-Freigabe','Automatische Freigabe für Beträge unter 50 EUR bei Büromaterial','automatic',1,1,'[{\"field\": \"total_amount\", \"value\": 50, \"operator\": \"<\"}, {\"field\": \"cost_center_id\", \"value\": \"OFFICE\", \"operator\": \"=\", \"logicalOperator\": \"AND\"}]','[{\"type\": \"auto_approve\", \"value\": \"approved\", \"description\": \"Automatisch freigeben und als bezahlt markieren\"}]',1,'2025-12-11 17:02:47','2025-12-11 17:02:47'),(2,'IT-Investitionen Freigabe','Manuelle Freigabe für IT-Kostenstelle oder Beträge über 500 EUR','manual',2,1,'[{\"field\": \"cost_center_id\", \"value\": \"IT\", \"operator\": \"=\"}, {\"field\": \"total_amount\", \"value\": 500, \"operator\": \">\", \"logicalOperator\": \"OR\"}]','[{\"type\": \"require_approval\", \"value\": \"manager\", \"description\": \"Freigabe durch Kostenstellen-Manager erforderlich\"}]',1,'2025-12-11 17:02:47','2025-12-11 17:02:47'),(3,'Hohe Beträge Doppel-Freigabe','Doppelte Freigabe für Beträge über 5000 EUR','manual',3,1,'[{\"field\": \"total_amount\", \"value\": 5000, \"operator\": \">\"}]','[{\"type\": \"require_approval\", \"value\": \"double\", \"description\": \"Freigabe durch Manager und Geschäftsführung erforderlich\"}]',1,'2025-12-11 17:02:47','2025-12-11 17:02:47'),(4,'Standard Freigabeprozess','Standard-Workflow für alle anderen Rechnungen','manual',999,1,'[]','[{\"type\": \"require_approval\", \"value\": \"standard\", \"description\": \"Standard-Freigabeprozess durch zuständigen Manager\"}]',1,'2025-12-11 17:02:47','2025-12-11 17:02:47');
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
  `status` enum('Pending','Approved','Rejected','Skipped') COLLATE utf8mb4_unicode_ci DEFAULT 'Pending',
  `comments` text COLLATE utf8mb4_unicode_ci,
  `approved_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `rule_id` (`rule_id`),
  KEY `idx_invoice_id` (`invoice_id`),
  KEY `idx_approver_id` (`approver_id`),
  KEY `idx_status` (`status`),
  CONSTRAINT `approval_workflows_ibfk_1` FOREIGN KEY (`invoice_id`) REFERENCES `invoices` (`id`) ON DELETE CASCADE,
  CONSTRAINT `approval_workflows_ibfk_2` FOREIGN KEY (`rule_id`) REFERENCES `approval_rules` (`id`),
  CONSTRAINT `approval_workflows_ibfk_3` FOREIGN KEY (`approver_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `approval_workflows`
--

LOCK TABLES `approval_workflows` WRITE;
/*!40000 ALTER TABLE `approval_workflows` DISABLE KEYS */;
INSERT INTO `approval_workflows` VALUES (10,13,NULL,1,1,1,'Approved','Freigabe erteilt','2025-12-15 13:38:31','2025-12-15 13:47:40'),(11,14,NULL,1,1,1,'Rejected','Kacke','2025-12-15 13:47:22','2025-12-15 13:47:40'),(12,15,NULL,1,1,1,'Approved','Freigabe erteilt','2025-12-15 13:47:47','2025-12-15 13:47:40'),(13,16,NULL,1,1,1,'Approved','Admin-Freigabe: Freigabe erteilt','2025-12-15 17:06:52','2025-12-15 13:47:40');
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
INSERT INTO `cost_centers` VALUES ('ADMIN','Administration',NULL,NULL,150000.00,1,'2025-12-15 12:20:12'),('FACILITY','Facility Management','Gebäude, Reinigung, Sicherheit',NULL,80000.00,1,'2025-12-11 17:02:47'),('FINANCE','Finanzen','Buchhaltung, Controlling und Finanzen',5,100000.00,1,'2025-12-11 17:02:47'),('HR','Personalabteilung','Human Resources und Personalentwicklung',3,150000.00,1,'2025-12-11 17:02:47'),('IT','IT-Abteilung','Informationstechnologie und Digitalisierung',4,250000.00,1,'2025-12-11 17:02:47'),('OFFICE','Büromaterial','Allgemeine Büroausstattung und Verbrauchsmaterial',1,25000.00,1,'2025-12-11 17:02:47'),('SALES','Vertrieb','Verkauf und Marketing',2,300000.00,1,'2025-12-11 17:02:47');
/*!40000 ALTER TABLE `cost_centers` ENABLE KEYS */;
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
  `old_status` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `new_status` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
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
) ENGINE=InnoDB AUTO_INCREMENT=21 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invoice_history`
--

LOCK TABLES `invoice_history` WRITE;
/*!40000 ALTER TABLE `invoice_history` DISABLE KEYS */;
INSERT INTO `invoice_history` VALUES (5,13,'Daten vervollständigt','8','0',NULL,NULL,'{\"ProjectId\": {\"NewValue\": \"WEB001\", \"OldValue\": \"\", \"DisplayName\": \"Projekt\"}, \"PurchaseOrderId\": {\"NewValue\": \"PO-2024-001\", \"OldValue\": \"\", \"DisplayName\": \"Bestellung\"}}',NULL,NULL,NULL,NULL,1,'2025-12-15 13:36:44'),(6,14,'Daten vervollständigt','8','0',NULL,NULL,'{\"ProjectId\": {\"NewValue\": \"OFF004\", \"OldValue\": \"\", \"DisplayName\": \"Projekt\"}, \"PurchaseOrderId\": {\"NewValue\": \"PO-2024-002\", \"OldValue\": \"\", \"DisplayName\": \"Bestellung\"}}',NULL,NULL,NULL,NULL,1,'2025-12-15 13:37:19'),(7,15,'Daten vervollständigt','8','0',NULL,NULL,'{\"ProjectId\": {\"NewValue\": \"WEB001\", \"OldValue\": \"\", \"DisplayName\": \"Projekt\"}, \"PurchaseOrderId\": {\"NewValue\": \"PO-2024-003\", \"OldValue\": \"\", \"DisplayName\": \"Bestellung\"}}',NULL,NULL,NULL,NULL,1,'2025-12-15 13:37:50'),(8,16,'Daten vervollständigt','8','0',NULL,NULL,'{\"ProjectId\": {\"NewValue\": \"OFF004\", \"OldValue\": \"\", \"DisplayName\": \"Projekt\"}, \"PurchaseOrderId\": {\"NewValue\": \"PO-2024-002\", \"OldValue\": \"\", \"DisplayName\": \"Bestellung\"}}',NULL,NULL,NULL,NULL,1,'2025-12-15 13:38:18'),(9,13,'Freigabe erteilt','3','0',NULL,'FREIGEGEBEN',NULL,'Freigabe erteilt (Teilfreigabe - Schritt 1 von 1)',NULL,NULL,NULL,1,'2025-12-15 13:38:31'),(10,14,'Freigabe abgelehnt','4','0',NULL,'ABGELEHNT',NULL,'Kacke',NULL,NULL,NULL,1,'2025-12-15 13:47:22'),(11,15,'Freigabe erteilt','3','0',NULL,'FREIGEGEBEN',NULL,'Freigabe erteilt (Teilfreigabe - Schritt 1 von 1)',NULL,NULL,NULL,1,'2025-12-15 13:47:47'),(12,15,'Freigabe erteilt','3','0',NULL,'FREIGEGEBEN',NULL,'Freigabe erteilt',NULL,NULL,NULL,1,'2025-12-15 13:48:13'),(13,22,'Automatisch freigegeben','Approved','System',NULL,'Freigegeben',NULL,'Rechnung automatisch freigegeben: Büromaterial - automatisch freigegeben (unter 100 EUR)',NULL,NULL,NULL,1,'2025-12-15 14:50:49'),(14,23,'Automatisch freigegeben','Approved','System',NULL,'Freigegeben',NULL,'Rechnung automatisch freigegeben: Wartungsarbeiten - automatisch freigegeben',NULL,NULL,NULL,1,'2025-12-15 14:50:49'),(15,24,'Automatisch freigegeben','Approved','System',NULL,'Freigegeben',NULL,'Rechnung automatisch freigegeben: Express-Lieferung Hardware - automatisch freigegeben',NULL,NULL,NULL,1,'2025-12-15 14:50:49'),(16,25,'Automatisch freigegeben','Approved','System',NULL,'Freigegeben',NULL,'Rechnung automatisch freigegeben: Verbrauchsmaterial Drucker - automatisch freigegeben',NULL,NULL,NULL,1,'2025-12-15 14:50:49'),(17,26,'Automatisch freigegeben','Approved','System',NULL,'Freigegeben',NULL,'Rechnung automatisch freigegeben: Monatliche Wartung - automatisch freigegeben',NULL,NULL,NULL,1,'2025-12-15 14:50:49'),(20,16,'Freigabe erteilt','Approved','User',NULL,'FREIGEGEBEN',NULL,'Admin hat alle ausstehenden Freigaben erteilt: Freigabe erteilt',NULL,NULL,NULL,1,'2025-12-15 17:06:52');
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
  `status` enum('Eingegangen','In_Pruefung','Freigabe_Erforderlich','Freigegeben','Abgelehnt','Bezahlt','Ueberfaellig','Storniert') COLLATE utf8mb4_unicode_ci DEFAULT 'Eingegangen',
  `requires_approval` tinyint(1) DEFAULT '1',
  `approval_level` int DEFAULT '1',
  `auto_approved` tinyint(1) DEFAULT '0',
  `pdf_file_path` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
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
  KEY `idx_status` (`status`),
  KEY `idx_supplier_id` (`supplier_id`),
  KEY `idx_cost_center_id` (`cost_center_id`),
  KEY `idx_project_id` (`project_id`),
  KEY `idx_invoice_date` (`invoice_date`),
  KEY `idx_due_date` (`due_date`),
  KEY `idx_received_date` (`received_date`),
  FULLTEXT KEY `description` (`description`,`internal_notes`),
  CONSTRAINT `invoices_ibfk_1` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`),
  CONSTRAINT `invoices_ibfk_2` FOREIGN KEY (`purchase_order_id`) REFERENCES `purchase_orders` (`id`),
  CONSTRAINT `invoices_ibfk_3` FOREIGN KEY (`cost_center_id`) REFERENCES `cost_centers` (`id`),
  CONSTRAINT `invoices_ibfk_4` FOREIGN KEY (`project_id`) REFERENCES `projects` (`id`),
  CONSTRAINT `invoices_ibfk_5` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`),
  CONSTRAINT `invoices_ibfk_6` FOREIGN KEY (`processed_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=27 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invoices`
--

LOCK TABLES `invoices` WRITE;
/*!40000 ALTER TABLE `invoices` DISABLE KEYS */;
INSERT INTO `invoices` VALUES (13,'INV-2024-001',1,'PO-2024-001','IT','WEB001',1000.00,190.00,1190.00,'EUR','2024-12-10','2026-01-15','2025-12-15 13:46:19','Freigegeben',1,1,0,NULL,NULL,NULL,'Microsoft software licenses',NULL,1,1,'2025-12-15 13:46:19','2025-12-15 14:45:16'),(14,'INV-2024-002',2,'PO-2024-002','OFFICE','OFF004',2500.00,475.00,2975.00,'EUR','2024-12-12','2026-01-15','2025-12-15 13:46:19','Abgelehnt',1,1,0,NULL,NULL,NULL,'Office supplies and equipment',NULL,1,1,'2025-12-15 13:46:19','2025-12-15 13:47:22'),(15,'INV-2024-003',4,'PO-2024-003','IT','WEB001',750.50,142.60,893.10,'EUR','2024-12-13','2026-01-15','2025-12-15 13:46:19','Freigegeben',1,1,0,NULL,NULL,NULL,'IT consulting services',NULL,2,1,'2025-12-15 13:46:19','2025-12-15 13:48:13'),(16,'INV-2024-004',3,'PO-2024-002','OFFICE','OFF004',350.00,66.50,416.50,'EUR','2024-12-14','2026-01-15','2025-12-15 13:46:19','Freigegeben',1,1,0,NULL,NULL,NULL,'Office furniture and storage',NULL,1,1,'2025-12-15 13:46:19','2025-12-15 17:06:52'),(22,'INV-2025-0020',1,NULL,'IT',NULL,71.43,13.57,85.00,'EUR','2025-01-10','2025-02-10','2025-12-15 14:50:49','Freigegeben',0,1,1,NULL,NULL,NULL,'Büromaterial - automatisch freigegeben (unter 100 EUR)',NULL,1,NULL,'2025-12-15 14:50:49','2025-12-15 14:50:49'),(23,'INV-2025-0021',1,NULL,'IT',NULL,63.45,12.05,75.50,'EUR','2025-01-11','2025-02-11','2025-12-15 14:50:49','Freigegeben',0,1,1,NULL,NULL,NULL,'Wartungsarbeiten - automatisch freigegeben',NULL,1,NULL,'2025-12-15 14:50:49','2025-12-15 14:50:49'),(24,'INV-2025-0022',1,NULL,'IT',NULL,79.83,15.17,95.00,'EUR','2025-01-12','2025-02-12','2025-12-15 14:50:49','Freigegeben',0,1,1,NULL,NULL,NULL,'Express-Lieferung Hardware - automatisch freigegeben',NULL,1,NULL,'2025-12-15 14:50:49','2025-12-15 14:50:49'),(25,'INV-2025-0023',1,NULL,'FINANCE',NULL,54.62,10.38,65.00,'EUR','2025-01-13','2025-02-13','2025-12-15 14:50:49','Freigegeben',0,1,1,NULL,NULL,NULL,'Verbrauchsmaterial Drucker - automatisch freigegeben',NULL,1,NULL,'2025-12-15 14:50:49','2025-12-15 14:50:49'),(26,'INV-2025-0024',1,NULL,'IT',NULL,75.62,14.37,89.99,'EUR','2025-01-14','2025-02-14','2025-12-15 14:50:49','Freigegeben',0,1,1,NULL,NULL,NULL,'Monatliche Wartung - automatisch freigegeben',NULL,1,NULL,'2025-12-15 14:50:49','2025-12-15 14:50:49');
/*!40000 ALTER TABLE `invoices` ENABLE KEYS */;
UNLOCK TABLES;

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
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `notifications`
--

LOCK TABLES `notifications` WRITE;
/*!40000 ALTER TABLE `notifications` DISABLE KEYS */;
INSERT INTO `notifications` VALUES (3,1,13,'invoice_approved','Rechnung freigegeben','Rechnung INV-2024-001 von Microsoft Deutschland über 1.190,00 € EUR wurde freigegeben.',0,'normal','/invoices/13','2025-12-15 13:38:31',NULL),(4,3,13,'invoice_approved','Rechnung freigegeben','Rechnung INV-2024-001 von Microsoft Deutschland über 1.190,00 € EUR wurde freigegeben.',0,'normal','/invoices/13','2025-12-15 13:38:31',NULL),(5,1,14,'invoice_rejected','Rechnung abgelehnt','Rechnung INV-2024-002 von Amazon Business über 2.975,00 € EUR wurde abgelehnt.',0,'high','/invoices/14','2025-12-15 13:47:22',NULL),(6,3,14,'invoice_rejected','Rechnung abgelehnt','Rechnung INV-2024-002 von Amazon Business über 2.975,00 € EUR wurde abgelehnt.',0,'high','/invoices/14','2025-12-15 13:47:22',NULL),(7,2,15,'invoice_approved','Rechnung freigegeben','Rechnung INV-2024-003 von IT-Solutions über 893,10 € EUR wurde freigegeben.',0,'normal','/invoices/15','2025-12-15 13:47:47',NULL),(8,3,15,'invoice_approved','Rechnung freigegeben','Rechnung INV-2024-003 von IT-Solutions über 893,10 € EUR wurde freigegeben.',0,'normal','/invoices/15','2025-12-15 13:47:47',NULL),(9,2,15,'invoice_approved','Rechnung freigegeben','Rechnung INV-2024-003 von IT-Solutions über 893,10 € EUR wurde freigegeben.',0,'normal','/invoices/15','2025-12-15 13:48:13',NULL),(10,3,15,'invoice_approved','Rechnung freigegeben','Rechnung INV-2024-003 von IT-Solutions über 893,10 € EUR wurde freigegeben.',0,'normal','/invoices/15','2025-12-15 13:48:13',NULL),(11,1,16,'invoice_approved','Rechnung freigegeben','Rechnung INV-2024-004 von Büroservice Express über 416,50 € EUR wurde freigegeben.',0,'normal','/invoices/16','2025-12-15 17:06:52',NULL),(12,3,16,'invoice_approved','Rechnung freigegeben','Rechnung INV-2024-004 von Büroservice Express über 416,50 € EUR wurde freigegeben.',0,'normal','/invoices/16','2025-12-15 17:06:52',NULL);
/*!40000 ALTER TABLE `notifications` ENABLE KEYS */;
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
  `status` enum('Geplant','Aktiv','Pausiert','Abgeschlossen') COLLATE utf8mb4_unicode_ci DEFAULT 'Geplant',
  `start_date` date DEFAULT NULL,
  `end_date` date DEFAULT NULL,
  `project_manager_id` int DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `cost_center_id` (`cost_center_id`),
  KEY `project_manager_id` (`project_manager_id`),
  CONSTRAINT `projects_ibfk_1` FOREIGN KEY (`cost_center_id`) REFERENCES `cost_centers` (`id`),
  CONSTRAINT `projects_ibfk_2` FOREIGN KEY (`project_manager_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `projects`
--

LOCK TABLES `projects` WRITE;
/*!40000 ALTER TABLE `projects` DISABLE KEYS */;
INSERT INTO `projects` VALUES ('HR002','Mitarbeiter-Portal','Entwicklung eines Self-Service Portals für Mitarbeiter','HR',15000.00,0.00,'Geplant',NULL,NULL,3,'2025-12-11 17:02:47'),('OFF004','Büroausstattung 2024','Modernisierung der Büroausstattung','OFFICE',5000.00,0.00,'Aktiv',NULL,NULL,1,'2025-12-11 17:02:47'),('SALES003','CRM System','Einführung eines neuen Customer Relationship Management Systems','SALES',40000.00,0.00,'Aktiv',NULL,NULL,2,'2025-12-11 17:02:47'),('WEB001','Website Relaunch','Neugestaltung der Unternehmenswebsite','IT',25000.00,0.00,'Aktiv',NULL,NULL,4,'2025-12-11 17:02:47');
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
  `cost_center_id` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `project_id` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `total_amount` decimal(12,2) NOT NULL,
  `currency` varchar(3) COLLATE utf8mb4_unicode_ci DEFAULT 'EUR',
  `status` enum('Offen','Teilweise_Erfuellt','Erfuellt','Storniert') COLLATE utf8mb4_unicode_ci DEFAULT 'Offen',
  `created_by` int NOT NULL,
  `approved_by` int DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `approved_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `cost_center_id` (`cost_center_id`),
  KEY `project_id` (`project_id`),
  KEY `created_by` (`created_by`),
  KEY `approved_by` (`approved_by`),
  CONSTRAINT `purchase_orders_ibfk_1` FOREIGN KEY (`cost_center_id`) REFERENCES `cost_centers` (`id`),
  CONSTRAINT `purchase_orders_ibfk_2` FOREIGN KEY (`project_id`) REFERENCES `projects` (`id`),
  CONSTRAINT `purchase_orders_ibfk_3` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`),
  CONSTRAINT `purchase_orders_ibfk_4` FOREIGN KEY (`approved_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `purchase_orders`
--

LOCK TABLES `purchase_orders` WRITE;
/*!40000 ALTER TABLE `purchase_orders` DISABLE KEYS */;
INSERT INTO `purchase_orders` VALUES ('PO-2024-001','Software-Lizenzen Q1','Einkauf Microsoft Lizenzen für Q1 2024','IT','WEB001',5000.00,'EUR','Offen',1,NULL,'2025-12-15 14:21:55',NULL),('PO-2024-002','Büromöbel Erweiterung','Neue Schreibtische und Stühle für Büro 3.OG','OFFICE','OFF004',3500.00,'EUR','Offen',1,NULL,'2025-12-15 14:21:55',NULL),('PO-2024-003','Marketing Kampagne','Online-Werbung für Produktlaunch','SALES','SALES003',10000.00,'EUR','Offen',2,NULL,'2025-12-15 14:21:55',NULL),('PO-2024-004','Schulungsmaßnahmen','Externe Schulungen für Mitarbeiter','HR','HR002',2500.00,'EUR','Offen',3,NULL,'2025-12-15 14:21:55',NULL);
/*!40000 ALTER TABLE `purchase_orders` ENABLE KEYS */;
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
  `permissions` json DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roles`
--

LOCK TABLES `roles` WRITE;
/*!40000 ALTER TABLE `roles` DISABLE KEYS */;
INSERT INTO `roles` VALUES (1,'Administrator','System Administrator mit allen Rechten','[\"all\"]'),(2,'Freigeber','Kann Rechnungen freigeben und ablehnen','[\"approve_invoices\", \"view_all_invoices\"]'),(3,'Buchhaltung','Kann alle Rechnungen einsehen und bearbeiten','[\"view_all_invoices\", \"edit_invoices\", \"process_payments\"]'),(4,'Mitarbeiter','Kann eigene Rechnungen einsehen','[\"view_own_invoices\"]'),(5,'Controller','Kann Reports erstellen und Budgets überwachen','[\"view_reports\", \"view_budgets\"]'),(6,'Manager','Kann Kostenstellen-bezogene Rechnungen freigeben','[\"approve_cost_center_invoices\", \"view_team_invoices\"]');
/*!40000 ALTER TABLE `roles` ENABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `suppliers`
--

LOCK TABLES `suppliers` WRITE;
/*!40000 ALTER TABLE `suppliers` DISABLE KEYS */;
INSERT INTO `suppliers` VALUES (1,'Microsoft Deutschland','Microsoft Deutschland GmbH',NULL,NULL,NULL,NULL,NULL,'München','Deutschland','buchhaltung@microsoft.de',NULL,NULL,NULL,NULL,30,1,'2025-12-15 12:43:54','2025-12-15 12:43:54'),(2,'Amazon Business','Amazon EU S.à r.l.',NULL,NULL,NULL,NULL,NULL,'Berlin','Deutschland','business@amazon.de',NULL,NULL,NULL,NULL,30,1,'2025-12-15 12:43:54','2025-12-15 12:43:54'),(3,'Büroservice Express','Büroservice Express GmbH',NULL,NULL,NULL,NULL,NULL,'Hamburg','Deutschland','info@bueroservice.de',NULL,NULL,NULL,NULL,30,1,'2025-12-15 12:43:54','2025-12-15 12:43:54'),(4,'IT-Solutions','IT-Solutions & Development GmbH',NULL,NULL,NULL,NULL,NULL,'Frankfurt','Deutschland','contact@it-solutions.de',NULL,NULL,NULL,NULL,30,1,'2025-12-15 12:43:54','2025-12-15 12:43:54'),(5,'Office World','Office World Handels-GmbH',NULL,NULL,NULL,NULL,NULL,'Köln','Deutschland','service@officeworld.de',NULL,NULL,NULL,NULL,30,1,'2025-12-15 12:43:54','2025-12-15 12:43:54');
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
INSERT INTO `system_config` VALUES (1,'company_name','Musterfirma GmbH','string','Name der Firma',1,NULL,'2025-12-11 17:02:47'),(2,'default_currency','EUR','string','Standard-Währung',1,NULL,'2025-12-11 17:02:47'),(3,'auto_approval_limit','50','number','Automatische Freigabe-Grenze in EUR',1,NULL,'2025-12-11 17:02:47'),(4,'payment_terms_days','30','number','Standard-Zahlungsziel in Tagen',1,NULL,'2025-12-11 17:02:47'),(5,'notification_email_enabled','true','boolean','E-Mail-Benachrichtigungen aktiviert',1,NULL,'2025-12-11 17:02:47'),(6,'pdf_storage_path','/uploads/invoices/','string','Pfad für PDF-Dateien',1,NULL,'2025-12-11 17:02:47'),(7,'max_file_size_mb','10','number','Maximale Dateigröße in MB',1,NULL,'2025-12-11 17:02:47'),(8,'ad_integration_enabled','true','boolean','Active Directory Integration aktiviert',1,NULL,'2025-12-11 17:02:47'),(9,'backup_retention_days','365','number','Aufbewahrungszeit für Backups in Tagen',1,NULL,'2025-12-11 17:02:47');
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
INSERT INTO `user_roles` VALUES (1,1,'2025-12-11 17:02:47'),(2,2,'2025-12-11 17:02:47'),(3,3,'2025-12-11 17:02:47'),(4,6,'2025-12-11 17:02:47'),(5,5,'2025-12-11 17:02:47');
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
INSERT INTO `users` VALUES (1,'admin','$2a$11$FNf.BJrxp6VE2zqNE3gFj.5ab4EW.usgVjxudFDZaywRcmO7sd0yi','admin@firma.de','System','Administrator',NULL,'2025-12-11 16:53:28',0,NULL,1,'2025-12-11 17:02:47','2025-12-15 17:16:08'),(2,'max.mustermann','$2a$11$RMfe8rKXC5JFJbcuATyBr.XceGHthHyAVVySps6ZHEVVab8hEcceG','max.mustermann@firma.de','Max','Mustermann',NULL,'2025-12-11 17:41:13',0,NULL,1,'2025-12-11 17:02:47','2025-12-11 19:13:29'),(3,'maria.mueller','$2a$11$LaNU.tjQsm1Cx8269R/bgu/JhP3rTWrnkA1m1TfuWFatVOGW8WUYW','maria.mueller@firma.de','Maria','Müller',NULL,'2025-12-11 17:41:13',0,NULL,1,'2025-12-11 17:02:47','2025-12-11 19:13:29'),(4,'hans.schmidt','$2a$11$q7x7BBVoRceMPThywy8L9.tr3tzJR/GyveC5KfqrnGP4dl/8ejLce','hans.schmidt@firma.de','Hans','Schmidt',NULL,'2025-12-11 17:41:13',0,NULL,1,'2025-12-11 17:02:47','2025-12-11 19:13:29'),(5,'lisa.klein','$2a$11$W3lIRzqp5sotjnismJSlc.sU33d86IwiLLoPeOHqFOvUkAh/2yERG','lisa.klein@firma.de','Lisa','Klein',NULL,'2025-12-11 17:41:13',0,NULL,1,'2025-12-11 17:02:47','2025-12-11 19:13:30');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Temporary view structure for view `v_dashboard_stats`
--

DROP TABLE IF EXISTS `v_dashboard_stats`;
/*!50001 DROP VIEW IF EXISTS `v_dashboard_stats`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `v_dashboard_stats` AS SELECT 
 1 AS `new_invoices`,
 1 AS `pending_approval`,
 1 AS `approved_invoices`,
 1 AS `overdue_invoices`,
 1 AS `monthly_approved_amount`,
 1 AS `pending_approval_amount`*/;
SET character_set_client = @saved_cs_client;

--
-- Temporary view structure for view `v_invoice_details`
--

DROP TABLE IF EXISTS `v_invoice_details`;
/*!50001 DROP VIEW IF EXISTS `v_invoice_details`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `v_invoice_details` AS SELECT 
 1 AS `id`,
 1 AS `invoice_number`,
 1 AS `total_amount`,
 1 AS `currency`,
 1 AS `status`,
 1 AS `invoice_date`,
 1 AS `due_date`,
 1 AS `description`,
 1 AS `supplier_name`,
 1 AS `supplier_email`,
 1 AS `cost_center_name`,
 1 AS `cost_center_manager_id`,
 1 AS `project_name`,
 1 AS `purchase_order_title`,
 1 AS `is_overdue`,
 1 AS `days_overdue`,
 1 AS `created_by_name`,
 1 AS `processed_by_name`*/;
SET character_set_client = @saved_cs_client;

--
-- Final view structure for view `v_dashboard_stats`
--

/*!50001 DROP VIEW IF EXISTS `v_dashboard_stats`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `v_dashboard_stats` AS select (select count(0) from `invoices` where (`invoices`.`status` = 'Eingegangen')) AS `new_invoices`,(select count(0) from `invoices` where (`invoices`.`status` = 'Freigabe_Erforderlich')) AS `pending_approval`,(select count(0) from `invoices` where (`invoices`.`status` = 'Freigegeben')) AS `approved_invoices`,(select count(0) from `invoices` where (`invoices`.`status` = 'Ueberfaellig')) AS `overdue_invoices`,(select coalesce(sum(`invoices`.`total_amount`),0) from `invoices` where ((`invoices`.`status` in ('Freigegeben','Bezahlt')) and (month(`invoices`.`invoice_date`) = month(curdate())) and (year(`invoices`.`invoice_date`) = year(curdate())))) AS `monthly_approved_amount`,(select coalesce(sum(`invoices`.`total_amount`),0) from `invoices` where (`invoices`.`status` = 'Freigabe_Erforderlich')) AS `pending_approval_amount` */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `v_invoice_details`
--

/*!50001 DROP VIEW IF EXISTS `v_invoice_details`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `v_invoice_details` AS select `i`.`id` AS `id`,`i`.`invoice_number` AS `invoice_number`,`i`.`total_amount` AS `total_amount`,`i`.`currency` AS `currency`,`i`.`status` AS `status`,`i`.`invoice_date` AS `invoice_date`,`i`.`due_date` AS `due_date`,`i`.`description` AS `description`,`s`.`name` AS `supplier_name`,`s`.`email` AS `supplier_email`,`cc`.`name` AS `cost_center_name`,`cc`.`manager_id` AS `cost_center_manager_id`,`p`.`name` AS `project_name`,`po`.`title` AS `purchase_order_title`,(case when ((`i`.`due_date` < curdate()) and (`i`.`status` not in ('Bezahlt','Storniert'))) then true else false end) AS `is_overdue`,(to_days(curdate()) - to_days(`i`.`due_date`)) AS `days_overdue`,concat(`creator`.`first_name`,' ',`creator`.`last_name`) AS `created_by_name`,concat(`processor`.`first_name`,' ',`processor`.`last_name`) AS `processed_by_name` from ((((((`invoices` `i` left join `suppliers` `s` on((`i`.`supplier_id` = `s`.`id`))) left join `cost_centers` `cc` on((`i`.`cost_center_id` = `cc`.`id`))) left join `projects` `p` on((`i`.`project_id` = `p`.`id`))) left join `purchase_orders` `po` on((`i`.`purchase_order_id` = `po`.`id`))) left join `users` `creator` on((`i`.`created_by` = `creator`.`id`))) left join `users` `processor` on((`i`.`processed_by` = `processor`.`id`))) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-12-15 19:43:22
