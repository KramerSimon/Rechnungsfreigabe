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
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `approval_rules`
--

LOCK TABLES `approval_rules` WRITE;
/*!40000 ALTER TABLE `approval_rules` DISABLE KEYS */;
INSERT INTO `approval_rules` VALUES (6,'Auto-Freigabe','','automatic',10,1,'[{\"field\": \"amount\", \"value\": \"100\", \"operator\": \"<=\"}]','[{\"type\": \"auto_approve\", \"value\": \"approved\", \"description\": \"Automatisch freigeben\"}]',1,'2025-12-18 14:27:26','2026-01-22 13:17:56'),(7,'Standard-Freigabe','','manual',999,1,'[{\"field\": \"amount\", \"value\": \"500\", \"operator\": \">\"}]','[{\"type\": \"assign_to\", \"value\": \"1\", \"description\": \"Zuweisen an Systemadministrator\"}]',1,'2025-12-20 11:48:34','2025-12-20 11:48:34'),(8,'Mehrstufige Freigabe','','manual',10,1,'[{\"field\": \"amount\", \"value\": \"1000\", \"operator\": \">\"}]','[{\"type\": \"require_approval\", \"value\": \"\", \"stages\": [{\"userId\": 5, \"stepNumber\": 1, \"approvalLevel\": 1}, {\"userId\": 1, \"stepNumber\": 2, \"approvalLevel\": 2}], \"description\": \"Mehrstufige Freigabe\"}]',1,'2025-12-21 12:45:11','2025-12-21 12:45:11'),(9,'Test','','automatic',10,1,'[{\"field\": \"costCenter\", \"value\": \"OFFICE\", \"operator\": \"=\"}]','[{\"type\": \"auto_approve\", \"value\": \"\", \"description\": \"Automatisch freigeben\"}]',1,'2026-01-07 09:48:50','2026-01-07 09:48:50');
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
  `status` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT 'Pending',
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
) ENGINE=InnoDB AUTO_INCREMENT=66 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `approval_workflows`
--

