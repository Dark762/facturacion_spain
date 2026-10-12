-- =====================================================================
-- Facturacion SaaS - Esquema completo e idempotente para MYSQL
--
-- Script UNICO de despliegue: crea el esquema Y aplica el endurecimiento
-- de facturas. No hay un segundo archivo que recordar.
--
--   CREATE DATABASE facturacion_saas CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
--     mysql -u root -p facturacion_saas < 01_full_schema.sql
--
-- IDEMPOTENTE: se puede reaplicar sin error.
--
-- ADVERTENCIA: es DESTRUCTIVO. Empieza droppeando las tablas, asi que
-- ejecutar este script sobre una base con datos los borra. Es un script de
-- instalacion, no de actualizacion.
--
-- Contenido:
--   1. Tablas, indices y claves foraneas
--   2. Restricciones CHECK e inmutabilidad de facturas emitidas
--
-- Verificado ejecutandolo contra un servidor MYSQL real.
-- =====================================================================

/*M!999999\- enable the sandbox mode */ 
-- MariaDB dump 10.20-13.0.2-MariaDB, for Win64 (AMD64)
--
-- Host: 127.0.0.1    Database: facturacion_saas
-- ------------------------------------------------------
-- Server version	13.0.2-MariaDB

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*M!100616 SET @OLD_NOTE_VERBOSITY=@@NOTE_VERBOSITY, NOTE_VERBOSITY=0 */;

--
-- Current Database: `facturacion_saas`
--

--
-- Table structure for table `aeatsubmissions`
--