LOCK TABLES `approval_workflows` WRITE;
/*!40000 ALTER TABLE `approval_workflows` DISABLE KEYS */;
INSERT INTO `approval_workflows` VALUES (63,116,NULL,1,2,1,'Pending',NULL,NULL,'2026-01-22 13:33:29'),(64,117,8,1,5,1,'Pending',NULL,NULL,'2026-01-22 13:33:54'),(65,117,8,2,1,2,'Waiting',NULL,NULL,'2026-01-22 13:33:54');
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
INSERT INTO `cost_centers` VALUES ('FACILITY','Facility Management','Geb√§ude, Reinigung, Sicherheit',NULL,80000.00,1,'2025-12-12 09:31:16'),('FINANCE','Finanzen','Buchhaltung, Controlling und Finanzen',5,100000.00,1,'2025-12-12 09:31:16'),('FLEET','Fuhrpark','',NULL,100000.00,1,'2025-12-20 14:16:26'),('HR','Personalabteilung','Human Resources und Personalentwicklung',3,150000.00,1,'2025-12-12 09:31:16'),('IT','IT-Abteilung','Informationstechnologie und Digitalisierung',4,250000.00,1,'2025-12-12 09:31:16'),('OFFICE','B√ºromaterial','Allgemeine B√ºroausstattung und Verbrauchsmaterial',1,25000.00,1,'2025-12-12 09:31:16'),('SALES','Vertrieb','Verkauf und Marketing',2,300000.00,1,'2025-12-12 09:31:16');
/*!40000 ALTER TABLE `cost_centers` ENABLE KEYS */;
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
  `trigger_status` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `trigger_after_hours` int NOT NULL DEFAULT '48',
  `repeat_interval_hours` int DEFAULT NULL,
  `max_escalations` int DEFAULT '3',
  `notify_role` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `notify_user_id` int DEFAULT NULL,
  `message_template` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `notify_user_id` (`notify_user_id`),
  CONSTRAINT `escalation_rules_ibfk_1` FOREIGN KEY (`notify_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `escalation_rules`
--

LOCK TABLES `escalation_rules` WRITE;
/*!40000 ALTER TABLE `escalation_rules` DISABLE KEYS */;
INSERT INTO `escalation_rules` VALUES (1,'Eskalation nach 48h','','Eingegangen',48,NULL,3,'',NULL,'',1,'2025-12-18 06:38:23','2025-12-18 06:38:23');
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
) ENGINE=InnoDB AUTO_INCREMENT=149 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invoice_history`
--

LOCK TABLES `invoice_history` WRITE;
/*!40000 ALTER TABLE `invoice_history` DISABLE KEYS */;
INSERT INTO `invoice_history` VALUES (147,116,'Rechnung importiert','Created','Import',NULL,'Freigabe_Erforderlic',NULL,NULL,NULL,NULL,'E-Mail',1,'2026-01-22 13:33:29'),(148,117,'Rechnung importiert','Created','Import',NULL,'Freigabe_Erforderlic',NULL,NULL,NULL,NULL,'E-Mail',1,'2026-01-22 13:33:54');
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
  `status` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT 'Eingegangen',
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
  KEY `idx_status` (`status`),
  KEY `idx_supplier_id` (`supplier_id`),
  KEY `idx_cost_center_id` (`cost_center_id`),
  KEY `idx_project_id` (`project_id`),
  KEY `idx_invoice_date` (`invoice_date`),
  KEY `idx_due_date` (`due_date`),
  KEY `idx_received_date` (`received_date`),
  KEY `idx_pdf_content` (`pdf_file_size`),
  FULLTEXT KEY `description` (`description`,`internal_notes`),
  CONSTRAINT `invoices_ibfk_1` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`),
  CONSTRAINT `invoices_ibfk_2` FOREIGN KEY (`purchase_order_id`) REFERENCES `purchase_orders` (`id`),
  CONSTRAINT `invoices_ibfk_3` FOREIGN KEY (`cost_center_id`) REFERENCES `cost_centers` (`id`),
  CONSTRAINT `invoices_ibfk_4` FOREIGN KEY (`project_id`) REFERENCES `projects` (`id`),
  CONSTRAINT `invoices_ibfk_5` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`),
  CONSTRAINT `invoices_ibfk_6` FOREIGN KEY (`processed_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=118 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invoices`
--

LOCK TABLES `invoices` WRITE;
/*!40000 ALTER TABLE `invoices` DISABLE KEYS */;
INSERT INTO `invoices` VALUES (116,'FAT-001-2026',61,NULL,NULL,NULL,245.90,54.10,300.00,'EUR','2025-12-15','2026-02-21','2026-01-22 13:33:29','Freigabe_Erforderlich',1,1,0,NULL,_binary '%PDF-1.4\r\n%ìåãû ReportLab Generated PDF document http://www.reportlab.com\r\n1 0 obj\r\n<<\r\n/F1 2 0 R /F2 3 0 R\r\n>>\r\nendobj\r\n2 0 obj\r\n<<\r\n/BaseFont /Helvetica /Encoding /WinAnsiEncoding /Name /F1 /Subtype /Type1 /Type /Font\r\n>>\r\nendobj\r\n3 0 obj\r\n<<\r\n/BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding /Name /F2 /Subtype /Type1 /Type /Font\r\n>>\r\nendobj\r\n4 0 obj\r\n<<\r\n/Contents 8 0 R /MediaBox [ 0 0 595.2756 841.8898 ] /Parent 7 0 R /Resources <<\r\n/Font 1 0 R /ProcSet [ /PDF /Text /ImageB /ImageC /ImageI ]\r\n>> /Rotate 0 /Trans <<\r\n\r\n>> \r\n  /Type /Page\r\n>>\r\nendobj\r\n5 0 obj\r\n<<\r\n/PageMode /UseNone /Pages 7 0 R /Type /Catalog\r\n>>\r\nendobj\r\n6 0 obj\r\n<<\r\n/Author (\\(anonymous\\)) /CreationDate (D:20251217135140+00\'00\') /Creator (\\(unspecified\\)) /Keywords () /ModDate (D:20251217135140+00\'00\') /Producer (ReportLab PDF Library - www.reportlab.com) \r\n  /Subject (\\(unspecified\\)) /Title (\\(anonymous\\)) /Trapped /False\r\n>>\r\nendobj\r\n7 0 obj\r\n<<\r\n/Count 1 /Kids [ 4 0 R ] /Type /Pages\r\n>>\r\nendobj\r\n8 0 obj\r\n<<\r\n/Filter [ /ASCII85Decode /FlateDecode ] /Length 699\r\n>>\r\nstream\r\nGat=)gMY_1&:N^lk+r]I<&8m+^#Y0=VFm+C!F24@Z!`DH_R=4G<oWF,?5m\"9>?b8*1?ZlQ3BO!i!ResRI@1(_MI>qpJ8pCr^n`>$HA4(Mmq7\\=bYUSRRA\\P2;$I&7Jc)W?\\a6O?,pou5#H\"Mt33O*,@H]GF\']IFHqN%C\"qCs#H61bZ74a_(`UcP3MXj\"aYAR6mN$+)%\\ErVt0FIp<0/rDEF*ro.H3-\'L%ITDHap3Bt]*#GEequPjXUsaeqh^tLtlXM5GbLpa@o(s>>DmMP`7@%^>I4VV&U-I?LSZZE=lQh,9o\\\"791,gp]ZQo`uN$0sp3kFNN^G##8d&RCIo?dY&7EE!#ILrM&?8Ln.l9[VGA<t@\'Aj:4T:N=tNF(sJ>b]UWT=6jnF?-!Pi5S3>oh+i3PLXKh^IF<)1PU6Hn$>H(Hj;QVDXjQCY$<?,2%=jAZCO`FpbM;GXQ>3=8LWT)%SWn+tQsq3a?JqgcpGLR7>t0fYE5\\W=-[]pp7p-FBoF66L^*V*Iri<%:<X+jm\\h,j%h8L1F$BuoLPK5B++b_:h_\\%\\G?8*e2\'!$2)UYgFIM!8\";pq:TG@[fe>)jo:q2Bcm**(g[3jAEPT(qnERg+l=&Sd-HXZFQGVR\\2VE-`9o.:14qGLOXYOnb-,9FSKf1/o7rO4Zohm#ljL8U_AC[%mGM8Q^`\"7G=8+u#;;onWW~>endstream\r\nendobj\r\nxref\r\n0 9\r\n0000000000 65535 f \r\n0000000073 00000 n \r\n0000000114 00000 n \r\n0000000221 00000 n \r\n0000000333 00000 n \r\n0000000536 00000 n \r\n0000000604 00000 n \r\n0000000887 00000 n \r\n0000000946 00000 n \r\ntrailer\r\n<<\r\n/ID \r\n[<82de21b91b86403725a14956d4a41851><82de21b91b86403725a14956d4a41851>]\r\n% ReportLab generated PDF document -- digest (http://www.reportlab.com)\r\n\r\n/Info 6 0 R\r\n/Root 5 0 R\r\n/Size 9\r\n>>\r\nstartxref\r\n1735\r\n%%EOF\r\n',2214,'fattura_003.pdf','FATTURA | Numero fattura: 003/2025 | Data: 15/12/2025 | Fornitore | Azienda Demo SRL',NULL,1,NULL,'2026-01-22 13:33:29','2026-01-22 14:33:28'),(117,'FAT-002-2026',61,NULL,NULL,NULL,1024.59,225.41,1250.00,'EUR','2025-12-15','2026-02-21','2026-01-22 13:33:54','Freigabe_Erforderlich',1,1,0,NULL,_binary '%PDF-1.6\r%\‚\„\œ\”\r\n7 0 obj\r<</Linearized 1/L 6183/O 9/E 1707/N 1/T 5894/H [ 447 132]>>\rendobj\r                       \r\n12 0 obj\r<</DecodeParms<</Columns 4/Predictor 12>>/Filter/FlateDecode/ID[<8B49BF133DB554C872F8842FFF1159FF><C30457C9E1DCAA4483AE873FF001ED09>]/Index[7 10]/Info 6 0 R/Length 48/Prev 5895/Root 8 0 R/Size 17/Type/XRef/W[1 2 1]>>stream\r\nh\ﬁbbd``b`2æ@ÇÒ<à∏	$˛\ÿ0012LI00Bàˇå[ˇ\0võ\÷\r\nendstream\rendobj\rstartxref\r\n0\r\n%%EOF\r\n        \r\n16 0 obj\r<</Filter/FlateDecode/I 67/Length 53/S 38>>stream\r\nh\ﬁb```f``\‚a\0iT¿d\‡h@cÜbÜÛúL[\Z†Ç¢\Ô!\Z%\0\0\\\⁄Û\r\nendstream\rendobj\r8 0 obj\r<</Metadata 1 0 R/PageMode/UseNone/Pages 5 0 R/Type/Catalog>>\rendobj\r9 0 obj\r<</Contents 11 0 R/CropBox[0 0 595.2756 841.8898]/MediaBox[0 0 595.2756 841.8898]/Parent 5 0 R/Resources<</Font 13 0 R/ProcSet[/PDF/Text/ImageB/ImageC/ImageI]>>/Rotate 0/Trans<<>>/Type/Page>>\rendobj\r10 0 obj\r<</Filter/FlateDecode/First 18/Length 122/N 3/Type/ObjStm>>stream\r\nh\ﬁ24V0P04Q02Q04U040Q∞±\—w3	(\ÈªÅÑÅ,;;†∏Sbq™[~^âæGjNYjIfr¢æk^r~Jf^∫~xfûc^q&úÔóòõ\n4H?∏4©§≤ U?HÇI}ê8\Ã\”u\ \œI!d®nC\0Ω\·Bj\r\nendstream\rendobj\r11 0 obj\r<</Filter/FlateDecode/Length 557>>stream\r\nxúùUMè\⁄0ΩÛ+\ÊRâ]©a\Ï8â\Ÿ[∫\’V\€jwO\\,0mV$QCh%~}\'NN K[E\‰c\ﬁøyûëaÄt1{_%\0˜\nFµ&<\Í	>™¡\œ´ôëÑH0O ±§\–˘˜\Â≤f9JLh)LñKq^.ƒ¢\»cBJ\¬\’\ZÜ•æ\Õ\'7†^A\›\¬˚6L™3∫\⁄\ ‹ø™\Ã+e2Å÷ÑU~\›\'&\œ`£ãbü\Î;@\‰#é<8ä_Tc\◊}^Võ\ÍÇ4X0bˇ¶“Å\¬¿-†•`)\\\…\Ÿm∂Ú\«\Zio≥<çã,7V˙T`Y\≈pràM∫\÷05Iã˘ì\ÂX\‰%\÷0\œ\r\ÏCdàUt9úYﬁú†g_&w®êq_a$\«¯ñY\◊Q\‚QüY&\ﬁ6˚aK~äãV\ÔcùÆ~ƒçÀæ∫q\n\"/Ù	\∆n\'<\…#0\Ë>i8RJÛCiû%\‰6∑≠\—\È\Œ\nU\„x\rÅ˚•\◊\·\‘\ÏVy|à≥\‘8u;Lé\rw∂\◊iK_`W6\‹\Á\‹5r\…\ÎVûÒ˝_eÖﬁö+¸K^ò¸WY3¨cXe\Ènø5\ÈA˜\‘JOà:ëı\‘\·SWéã3†á=\≈\€Ã≤°ü\È˜Z∑b˛â>~õï\ÿv\—\À6Üu[áRÅgy\‘*F/Uå6\Õ\ÀgC\‰u6w≥˝:ËüÇ≥Û9u\Á∞E\„ø=≠x˚¥™{‹úçe´\·|œã	£ˇ<\È–†øÑwß´\Ì~ß;\"0¯lr	\r\nendstream\rendobj\r1 0 obj\r<</Length 3691/Subtype/XML/Type/Metadata>>stream\r\n<?xpacket begin=\"Ôªø\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n<x:xmpmeta xmlns:x=\"adobe:ns:meta/\" x:xmptk=\"Adobe XMP Core 5.4-c006 80.159825, 2016/09/16-03:31:08        \">\n   <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n      <rdf:Description rdf:about=\"\"\n            xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n            xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"\n            xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\"\n            xmlns:xmpMM=\"http://ns.adobe.com/xap/1.0/mm/\">\n         <dc:format>application/pdf</dc:format>\n         <dc:creator>\n            <rdf:Seq>\n               <rdf:li>(anonymous)</rdf:li>\n            </rdf:Seq>\n         </dc:creator>\n         <dc:description>\n            <rdf:Alt>\n               <rdf:li xml:lang=\"x-default\">(unspecified)</rdf:li>\n            </rdf:Alt>\n         </dc:description>\n         <dc:title>\n            <rdf:Alt>\n               <rdf:li xml:lang=\"x-default\">(anonymous)</rdf:li>\n            </rdf:Alt>\n         </dc:title>\n         <xmp:CreateDate>2025-12-17T13:51:40Z</xmp:CreateDate>\n         <xmp:CreatorTool>(unspecified)</xmp:CreatorTool>\n         <xmp:ModifyDate>2026-01-21T15:31:23+01:00</xmp:ModifyDate>\n         <xmp:MetadataDate>2026-01-21T15:31:23+01:00</xmp:MetadataDate>\n         <pdf:Keywords/>\n         <pdf:Producer>ReportLab PDF Library - www.reportlab.com</pdf:Producer>\n         <pdf:Trapped>False</pdf:Trapped>\n         <xmpMM:DocumentID>uuid:51159c19-db28-4aba-b679-aeb9d688ab17</xmpMM:DocumentID>\n         <xmpMM:InstanceID>uuid:102b4301-c501-4e5e-81da-abbb2108831e</xmpMM:InstanceID>\n      </rdf:Description>\n   </rdf:RDF>\n</x:xmpmeta>\n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                           \n<?xpacket end=\"w\"?>\r\nendstream\rendobj\r2 0 obj\r<</Filter/FlateDecode/First 4/Length 48/N 1/Type/ObjStm>>stream\r\nh\ﬁ2U0P∞±\—w\Œ/\Õ+Q0\‘˜\ŒL)é∂ä\≈\ÍáT§\Í$¶ß\€\Ÿ\0\÷wÄ\r\nendstream\rendobj\r3 0 obj\r<</Filter/FlateDecode/First 4/Length 187/N 1/Type/ObjStm>>stream\r\nh\ﬁd\œ\À\n\¬0Ö\·W\…\Œ\—$≠U\ƒ“ç\n¢Æ\ƒM⁄åi;eöP˙ˆ^]ˇ|Œî)∂X\»U7$~\·¶∆∫Ø0¥!\‰ö¿xáuj<t©(—ëû\È8\—u˛Ù7u\€@\·Æ\Ïn†\ÔêlÀÖ‹°˝Ú©zzù\ƒ:äáJî\Zπ\'¥°\0\‚hê¸\÷\‰lüfl\Îr2‘≥\Î∫nL\ÔVö|\\`%\‰1\‰w(¸ˇ\…˘~oú\»4\rXôô≤Ö\ÂÚ!¿\0ENã\r\nendstream\rendobj\r4 0 obj\r<</DecodeParms<</Columns 3/Predictor 12>>/Filter/FlateDecode/ID[<8B49BF133DB554C872F8842FFF1159FF><C30457C9E1DCAA4483AE873FF001ED09>]/Info 6 0 R/Length 37/Root 8 0 R/Size 7/Type/XRef/W[1 2 0]>>stream\r\nh\ﬁbb```bd[\Õ\ƒ¿øÉâÅ°áâÅIÇâÒ\Âo õ ¿\0/Q\r\nendstream\rendobj\rstartxref\r\n116\r\n%%EOF\r\n',6183,'fattura_002.pdf','FATTURA | Numero fattura: 002/2025 | Data: 15/12/2025 | Fornitore | Azienda Demo SRL',NULL,1,NULL,'2026-01-22 13:33:54','2026-01-22 14:33:54');
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
) ENGINE=InnoDB AUTO_INCREMENT=221 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `notifications`
--

LOCK TABLES `notifications` WRITE;
/*!40000 ALTER TABLE `notifications` DISABLE KEYS */;
INSERT INTO `notifications` VALUES (214,2,116,'invoice_approval_required','Neue Rechnung zur Freigabe','Rechnung FAT-001-2026 von Azienda Demo SRL √ºber 300,00 ‚Ç¨ EUR wartet auf Ihre Freigabe.',0,'normal','/invoices/116','2026-01-22 13:33:29',NULL),(215,3,116,'invoice_received','Neue Rechnung eingegangen','Rechnung FAT-001-2026 von Azienda Demo SRL √ºber 300,00 ‚Ç¨ EUR ist eingegangen.',0,'low','/invoices/116','2026-01-22 13:33:29',NULL),(216,1,116,'invoice_received','Neue Rechnung eingegangen','Rechnung FAT-001-2026 von Azienda Demo SRL √ºber 300,00 ‚Ç¨ EUR ist eingegangen.',0,'normal','/invoices/116','2026-01-22 13:33:29',NULL),(217,5,117,'invoice_approval_required','Neue Rechnung zur Freigabe','Rechnung FAT-002-2026 von Azienda Demo SRL √ºber 1.250,00 ‚Ç¨ EUR wartet auf Ihre Freigabe.',0,'normal','/invoices/117','2026-01-22 13:33:55',NULL),(218,3,117,'invoice_received','Neue Rechnung eingegangen','Rechnung FAT-002-2026 von Azienda Demo SRL √ºber 1.250,00 ‚Ç¨ EUR ist eingegangen.',0,'low','/invoices/117','2026-01-22 13:33:55',NULL),(219,1,117,'invoice_received','Neue Rechnung eingegangen','Rechnung FAT-002-2026 von Azienda Demo SRL √ºber 1.250,00 ‚Ç¨ EUR ist eingegangen.',0,'normal','/invoices/117','2026-01-22 13:33:55',NULL),(220,2,117,'invoice_received','Neue Rechnung eingegangen','Rechnung FAT-002-2026 von Azienda Demo SRL √ºber 1.250,00 ‚Ç¨ EUR ist eingegangen.',0,'normal','/invoices/117','2026-01-22 13:33:55',NULL);
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
  `status` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT 'Geplant',
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
INSERT INTO `projects` VALUES ('10000','Fuhrpark-Reparaturen','','FLEET',0.00,0.00,'Aktiv',NULL,NULL,NULL,'2025-12-20 14:24:13'),('HR002','Mitarbeiter-Portal','Entwicklung eines Self-Service Portals f√ºr Mitarbeiter','HR',15000.00,0.00,'Geplant',NULL,NULL,3,'2025-12-12 09:31:17'),('OFF004','B√ºroausstattung 2024','Modernisierung der B√ºroausstattung','OFFICE',5000.00,0.00,'Aktiv',NULL,NULL,1,'2025-12-12 09:31:17'),('SALES003','CRM System','Einf√ºhrung eines neuen Customer Relationship Management Systems','SALES',40000.00,0.00,'Aktiv',NULL,NULL,2,'2025-12-12 09:31:17'),('WEB001','Website Relaunch','Neugestaltung der Unternehmenswebsite','IT',25000.00,0.00,'Aktiv',NULL,NULL,4,'2025-12-12 09:31:17');
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
  `status` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT 'Offen',
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
INSERT INTO `roles` VALUES (1,'Administrator','System Administrator mit allen Rechten','#F44336',1),(2,'Freigeber','Kann Rechnungen freigeben und ablehnen','#00bfff',0),(3,'Buchhaltung','Kann alle Rechnungen einsehen und bearbeiten','#4CAF50',1),(4,'Mitarbeiter','Kann eigene Rechnungen einsehen','#795548',0),(5,'Controller','Kann Reports erstellen und Budgets √ºberwachen','#fbff00',0),(6,'Manager','Kann Kostenstellen-bezogene Rechnungen freigeben','#FF9800',0);
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
INSERT INTO `system_config` VALUES (1,'company_name','Musterfirma GmbH','string','Name der Firma',1,NULL,'2025-12-12 09:31:17'),(2,'default_currency','EUR','string','Standard-W√§hrung',1,NULL,'2025-12-12 09:31:17'),(3,'auto_approval_limit','50','number','Automatische Freigabe-Grenze in EUR',1,NULL,'2025-12-12 09:31:17'),(4,'payment_terms_days','30','number','Standard-Zahlungsziel in Tagen',1,NULL,'2025-12-12 09:31:17'),(5,'notification_email_enabled','true','boolean','E-Mail-Benachrichtigungen aktiviert',1,NULL,'2025-12-12 09:31:17'),(6,'pdf_storage_path','/uploads/invoices/','string','Pfad f√ºr PDF-Dateien',1,NULL,'2025-12-12 09:31:17'),(7,'max_file_size_mb','10','number','Maximale Dateigr√∂√üe in MB',1,NULL,'2025-12-12 09:31:17'),(8,'ad_integration_enabled','true','boolean','Active Directory Integration aktiviert',1,NULL,'2025-12-12 09:31:17'),(9,'backup_retention_days','365','number','Aufbewahrungszeit f√ºr Backups in Tagen',1,NULL,'2025-12-12 09:31:17');
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
INSERT INTO `user_roles` VALUES (1,1,'2025-12-12 09:31:16'),(2,1,'2026-01-22 13:29:29'),(2,2,'2026-01-22 13:29:29'),(3,3,'2025-12-12 09:31:16'),(4,6,'2025-12-12 09:31:16'),(5,2,'2025-12-21 15:18:51'),(5,4,'2025-12-21 15:18:51');
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
INSERT INTO `users` VALUES (1,'admin','$2a$11$OsB0yW6RMM/44lEKraS1gOdLpCqz.s6j2oIQH1x6/x0VOTbi3ly5.','admin@firma.de','System','Administrator',NULL,'2025-12-12 09:31:16',0,NULL,1,'2025-12-12 09:31:16','2026-01-27 07:53:49','2026-01-27 07:53:49'),(2,'max.mustermann','$2a$11$mAAO7nsfAEm8DkyXs.2Bmey1OYdC7pNza3wFjyZuF/MTtEPriiuFO','max.mustermann@firma.de','Max','Mustermann',NULL,'2025-12-12 09:31:16',0,NULL,1,'2025-12-12 09:31:16','2026-01-22 13:29:29',NULL),(3,'maria.mueller','$2a$11$eGjbz1.YRSQ5tcJWXwmeZu5S/cCDqg2PUG68XthzP5NwOiG61oaRK','maria.mueller@firma.de','Maria','M√ºller',NULL,'2025-12-12 09:31:16',0,NULL,1,'2025-12-12 09:31:16','2025-12-12 08:39:48',NULL),(4,'hans.schmidt','$2a$11$0.kYmWmgLz8OcAou3aysd.snnN/IqV3u/b9n3EtqX.gVe0OhPIL8W','hans.schmidt@firma.de','Hans','Schmidt',NULL,'2025-12-12 09:31:16',0,NULL,1,'2025-12-12 09:31:16','2025-12-12 08:39:48',NULL),(5,'lisa.klein','$2a$11$eIKzA9xWOS.XpIv3P83lOu7wLQNEJeCjYO2LFd18BLrtx0QIdTv.6','lisa.klein@firma.de','Lisa','Klein',NULL,'2025-12-12 09:31:16',0,NULL,1,'2025-12-12 09:31:16','2026-01-21 13:36:17',NULL);
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

-- Dump completed on 2026-01-27  9:56:24