DROP TABLE IF EXISTS `aeatsubmissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `aeatsubmissions` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `FiscalRecordId` char(36) NOT NULL,
  `Environment` varchar(20) NOT NULL,
  `SubmittedAt` datetime(6) NOT NULL,
  `Status` int(11) NOT NULL,
  `Csv` varchar(150) DEFAULT NULL,
  `ErrorCode` varchar(80) DEFAULT NULL,
  `ErrorDescription` varchar(1000) DEFAULT NULL,
  `RequestXml` longtext NOT NULL,
  `ResponseXml` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_AeatSubmissions_TenantId_Id` (`TenantId`,`Id`),
  KEY `FK_AeatSubmissions_FiscalRecords_0` (`TenantId`,`FiscalRecordId`),
  CONSTRAINT `FK_AeatSubmissions_FiscalRecords_0` FOREIGN KEY (`TenantId`, `FiscalRecordId`) REFERENCES `fiscalrecords` (`TenantId`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aeatsubmissions`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `aeatsubmissions` WRITE;
/*!40000 ALTER TABLE `aeatsubmissions` DISABLE KEYS */;
/*!40000 ALTER TABLE `aeatsubmissions` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `aspnetroleclaims`
--

DROP TABLE IF EXISTS `aspnetroleclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetroleclaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `RoleId` varchar(255) NOT NULL,
  `ClaimType` longtext DEFAULT NULL,
  `ClaimValue` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_RoleClaims_RoleId` (`RoleId`),
  CONSTRAINT `1` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetroleclaims`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `aspnetroleclaims` WRITE;
/*!40000 ALTER TABLE `aspnetroleclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetroleclaims` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `aspnetroles`
--

DROP TABLE IF EXISTS `aspnetroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetroles` (
  `Id` varchar(255) NOT NULL,
  `Name` varchar(256) DEFAULT NULL,
  `NormalizedName` varchar(256) DEFAULT NULL,
  `ConcurrencyStamp` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetroles`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `aspnetroles` WRITE;
/*!40000 ALTER TABLE `aspnetroles` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetroles` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `aspnetuserclaims`
--

DROP TABLE IF EXISTS `aspnetuserclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserclaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` varchar(255) NOT NULL,
  `ClaimType` longtext DEFAULT NULL,
  `ClaimValue` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_UserClaims_UserId` (`UserId`),
  CONSTRAINT `1` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserclaims`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `aspnetuserclaims` WRITE;
/*!40000 ALTER TABLE `aspnetuserclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetuserclaims` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `aspnetuserlogins`
--

DROP TABLE IF EXISTS `aspnetuserlogins`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserlogins` (
  `LoginProvider` varchar(255) NOT NULL,
  `ProviderKey` varchar(255) NOT NULL,
  `ProviderDisplayName` longtext DEFAULT NULL,
  `UserId` varchar(255) NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  KEY `IX_UserLogins_UserId` (`UserId`),
  CONSTRAINT `1` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserlogins`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `aspnetuserlogins` WRITE;
/*!40000 ALTER TABLE `aspnetuserlogins` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetuserlogins` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `aspnetuserroles`
--

DROP TABLE IF EXISTS `aspnetuserroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserroles` (
  `UserId` varchar(255) NOT NULL,
  `RoleId` varchar(255) NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IX_UserRoles_RoleId` (`RoleId`),
  CONSTRAINT `1` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `2` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserroles`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `aspnetuserroles` WRITE;
/*!40000 ALTER TABLE `aspnetuserroles` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetuserroles` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `aspnetusers`
--

DROP TABLE IF EXISTS `aspnetusers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetusers` (
  `Id` varchar(255) NOT NULL,
  `UserName` varchar(256) DEFAULT NULL,
  `NormalizedUserName` varchar(256) DEFAULT NULL,
  `Email` varchar(256) DEFAULT NULL,
  `NormalizedEmail` varchar(256) DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `PasswordHash` longtext DEFAULT NULL,
  `SecurityStamp` longtext DEFAULT NULL,
  `ConcurrencyStamp` longtext DEFAULT NULL,
  `PhoneNumber` longtext DEFAULT NULL,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `LockoutEnd` datetime(6) DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL,
  `AccessFailedCount` int(11) NOT NULL,
  `DisplayName` longtext NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
  KEY `EmailIndex` (`NormalizedEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetusers`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `aspnetusers` WRITE;
/*!40000 ALTER TABLE `aspnetusers` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetusers` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `aspnetusertokens`
--

DROP TABLE IF EXISTS `aspnetusertokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetusertokens` (
  `UserId` varchar(255) NOT NULL,
  `LoginProvider` varchar(255) NOT NULL,
  `Name` varchar(255) NOT NULL,
  `Value` longtext DEFAULT NULL,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
  CONSTRAINT `1` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetusertokens`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `aspnetusertokens` WRITE;
/*!40000 ALTER TABLE `aspnetusertokens` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetusertokens` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `auditevents`
--

DROP TABLE IF EXISTS `auditevents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `auditevents` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `CompanyId` char(36) NOT NULL,
  `EntityType` varchar(100) NOT NULL,
  `EntityId` varchar(100) NOT NULL,
  `Action` varchar(100) NOT NULL,
  `UserId` varchar(450) DEFAULT NULL,
  `OccurredAt` datetime(6) NOT NULL,
  `PreviousValues` longtext DEFAULT NULL,
  `NewValues` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_AuditEvents_TenantId_Id` (`TenantId`,`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `auditevents`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `auditevents` WRITE;
/*!40000 ALTER TABLE `auditevents` DISABLE KEYS */;
/*!40000 ALTER TABLE `auditevents` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `b2btransmissions`
--

DROP TABLE IF EXISTS `b2btransmissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `b2btransmissions` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `ElectronicDocumentId` char(36) NOT NULL,
  `Provider` varchar(100) NOT NULL,
  `ExternalId` varchar(150) DEFAULT NULL,
  `Status` int(11) NOT NULL,
  `SubmittedAt` datetime(6) DEFAULT NULL,
  `AcknowledgedAt` datetime(6) DEFAULT NULL,
  `RequestPayload` longtext DEFAULT NULL,
  `ResponsePayload` longtext DEFAULT NULL,
  `ErrorCode` varchar(80) DEFAULT NULL,
  `ErrorDescription` varchar(1000) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_B2BTransmissions_TenantId_Id` (`TenantId`,`Id`),
  KEY `FK_B2BTransmissions_ElectronicDocuments_0` (`TenantId`,`ElectronicDocumentId`),
  CONSTRAINT `FK_B2BTransmissions_ElectronicDocuments_0` FOREIGN KEY (`TenantId`, `ElectronicDocumentId`) REFERENCES `electronicdocuments` (`TenantId`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `b2btransmissions`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `b2btransmissions` WRITE;
/*!40000 ALTER TABLE `b2btransmissions` DISABLE KEYS */;
/*!40000 ALTER TABLE `b2btransmissions` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `companys`
--

DROP TABLE IF EXISTS `companys`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `companys` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `Nif` varchar(20) NOT NULL,
  `LegalName` varchar(250) NOT NULL,
  `TradeName` varchar(250) DEFAULT NULL,
  `Address` varchar(500) NOT NULL,
  `PostalCode` varchar(16) NOT NULL,
  `City` varchar(120) NOT NULL,
  `Province` varchar(120) NOT NULL,
  `CountryCode` varchar(2) NOT NULL,
  `Email` varchar(254) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_Companys_TenantId_Id` (`TenantId`,`Id`),
  UNIQUE KEY `UX_Companys_0` (`TenantId`,`Nif`),
  CONSTRAINT `FK_Companys_Tenants_0` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `companys`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `companys` WRITE;
/*!40000 ALTER TABLE `companys` DISABLE KEYS */;
INSERT INTO `companys` VALUES
('632eaaa9-fe08-4603-8d8c-56b0da8b407a','3b9dbb67-b6bb-41b4-9236-a59232f4aef9','Bee0270d6','Empresa Live SL',NULL,'','','','','ES',NULL,1);
/*!40000 ALTER TABLE `companys` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `companyusers`
--

DROP TABLE IF EXISTS `companyusers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `companyusers` (
  `Id` char(36) NOT NULL,
  `TenantId` char(36) NOT NULL,
  `CompanyId` char(36) NOT NULL,
  `UserId` varchar(255) NOT NULL,
  `Role` varchar(32) NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_CompanyUsers` (`TenantId`,`CompanyId`,`UserId`),
  KEY `TenantId` (`TenantId`,`UserId`),
  KEY `UserId` (`UserId`),
  CONSTRAINT `1` FOREIGN KEY (`TenantId`, `CompanyId`) REFERENCES `companys` (`TenantId`, `Id`),
  CONSTRAINT `2` FOREIGN KEY (`TenantId`, `UserId`) REFERENCES `tenantusers` (`TenantId`, `UserId`),
  CONSTRAINT `3` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `companyusers`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `companyusers` WRITE;
/*!40000 ALTER TABLE `companyusers` DISABLE KEYS */;
/*!40000 ALTER TABLE `companyusers` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `customers`
--

DROP TABLE IF EXISTS `customers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `customers` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `CompanyId` char(36) NOT NULL,
  `TaxId` varchar(20) NOT NULL,
  `LegalName` varchar(250) NOT NULL,
  `Address` varchar(500) NOT NULL,
  `PostalCode` varchar(16) NOT NULL,
  `City` varchar(120) NOT NULL,
  `CountryCode` varchar(2) NOT NULL,
  `Email` varchar(254) DEFAULT NULL,
  `IsBusiness` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_Customers_TenantId_Id` (`TenantId`,`Id`),
  KEY `FK_Customers_Companys_0` (`TenantId`,`CompanyId`),
  CONSTRAINT `FK_Customers_Companys_0` FOREIGN KEY (`TenantId`, `CompanyId`) REFERENCES `companys` (`TenantId`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `customers`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `customers` WRITE;
/*!40000 ALTER TABLE `customers` DISABLE KEYS */;
INSERT INTO `customers` VALUES
('632eaaa9-fe08-4603-8d8c-56b0da8b407a','f877c8ed-2bfd-4dc1-8708-7e36743e8762','3b9dbb67-b6bb-41b4-9236-a59232f4aef9','Bfc00319b','Cliente Live SL','','','','ES',NULL,1);
/*!40000 ALTER TABLE `customers` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `electronicdocuments`
--

DROP TABLE IF EXISTS `electronicdocuments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `electronicdocuments` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `CompanyId` char(36) NOT NULL,
  `InvoiceId` char(36) DEFAULT NULL,
  `Direction` int(11) NOT NULL,
  `Format` varchar(50) NOT NULL,
  `FormatVersion` varchar(30) NOT NULL,
  `DocumentNumber` varchar(80) NOT NULL,
  `XmlPayload` longtext NOT NULL,
  `ContentHash` varchar(128) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_ElectronicDocuments_TenantId_Id` (`TenantId`,`Id`),
  KEY `FK_ElectronicDocuments_Invoices_0` (`TenantId`,`InvoiceId`),
  CONSTRAINT `FK_ElectronicDocuments_Invoices_0` FOREIGN KEY (`TenantId`, `InvoiceId`) REFERENCES `invoices` (`TenantId`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `electronicdocuments`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `electronicdocuments` WRITE;
/*!40000 ALTER TABLE `electronicdocuments` DISABLE KEYS */;
/*!40000 ALTER TABLE `electronicdocuments` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `fiscalchains`
--

DROP TABLE IF EXISTS `fiscalchains`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `fiscalchains` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `CompanyId` char(36) NOT NULL,
  `InstallationId` varchar(100) NOT NULL,
  `LastRecordId` char(36) DEFAULT NULL,
  `LastHash` varchar(64) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_FiscalChains_TenantId_Id` (`TenantId`,`Id`),
  UNIQUE KEY `UX_FiscalChains_0` (`TenantId`,`CompanyId`,`InstallationId`),
  CONSTRAINT `FK_FiscalChains_Companys_0` FOREIGN KEY (`TenantId`, `CompanyId`) REFERENCES `companys` (`TenantId`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `fiscalchains`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `fiscalchains` WRITE;
/*!40000 ALTER TABLE `fiscalchains` DISABLE KEYS */;
/*!40000 ALTER TABLE `fiscalchains` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `fiscalrecords`
--

DROP TABLE IF EXISTS `fiscalrecords`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `fiscalrecords` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `InvoiceId` char(36) NOT NULL,
  `RecordType` int(11) NOT NULL,
  `PreviousRecordId` char(36) DEFAULT NULL,
  `PreviousHash` varchar(64) NOT NULL,
  `Hash` varchar(64) NOT NULL,
  `GeneratedAt` datetime(6) NOT NULL,
  `SoftwareName` varchar(200) NOT NULL,
  `SoftwareVersion` varchar(50) NOT NULL,
  `InstallationId` varchar(100) NOT NULL,
  `XmlPayload` longtext NOT NULL,
  `SubmissionStatus` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_FiscalRecords_TenantId_Id` (`TenantId`,`Id`),
  KEY `FK_FiscalRecords_Invoices_0` (`TenantId`,`InvoiceId`),
  CONSTRAINT `FK_FiscalRecords_Invoices_0` FOREIGN KEY (`TenantId`, `InvoiceId`) REFERENCES `invoices` (`TenantId`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `fiscalrecords`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `fiscalrecords` WRITE;
/*!40000 ALTER TABLE `fiscalrecords` DISABLE KEYS */;
/*!40000 ALTER TABLE `fiscalrecords` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `invoicelines`
--

DROP TABLE IF EXISTS `invoicelines`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `invoicelines` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `InvoiceId` char(36) NOT NULL,
  `LineNumber` int(11) NOT NULL,
  `Description` varchar(1000) NOT NULL,
  `Quantity` decimal(18,6) NOT NULL,
  `UnitPrice` decimal(18,6) NOT NULL,
  `DiscountPercent` decimal(9,4) NOT NULL,
  `VatRate` decimal(9,4) NOT NULL,
  `NetAmount` decimal(18,2) NOT NULL,
  `VatAmount` decimal(18,2) NOT NULL,
  `TotalAmount` decimal(18,2) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_InvoiceLines_TenantId_Id` (`TenantId`,`Id`),
  KEY `FK_InvoiceLines_Invoices_0` (`TenantId`,`InvoiceId`),
  CONSTRAINT `FK_InvoiceLines_Invoices_0` FOREIGN KEY (`TenantId`, `InvoiceId`) REFERENCES `invoices` (`TenantId`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invoicelines`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `invoicelines` WRITE;
/*!40000 ALTER TABLE `invoicelines` DISABLE KEYS */;
INSERT INTO `invoicelines` VALUES
('632eaaa9-fe08-4603-8d8c-56b0da8b407a','32970e20-a1c9-4e78-b5f5-6051548cec77','95a31dbb-5922-441a-9af9-66e0e2fa4f70',1,'Linea 3',2.000000,50.000000,0.0000,21.0000,100.00,21.00,121.00),
('632eaaa9-fe08-4603-8d8c-56b0da8b407a','93bd73ba-61cb-4173-bf81-4713449cc256','32e795fb-a331-4617-a8a0-afec36d0ec47',1,'Linea 2',2.000000,50.000000,0.0000,21.0000,100.00,21.00,121.00),
('632eaaa9-fe08-4603-8d8c-56b0da8b407a','a0dfaf21-086f-4a71-b895-8408437484e0','ae0243a1-3921-448e-84f4-f3c7ea497388',1,'Linea 1',2.000000,50.000000,0.0000,21.0000,100.00,21.00,121.00);
/*!40000 ALTER TABLE `invoicelines` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `invoicepayments`
--

DROP TABLE IF EXISTS `invoicepayments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `invoicepayments` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `InvoiceId` char(36) NOT NULL,
  `Amount` decimal(18,2) NOT NULL,
  `PaymentDate` date NOT NULL,
  `PaymentMethod` varchar(60) NOT NULL,
  `ExternalReference` varchar(200) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_InvoicePayments_TenantId_Id` (`TenantId`,`Id`),
  KEY `FK_InvoicePayments_Invoices_0` (`TenantId`,`InvoiceId`),
  CONSTRAINT `FK_InvoicePayments_Invoices_0` FOREIGN KEY (`TenantId`, `InvoiceId`) REFERENCES `invoices` (`TenantId`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invoicepayments`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `invoicepayments` WRITE;
/*!40000 ALTER TABLE `invoicepayments` DISABLE KEYS */;
/*!40000 ALTER TABLE `invoicepayments` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `invoices`
--

DROP TABLE IF EXISTS `invoices`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `invoices` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `CompanyId` char(36) NOT NULL,
  `CustomerId` char(36) NOT NULL,
  `Series` varchar(32) DEFAULT NULL,
  `Number` varchar(40) DEFAULT NULL,
  `Type` int(11) NOT NULL,
  `Status` int(11) NOT NULL,
  `IssueDate` date NOT NULL,
  `OperationDate` date DEFAULT NULL,
  `DueDate` date DEFAULT NULL,
  `Currency` varchar(3) NOT NULL,
  `TaxableBase` decimal(18,2) NOT NULL,
  `TaxAmount` decimal(18,2) NOT NULL,
  `WithholdingAmount` decimal(18,2) NOT NULL,
  `Total` decimal(18,2) NOT NULL,
  `IssuerNif` varchar(20) NOT NULL,
  `IssuerName` varchar(250) NOT NULL,
  `CustomerTaxId` varchar(20) NOT NULL,
  `CustomerName` varchar(250) NOT NULL,
  `CorrectedInvoiceId` char(36) DEFAULT NULL,
  `IssuedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_Invoices_TenantId_Id` (`TenantId`,`Id`),
  UNIQUE KEY `UX_Invoices_0` (`TenantId`,`CompanyId`,`Series`,`Number`),
  KEY `FK_Invoices_Customers_1` (`TenantId`,`CustomerId`),
  CONSTRAINT `FK_Invoices_Companys_0` FOREIGN KEY (`TenantId`, `CompanyId`) REFERENCES `companys` (`TenantId`, `Id`),
  CONSTRAINT `FK_Invoices_Customers_1` FOREIGN KEY (`TenantId`, `CustomerId`) REFERENCES `customers` (`TenantId`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invoices`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `invoices` WRITE;
/*!40000 ALTER TABLE `invoices` DISABLE KEYS */;
INSERT INTO `invoices` VALUES
('632eaaa9-fe08-4603-8d8c-56b0da8b407a','32e795fb-a331-4617-a8a0-afec36d0ec47','3b9dbb67-b6bb-41b4-9236-a59232f4aef9','f877c8ed-2bfd-4dc1-8708-7e36743e8762',NULL,NULL,1,0,'2026-03-15',NULL,NULL,'EUR',100.00,21.00,0.00,121.00,'Bee0270d6','Empresa Live SL','Bfc00319b','Cliente Live SL',NULL,NULL),
('632eaaa9-fe08-4603-8d8c-56b0da8b407a','95a31dbb-5922-441a-9af9-66e0e2fa4f70','3b9dbb67-b6bb-41b4-9236-a59232f4aef9','f877c8ed-2bfd-4dc1-8708-7e36743e8762',NULL,NULL,1,0,'2026-03-15',NULL,NULL,'EUR',100.00,21.00,0.00,121.00,'Bee0270d6','Empresa Live SL','Bfc00319b','Cliente Live SL',NULL,NULL),
('632eaaa9-fe08-4603-8d8c-56b0da8b407a','ae0243a1-3921-448e-84f4-f3c7ea497388','3b9dbb67-b6bb-41b4-9236-a59232f4aef9','f877c8ed-2bfd-4dc1-8708-7e36743e8762',NULL,NULL,1,0,'2026-03-15',NULL,NULL,'EUR',100.00,21.00,0.00,121.00,'Bee0270d6','Empresa Live SL','Bfc00319b','Cliente Live SL',NULL,NULL);
/*!40000 ALTER TABLE `invoices` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `invoicesequences`
--

DROP TABLE IF EXISTS `invoicesequences`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `invoicesequences` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `CompanyId` char(36) NOT NULL,
  `Series` varchar(32) NOT NULL,
  `FiscalYear` int(11) NOT NULL,
  `NextNumber` bigint(20) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_InvoiceSequences_TenantId_Id` (`TenantId`,`Id`),
  UNIQUE KEY `UX_InvoiceSequences_0` (`TenantId`,`CompanyId`,`Series`,`FiscalYear`),
  CONSTRAINT `FK_InvoiceSequences_Companys_0` FOREIGN KEY (`TenantId`, `CompanyId`) REFERENCES `companys` (`TenantId`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invoicesequences`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `invoicesequences` WRITE;
/*!40000 ALTER TABLE `invoicesequences` DISABLE KEYS */;
/*!40000 ALTER TABLE `invoicesequences` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `invoicestatusevents`
--

DROP TABLE IF EXISTS `invoicestatusevents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `invoicestatusevents` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `InvoiceId` char(36) NOT NULL,
  `EventType` varchar(100) NOT NULL,
  `Source` varchar(100) NOT NULL,
  `OccurredAt` datetime(6) NOT NULL,
  `ReportedAt` datetime(6) DEFAULT NULL,
  `ExternalReference` varchar(200) DEFAULT NULL,
  `Payload` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_InvoiceStatusEvents_TenantId_Id` (`TenantId`,`Id`),
  KEY `FK_InvoiceStatusEvents_Invoices_0` (`TenantId`,`InvoiceId`),
  CONSTRAINT `FK_InvoiceStatusEvents_Invoices_0` FOREIGN KEY (`TenantId`, `InvoiceId`) REFERENCES `invoices` (`TenantId`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invoicestatusevents`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `invoicestatusevents` WRITE;
/*!40000 ALTER TABLE `invoicestatusevents` DISABLE KEYS */;
/*!40000 ALTER TABLE `invoicestatusevents` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `invoicetaxbreakdowns`
--

DROP TABLE IF EXISTS `invoicetaxbreakdowns`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `invoicetaxbreakdowns` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `InvoiceId` char(36) NOT NULL,
  `TaxCode` varchar(20) NOT NULL,
  `RegimeKey` varchar(20) DEFAULT NULL,
  `OperationClassification` varchar(60) DEFAULT NULL,
  `ExemptionCode` varchar(30) DEFAULT NULL,
  `TaxRate` decimal(9,4) NOT NULL,
  `TaxableBase` decimal(18,2) NOT NULL,
  `TaxAmount` decimal(18,2) NOT NULL,
  `SurchargeRate` decimal(9,4) DEFAULT NULL,
  `SurchargeAmount` decimal(18,2) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_InvoiceTaxBreakdowns_TenantId_Id` (`TenantId`,`Id`),
  KEY `FK_InvoiceTaxBreakdowns_Invoices_0` (`TenantId`,`InvoiceId`),
  CONSTRAINT `FK_InvoiceTaxBreakdowns_Invoices_0` FOREIGN KEY (`TenantId`, `InvoiceId`) REFERENCES `invoices` (`TenantId`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invoicetaxbreakdowns`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `invoicetaxbreakdowns` WRITE;
/*!40000 ALTER TABLE `invoicetaxbreakdowns` DISABLE KEYS */;
INSERT INTO `invoicetaxbreakdowns` VALUES
('632eaaa9-fe08-4603-8d8c-56b0da8b407a','3ceb759c-c4f9-442e-975b-96c2be0bd04f','ae0243a1-3921-448e-84f4-f3c7ea497388','IVA',NULL,NULL,NULL,21.0000,100.00,21.00,NULL,NULL),
('632eaaa9-fe08-4603-8d8c-56b0da8b407a','c65847dd-8055-4544-abb4-d34e7f591788','32e795fb-a331-4617-a8a0-afec36d0ec47','IVA',NULL,NULL,NULL,21.0000,100.00,21.00,NULL,NULL),
('632eaaa9-fe08-4603-8d8c-56b0da8b407a','cbe1f528-5006-4325-aa59-c5f77d986604','95a31dbb-5922-441a-9af9-66e0e2fa4f70','IVA',NULL,NULL,NULL,21.0000,100.00,21.00,NULL,NULL);
/*!40000 ALTER TABLE `invoicetaxbreakdowns` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `outboxmessages`
--

DROP TABLE IF EXISTS `outboxmessages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `outboxmessages` (
  `TenantId` char(36) NOT NULL,
  `Id` char(36) NOT NULL,
  `Type` varchar(100) NOT NULL,
  `AggregateId` varchar(100) NOT NULL,
  `Payload` longtext NOT NULL,
  `Status` varchar(32) NOT NULL,
  `Attempts` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `NextAttemptAt` datetime(6) DEFAULT NULL,
  `ProcessedAt` datetime(6) DEFAULT NULL,
  `LastError` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_OutboxMessages_TenantId_Id` (`TenantId`,`Id`),
  KEY `IX_OutboxMessage_Pending` (`TenantId`,`Status`,`NextAttemptAt`)
  -- El worker reclama por Status sin filtrar por tenant: este indice es el que lo sostiene.
  KEY `IX_OutboxMessages_Status_NextAttemptAt` (`Status`,`NextAttemptAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `outboxmessages`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `outboxmessages` WRITE;
/*!40000 ALTER TABLE `outboxmessages` DISABLE KEYS */;
/*!40000 ALTER TABLE `outboxmessages` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `processedpaymentevents`
--

DROP TABLE IF EXISTS `processedpaymentevents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `processedpaymentevents` (
  `Id` varchar(255) NOT NULL,
  `ReceivedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `processedpaymentevents`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `processedpaymentevents` WRITE;
/*!40000 ALTER TABLE `processedpaymentevents` DISABLE KEYS */;
/*!40000 ALTER TABLE `processedpaymentevents` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `provisioningoperations`
--

DROP TABLE IF EXISTS `provisioningoperations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `provisioningoperations` (
  `RegistrationId` char(36) NOT NULL,
  `TenantId` char(36) DEFAULT NULL,
  `CompanyId` char(36) DEFAULT NULL,
  `Status` varchar(32) NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `UpdatedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`RegistrationId`),
  KEY `FK_ProvisioningOperations_Tenant` (`TenantId`),
  KEY `FK_ProvisioningOperations_Company` (`CompanyId`),
  CONSTRAINT `FK_ProvisioningOperations_Company` FOREIGN KEY (`CompanyId`) REFERENCES `companys` (`Id`),
  CONSTRAINT `FK_ProvisioningOperations_Tenant` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `provisioningoperations`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `provisioningoperations` WRITE;
/*!40000 ALTER TABLE `provisioningoperations` DISABLE KEYS */;
/*!40000 ALTER TABLE `provisioningoperations` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `registrationintents`
--

DROP TABLE IF EXISTS `registrationintents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `registrationintents` (
  `Id` char(36) NOT NULL,
  `UserId` varchar(255) NOT NULL,
  `TenantName` varchar(250) NOT NULL,
  `Nif` varchar(30) NOT NULL,
  `LegalName` varchar(250) NOT NULL,
  `Address` varchar(500) NOT NULL,
  `PostalCode` varchar(20) NOT NULL,
  `City` varchar(150) NOT NULL,
  `Province` varchar(150) NOT NULL,
  `PlanCode` varchar(64) NOT NULL DEFAULT '',
  `CheckoutSessionId` varchar(255) DEFAULT NULL,
  `CheckoutTokenHash` varchar(64) DEFAULT NULL,
  `TenantId` char(36) DEFAULT NULL,
  `Status` varchar(40) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_RegistrationIntents_UserId` (`UserId`),
  UNIQUE KEY `UX_RegistrationIntents_Session` (`CheckoutSessionId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `registrationintents`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `registrationintents` WRITE;
/*!40000 ALTER TABLE `registrationintents` DISABLE KEYS */;
/*!40000 ALTER TABLE `registrationintents` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `subscriptionplans`
--

DROP TABLE IF EXISTS `subscriptionplans`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `subscriptionplans` (
  `Code` varchar(64) NOT NULL,
  `Name` varchar(150) NOT NULL,
  `StripePriceId` varchar(255) NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Code`),
  UNIQUE KEY `UX_SubscriptionPlans_Price` (`StripePriceId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `subscriptionplans`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `subscriptionplans` WRITE;
/*!40000 ALTER TABLE `subscriptionplans` DISABLE KEYS */;
/*!40000 ALTER TABLE `subscriptionplans` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `subscriptionstates`
--

DROP TABLE IF EXISTS `subscriptionstates`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `subscriptionstates` (
  `Id` char(36) NOT NULL,
  `TenantId` char(36) NOT NULL,
  `StripeCustomerId` varchar(255) NOT NULL,
  `StripeSubscriptionId` varchar(255) NOT NULL,
  `PlanCode` varchar(64) NOT NULL,
  `Status` varchar(40) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_SubscriptionStates_Tenant` (`TenantId`),
  UNIQUE KEY `UX_SubscriptionStates_StripeSubscription` (`StripeSubscriptionId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `subscriptionstates`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `subscriptionstates` WRITE;
/*!40000 ALTER TABLE `subscriptionstates` DISABLE KEYS */;
/*!40000 ALTER TABLE `subscriptionstates` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `tenant_invitations`
--

DROP TABLE IF EXISTS `tenant_invitations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `tenant_invitations` (
  `Id` char(36) NOT NULL,
  `TenantId` char(36) NOT NULL,
  `CompanyId` char(36) NOT NULL,
  `Email` varchar(256) NOT NULL,
  `Role` varchar(20) NOT NULL,
  `TokenHash` char(64) NOT NULL,
  `InvitedByUserId` varchar(255) NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `ExpiresAtUtc` datetime(6) NOT NULL,
  `AcceptedAtUtc` datetime(6) DEFAULT NULL,
  `AcceptedByUserId` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_tenant_invitations_token` (`TokenHash`),
  KEY `IX_tenant_invitations_company` (`TenantId`,`CompanyId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tenant_invitations`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `tenant_invitations` WRITE;
/*!40000 ALTER TABLE `tenant_invitations` DISABLE KEYS */;
/*!40000 ALTER TABLE `tenant_invitations` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `tenants`
--

DROP TABLE IF EXISTS `tenants`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `tenants` (
  `Id` char(36) NOT NULL,
  `Name` varchar(200) NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tenants`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `tenants` WRITE;
/*!40000 ALTER TABLE `tenants` DISABLE KEYS */;
INSERT INTO `tenants` VALUES
('632eaaa9-fe08-4603-8d8c-56b0da8b407a','Prueba live',1);
/*!40000 ALTER TABLE `tenants` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;

--
-- Table structure for table `tenantusers`
--

DROP TABLE IF EXISTS `tenantusers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `tenantusers` (
  `Id` char(36) NOT NULL,
  `TenantId` char(36) NOT NULL,
  `UserId` varchar(255) NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_TenantUsers` (`TenantId`,`UserId`),
  KEY `UserId` (`UserId`),
  CONSTRAINT `1` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`),
  CONSTRAINT `2` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tenantusers`
--

SET @OLD_AUTOCOMMIT=@@AUTOCOMMIT, @@AUTOCOMMIT=0;
LOCK TABLES `tenantusers` WRITE;
/*!40000 ALTER TABLE `tenantusers` DISABLE KEYS */;
/*!40000 ALTER TABLE `tenantusers` ENABLE KEYS */;
UNLOCK TABLES;
COMMIT;
SET AUTOCOMMIT=@OLD_AUTOCOMMIT;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*M!100616 SET NOTE_VERBOSITY=@OLD_NOTE_VERBOSITY */;

-- Dump completed

ALTER TABLE `Invoices`
  MODIFY COLUMN `Series` VARCHAR(32) NULL,
  MODIFY COLUMN `Number` VARCHAR(40) NULL;

-- ---------------------------------------------------------------------
-- 2. Preflight: NO continuar si hay datos que violen las restricciones
-- ---------------------------------------------------------------------
-- Esta consulta debe devolver 0 filas. Si devuelve alguna, PARAR y
-- corregir los datos a mano: este script no repara facturas.
SELECT `Id`,`Status`,`Series`,`Number`,`IssuedAt` FROM `Invoices`
WHERE (`Series` IS NULL) <> (`Number` IS NULL)
   OR (`Series` IS NOT NULL AND TRIM(`Series`) = '')
   OR (`Number` IS NOT NULL AND TRIM(`Number`) = '')
   OR ((`IssuedAt` IS NOT NULL OR `Status` = 1)
       AND (`Series` IS NULL OR `Number` IS NULL));

-- ---------------------------------------------------------------------
-- 3. Objetos previos (para poder reejecutar)
-- ---------------------------------------------------------------------
DROP TRIGGER IF EXISTS `TR_Invoices_ProtectIssued_Update`;
DROP TRIGGER IF EXISTS `TR_Invoices_ProtectIssued_Delete`;
DROP TRIGGER IF EXISTS `TR_InvoiceLines_ProtectIssued_Insert`;
DROP TRIGGER IF EXISTS `TR_InvoiceLines_ProtectIssued_Update`;
DROP TRIGGER IF EXISTS `TR_InvoiceLines_ProtectIssued_Delete`;
DROP TRIGGER IF EXISTS `TR_InvoiceTaxBreakdowns_ProtectIssued_Insert`;
DROP TRIGGER IF EXISTS `TR_InvoiceTaxBreakdowns_ProtectIssued_Update`;
DROP TRIGGER IF EXISTS `TR_InvoiceTaxBreakdowns_ProtectIssued_Delete`;

SET @ck1 = (SELECT CONSTRAINT_NAME FROM information_schema.CHECK_CONSTRAINTS
            WHERE CONSTRAINT_SCHEMA = DATABASE() AND CONSTRAINT_NAME = 'CK_Invoices_NumberPair');
SET @sql1 = IF(@ck1 IS NULL, 'SELECT 1',
  CONCAT('ALTER TABLE `Invoices` DROP CHECK `CK_Invoices_NumberPair`'));
PREPARE s1 FROM @sql1; EXECUTE s1; DEALLOCATE PREPARE s1;

SET @ck2 = (SELECT CONSTRAINT_NAME FROM information_schema.CHECK_CONSTRAINTS
            WHERE CONSTRAINT_SCHEMA = DATABASE() AND CONSTRAINT_NAME = 'CK_Invoices_IssuedNumber');
SET @sql2 = IF(@ck2 IS NULL, 'SELECT 1',
  CONCAT('ALTER TABLE `Invoices` DROP CHECK `CK_Invoices_IssuedNumber`'));
PREPARE s2 FROM @sql2; EXECUTE s2; DEALLOCATE PREPARE s2;

-- ---------------------------------------------------------------------
-- 4. Restricciones CHECK
-- ---------------------------------------------------------------------
-- Serie y numero van juntos: ambos ausentes, o ambos presentes y no vacios.
ALTER TABLE `Invoices`
  ADD CONSTRAINT `CK_Invoices_NumberPair`
  CHECK ((`Series` IS NULL AND `Number` IS NULL) OR
         (`Series` IS NOT NULL AND `Number` IS NOT NULL
          AND TRIM(`Series`) <> '' AND TRIM(`Number`) <> ''));

-- Una factura emitida exige numeracion. Cubre Status=Issued e IssuedAt.
ALTER TABLE `Invoices`
  ADD CONSTRAINT `CK_Invoices_IssuedNumber`
  CHECK ((`Status` <> 1 AND `IssuedAt` IS NULL) OR
         (`Series` IS NOT NULL AND `Number` IS NOT NULL));

DELIMITER $$

-- ---------------------------------------------------------------------
-- 5. Cabecera: inmutabilidad de la factura emitida
--    Unico campo mutable: Status, y solo hacia adelante.
-- ---------------------------------------------------------------------
CREATE TRIGGER `TR_Invoices_ProtectIssued_Update` BEFORE UPDATE ON `Invoices`
FOR EACH ROW BEGIN
 DECLARE v_old_issued BOOLEAN DEFAULT (OLD.`IssuedAt` IS NOT NULL OR OLD.`Status` = 1);
 -- Volver a borrador seria deshacer una numeracion ya comunicada.
 IF v_old_issued AND NEW.`Status` = 0 THEN
   SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'An issued invoice cannot return to draft';
 END IF;
 -- Todo lo demas congelado. Si se anade una columna a Invoices, debe
 -- anadirse tambien aqui para que quede protegida por defecto.
 IF v_old_issued AND (
 NOT (OLD.`Id`                 <=> NEW.`Id`)                 OR
 NOT (OLD.`TenantId`           <=> NEW.`TenantId`)           OR
 NOT (OLD.`CompanyId`          <=> NEW.`CompanyId`)          OR
 NOT (OLD.`CustomerId`         <=> NEW.`CustomerId`)         OR
 NOT (OLD.`Series`             <=> NEW.`Series`)             OR
 NOT (OLD.`Number`             <=> NEW.`Number`)             OR
 NOT (OLD.`Type`               <=> NEW.`Type`)               OR
 NOT (OLD.`IssueDate`          <=> NEW.`IssueDate`)          OR
 NOT (OLD.`OperationDate`      <=> NEW.`OperationDate`)      OR
 NOT (OLD.`DueDate`            <=> NEW.`DueDate`)            OR
 NOT (OLD.`Currency`           <=> NEW.`Currency`)           OR
 NOT (OLD.`TaxableBase`        <=> NEW.`TaxableBase`)        OR
 NOT (OLD.`TaxAmount`          <=> NEW.`TaxAmount`)          OR
 NOT (OLD.`WithholdingAmount`  <=> NEW.`WithholdingAmount`)  OR
 NOT (OLD.`Total`              <=> NEW.`Total`)              OR
 NOT (OLD.`IssuerNif`          <=> NEW.`IssuerNif`)          OR
 NOT (OLD.`IssuerName`         <=> NEW.`IssuerName`)         OR
 NOT (OLD.`CustomerTaxId`      <=> NEW.`CustomerTaxId`)      OR
 NOT (OLD.`CustomerName`       <=> NEW.`CustomerName`)       OR
 NOT (OLD.`CorrectedInvoiceId` <=> NEW.`CorrectedInvoiceId`) OR
 NOT (OLD.`IssuedAt`           <=> NEW.`IssuedAt`)
 ) THEN
   SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Issued invoice fiscal fields are immutable';
 END IF;
END$$

CREATE TRIGGER `TR_Invoices_ProtectIssued_Delete` BEFORE DELETE ON `Invoices`
FOR EACH ROW BEGIN
 IF OLD.`IssuedAt` IS NOT NULL OR OLD.`Status` = 1 THEN
   SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Issued invoices cannot be deleted';
 END IF;
END$$

-- ---------------------------------------------------------------------
-- 6. Lineas: sin trigger sobre la cabecera, el detalle de una factura
--    emitida se podria reescribir o borrar por completo.
--    En UPDATE se comprueban las facturas padron ANTIGUA y NUEVA, para que
--    tampoco sirva mover una linea de un borrador a una emitida.
-- ---------------------------------------------------------------------
CREATE TRIGGER `TR_InvoiceLines_ProtectIssued_Insert` BEFORE INSERT ON `InvoiceLines`
FOR EACH ROW BEGIN
 IF EXISTS (SELECT 1 FROM `Invoices` i
            WHERE i.`TenantId` = NEW.`TenantId` AND i.`Id` = NEW.`InvoiceId`
              AND (i.`IssuedAt` IS NOT NULL OR i.`Status` = 1)) THEN
   SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Cannot add lines to an issued invoice';
 END IF;
END$$

CREATE TRIGGER `TR_InvoiceLines_ProtectIssued_Update` BEFORE UPDATE ON `InvoiceLines`
FOR EACH ROW BEGIN
 IF EXISTS (SELECT 1 FROM `Invoices` i
            WHERE ((i.`TenantId` = OLD.`TenantId` AND i.`Id` = OLD.`InvoiceId`)
                OR (i.`TenantId` = NEW.`TenantId` AND i.`Id` = NEW.`InvoiceId`))
              AND (i.`IssuedAt` IS NOT NULL OR i.`Status` = 1)) THEN
   SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Cannot modify lines of an issued invoice';
 END IF;
END$$

CREATE TRIGGER `TR_InvoiceLines_ProtectIssued_Delete` BEFORE DELETE ON `InvoiceLines`
FOR EACH ROW BEGIN
 IF EXISTS (SELECT 1 FROM `Invoices` i
            WHERE i.`TenantId` = OLD.`TenantId` AND i.`Id` = OLD.`InvoiceId`
              AND (i.`IssuedAt` IS NOT NULL OR i.`Status` = 1)) THEN
   SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Cannot delete lines of an issued invoice';
 END IF;
END$$

-- ---------------------------------------------------------------------
-- 7. Desglose de impuestos: misma proteccion que las lineas
-- ---------------------------------------------------------------------
CREATE TRIGGER `TR_InvoiceTaxBreakdowns_ProtectIssued_Insert` BEFORE INSERT ON `InvoiceTaxBreakdowns`
FOR EACH ROW BEGIN
 IF EXISTS (SELECT 1 FROM `Invoices` i
            WHERE i.`TenantId` = NEW.`TenantId` AND i.`Id` = NEW.`InvoiceId`
              AND (i.`IssuedAt` IS NOT NULL OR i.`Status` = 1)) THEN
   SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Cannot add tax breakdown rows to an issued invoice';
 END IF;
END$$

CREATE TRIGGER `TR_InvoiceTaxBreakdowns_ProtectIssued_Update` BEFORE UPDATE ON `InvoiceTaxBreakdowns`
FOR EACH ROW BEGIN
 IF EXISTS (SELECT 1 FROM `Invoices` i
            WHERE ((i.`TenantId` = OLD.`TenantId` AND i.`Id` = OLD.`InvoiceId`)
                OR (i.`TenantId` = NEW.`TenantId` AND i.`Id` = NEW.`InvoiceId`))
              AND (i.`IssuedAt` IS NOT NULL OR i.`Status` = 1)) THEN
   SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Cannot modify tax breakdown rows of an issued invoice';
 END IF;
END$$

CREATE TRIGGER `TR_InvoiceTaxBreakdowns_ProtectIssued_Delete` BEFORE DELETE ON `InvoiceTaxBreakdowns`
FOR EACH ROW BEGIN
 IF EXISTS (SELECT 1 FROM `Invoices` i
            WHERE i.`TenantId` = OLD.`TenantId` AND i.`Id` = OLD.`InvoiceId`
              AND (i.`IssuedAt` IS NOT NULL OR i.`Status` = 1)) THEN
   SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Cannot delete tax breakdown rows of an issued invoice';
 END IF;
END$$

DELIMITER ;
