-- MariaDB 10.6+ | Esquema para el modelo de Facturacion
-- Ejecutar SOLO en una base nueva. Revisar contra el modelo final antes de producción.
CREATE DATABASE IF NOT EXISTS `facturacion_saas` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `facturacion_saas`;

CREATE TABLE IF NOT EXISTS `Tenants` (
  `Id` CHAR(36) NOT NULL,
  `Name` VARCHAR(200) NOT NULL,
  `IsActive` TINYINT(1) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Companys` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `Nif` VARCHAR(20) NOT NULL,
  `LegalName` VARCHAR(250) NOT NULL,
  `TradeName` VARCHAR(250) NULL,
  `Address` VARCHAR(500) NOT NULL,
  `PostalCode` VARCHAR(16) NOT NULL,
  `City` VARCHAR(120) NOT NULL,
  `Province` VARCHAR(120) NOT NULL,
  `CountryCode` VARCHAR(2) NOT NULL,
  `Email` VARCHAR(254) NULL,
  `IsActive` TINYINT(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_Companys_TenantId_Id` (`TenantId`,`Id`),
  UNIQUE KEY `UX_Companys_0` (`TenantId`,`Nif`),
  CONSTRAINT `FK_Companys_Tenants_0` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Customers` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `CompanyId` CHAR(36) NOT NULL,
  `TaxId` VARCHAR(20) NOT NULL,
  `LegalName` VARCHAR(250) NOT NULL,
  `Address` VARCHAR(500) NOT NULL,
  `PostalCode` VARCHAR(16) NOT NULL,
  `City` VARCHAR(120) NOT NULL,
  `CountryCode` VARCHAR(2) NOT NULL,
  `Email` VARCHAR(254) NULL,
  `IsBusiness` TINYINT(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_Customers_TenantId_Id` (`TenantId`,`Id`),
  CONSTRAINT `FK_Customers_Companys_0` FOREIGN KEY (`TenantId`,`CompanyId`) REFERENCES `Companys` (`TenantId`,`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Invoices` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `CompanyId` CHAR(36) NOT NULL,
  `CustomerId` CHAR(36) NOT NULL,
  `Series` VARCHAR(32) NOT NULL,
  `Number` VARCHAR(40) NOT NULL,
  `Type` INT NOT NULL,
  `Status` INT NOT NULL,
  `IssueDate` DATE NOT NULL,
  `OperationDate` DATE NULL,
  `DueDate` DATE NULL,
  `Currency` VARCHAR(3) NOT NULL,
  `TaxableBase` DECIMAL(18,2) NOT NULL,
  `TaxAmount` DECIMAL(18,2) NOT NULL,
  `WithholdingAmount` DECIMAL(18,2) NOT NULL,
  `Total` DECIMAL(18,2) NOT NULL,
  `IssuerNif` VARCHAR(20) NOT NULL,
  `IssuerName` VARCHAR(250) NOT NULL,
  `CustomerTaxId` VARCHAR(20) NOT NULL,
  `CustomerName` VARCHAR(250) NOT NULL,
  `CorrectedInvoiceId` CHAR(36) NULL,
  `IssuedAt` DATETIME(6) NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_Invoices_TenantId_Id` (`TenantId`,`Id`),
  UNIQUE KEY `UX_Invoices_0` (`TenantId`,`CompanyId`,`Series`,`Number`),
  CONSTRAINT `FK_Invoices_Companys_0` FOREIGN KEY (`TenantId`,`CompanyId`) REFERENCES `Companys` (`TenantId`,`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Invoices_Customers_1` FOREIGN KEY (`TenantId`,`CustomerId`) REFERENCES `Customers` (`TenantId`,`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `InvoiceLines` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `InvoiceId` CHAR(36) NOT NULL,
  `LineNumber` INT NOT NULL,
  `Description` VARCHAR(1000) NOT NULL,
  `Quantity` DECIMAL(18,6) NOT NULL,
  `UnitPrice` DECIMAL(18,6) NOT NULL,
  `DiscountPercent` DECIMAL(9,4) NOT NULL,
  `VatRate` DECIMAL(9,4) NOT NULL,
  `NetAmount` DECIMAL(18,2) NOT NULL,
  `VatAmount` DECIMAL(18,2) NOT NULL,
  `TotalAmount` DECIMAL(18,2) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_InvoiceLines_TenantId_Id` (`TenantId`,`Id`),
  CONSTRAINT `FK_InvoiceLines_Invoices_0` FOREIGN KEY (`TenantId`,`InvoiceId`) REFERENCES `Invoices` (`TenantId`,`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `InvoiceTaxBreakdowns` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `InvoiceId` CHAR(36) NOT NULL,
  `TaxCode` VARCHAR(20) NOT NULL,
  `RegimeKey` VARCHAR(20) NULL,
  `OperationClassification` VARCHAR(60) NULL,
  `ExemptionCode` VARCHAR(30) NULL,
  `TaxRate` DECIMAL(9,4) NOT NULL,
  `TaxableBase` DECIMAL(18,2) NOT NULL,
  `TaxAmount` DECIMAL(18,2) NOT NULL,
  `SurchargeRate` DECIMAL(9,4) NULL,
  `SurchargeAmount` DECIMAL(18,2) NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_InvoiceTaxBreakdowns_TenantId_Id` (`TenantId`,`Id`),
  CONSTRAINT `FK_InvoiceTaxBreakdowns_Invoices_0` FOREIGN KEY (`TenantId`,`InvoiceId`) REFERENCES `Invoices` (`TenantId`,`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `InvoiceSequences` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `CompanyId` CHAR(36) NOT NULL,
  `Series` VARCHAR(32) NOT NULL,
  `FiscalYear` INT NOT NULL,
  `NextNumber` BIGINT NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_InvoiceSequences_TenantId_Id` (`TenantId`,`Id`),
  UNIQUE KEY `UX_InvoiceSequences_0` (`TenantId`,`CompanyId`,`Series`,`FiscalYear`),
  CONSTRAINT `FK_InvoiceSequences_Companys_0` FOREIGN KEY (`TenantId`,`CompanyId`) REFERENCES `Companys` (`TenantId`,`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `FiscalChains` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `CompanyId` CHAR(36) NOT NULL,
  `InstallationId` VARCHAR(100) NOT NULL,
  `LastRecordId` CHAR(36) NULL,
  `LastHash` VARCHAR(64) NOT NULL,
  `UpdatedAt` DATETIME(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_FiscalChains_TenantId_Id` (`TenantId`,`Id`),
  UNIQUE KEY `UX_FiscalChains_0` (`TenantId`,`CompanyId`,`InstallationId`),
  CONSTRAINT `FK_FiscalChains_Companys_0` FOREIGN KEY (`TenantId`,`CompanyId`) REFERENCES `Companys` (`TenantId`,`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `FiscalRecords` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `InvoiceId` CHAR(36) NOT NULL,
  `RecordType` INT NOT NULL,
  `PreviousRecordId` CHAR(36) NULL,
  `PreviousHash` VARCHAR(64) NOT NULL,
  `Hash` VARCHAR(64) NOT NULL,
  `GeneratedAt` DATETIME(6) NOT NULL,
  `SoftwareName` VARCHAR(200) NOT NULL,
  `SoftwareVersion` VARCHAR(50) NOT NULL,
  `InstallationId` VARCHAR(100) NOT NULL,
  `XmlPayload` LONGTEXT NOT NULL,
  `SubmissionStatus` INT NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_FiscalRecords_TenantId_Id` (`TenantId`,`Id`),
  CONSTRAINT `FK_FiscalRecords_Invoices_0` FOREIGN KEY (`TenantId`,`InvoiceId`) REFERENCES `Invoices` (`TenantId`,`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `AeatSubmissions` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `FiscalRecordId` CHAR(36) NOT NULL,
  `Environment` VARCHAR(20) NOT NULL,
  `SubmittedAt` DATETIME(6) NOT NULL,
  `Status` INT NOT NULL,
  `Csv` VARCHAR(150) NULL,
  `ErrorCode` VARCHAR(80) NULL,
  `ErrorDescription` VARCHAR(1000) NULL,
  `RequestXml` LONGTEXT NOT NULL,
  `ResponseXml` LONGTEXT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_AeatSubmissions_TenantId_Id` (`TenantId`,`Id`),
  CONSTRAINT `FK_AeatSubmissions_FiscalRecords_0` FOREIGN KEY (`TenantId`,`FiscalRecordId`) REFERENCES `FiscalRecords` (`TenantId`,`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `ElectronicDocuments` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `CompanyId` CHAR(36) NOT NULL,
  `InvoiceId` CHAR(36) NULL,
  `Direction` INT NOT NULL,
  `Format` VARCHAR(50) NOT NULL,
  `FormatVersion` VARCHAR(30) NOT NULL,
  `DocumentNumber` VARCHAR(80) NOT NULL,
  `XmlPayload` LONGTEXT NOT NULL,
  `ContentHash` VARCHAR(128) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_ElectronicDocuments_TenantId_Id` (`TenantId`,`Id`),
  CONSTRAINT `FK_ElectronicDocuments_Invoices_0` FOREIGN KEY (`TenantId`,`InvoiceId`) REFERENCES `Invoices` (`TenantId`,`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `B2BTransmissions` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `ElectronicDocumentId` CHAR(36) NOT NULL,
  `Provider` VARCHAR(100) NOT NULL,
  `ExternalId` VARCHAR(150) NULL,
  `Status` INT NOT NULL,
  `SubmittedAt` DATETIME(6) NULL,
  `AcknowledgedAt` DATETIME(6) NULL,
  `RequestPayload` LONGTEXT NULL,
  `ResponsePayload` LONGTEXT NULL,
  `ErrorCode` VARCHAR(80) NULL,
  `ErrorDescription` VARCHAR(1000) NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_B2BTransmissions_TenantId_Id` (`TenantId`,`Id`),
  CONSTRAINT `FK_B2BTransmissions_ElectronicDocuments_0` FOREIGN KEY (`TenantId`,`ElectronicDocumentId`) REFERENCES `ElectronicDocuments` (`TenantId`,`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `InvoiceStatusEvents` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `InvoiceId` CHAR(36) NOT NULL,
  `EventType` VARCHAR(100) NOT NULL,
  `Source` VARCHAR(100) NOT NULL,
  `OccurredAt` DATETIME(6) NOT NULL,
  `ReportedAt` DATETIME(6) NULL,
  `ExternalReference` VARCHAR(200) NULL,
  `Payload` LONGTEXT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_InvoiceStatusEvents_TenantId_Id` (`TenantId`,`Id`),
  CONSTRAINT `FK_InvoiceStatusEvents_Invoices_0` FOREIGN KEY (`TenantId`,`InvoiceId`) REFERENCES `Invoices` (`TenantId`,`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `InvoicePayments` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `InvoiceId` CHAR(36) NOT NULL,
  `Amount` DECIMAL(18,2) NOT NULL,
  `PaymentDate` DATE NOT NULL,
  `PaymentMethod` VARCHAR(60) NOT NULL,
  `ExternalReference` VARCHAR(200) NULL,
  `CreatedAt` DATETIME(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_InvoicePayments_TenantId_Id` (`TenantId`,`Id`),
  CONSTRAINT `FK_InvoicePayments_Invoices_0` FOREIGN KEY (`TenantId`,`InvoiceId`) REFERENCES `Invoices` (`TenantId`,`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `OutboxMessages` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `Type` VARCHAR(100) NOT NULL,
  `AggregateId` VARCHAR(100) NOT NULL,
  `Payload` LONGTEXT NOT NULL,
  `Status` VARCHAR(32) NOT NULL,
  `Attempts` INT NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL,
  `NextAttemptAt` DATETIME(6) NULL,
  `ProcessedAt` DATETIME(6) NULL,
  `LastError` LONGTEXT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_OutboxMessages_TenantId_Id` (`TenantId`,`Id`),
  KEY `IX_OutboxMessage_Pending` (`TenantId`,`Status`,`NextAttemptAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `AuditEvents` (
  `TenantId` CHAR(36) NOT NULL,
  `Id` CHAR(36) NOT NULL,
  `CompanyId` CHAR(36) NOT NULL,
  `EntityType` VARCHAR(100) NOT NULL,
  `EntityId` VARCHAR(100) NOT NULL,
  `Action` VARCHAR(100) NOT NULL,
  `UserId` VARCHAR(450) NULL,
  `OccurredAt` DATETIME(6) NOT NULL,
  `PreviousValues` LONGTEXT NULL,
  `NewValues` LONGTEXT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_AuditEvents_TenantId_Id` (`TenantId`,`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ASP.NET Core Identity: IdentityDbContext<ApplicationUser> (clave string)
CREATE TABLE IF NOT EXISTS `AspNetUsers` (
 `Id` VARCHAR(255) NOT NULL, `UserName` VARCHAR(256) NULL, `NormalizedUserName` VARCHAR(256) NULL,
 `Email` VARCHAR(256) NULL, `NormalizedEmail` VARCHAR(256) NULL, `EmailConfirmed` TINYINT(1) NOT NULL,
 `PasswordHash` LONGTEXT NULL, `SecurityStamp` LONGTEXT NULL, `ConcurrencyStamp` LONGTEXT NULL,
 `PhoneNumber` LONGTEXT NULL, `PhoneNumberConfirmed` TINYINT(1) NOT NULL,
 `TwoFactorEnabled` TINYINT(1) NOT NULL, `LockoutEnd` DATETIME(6) NULL,
 `LockoutEnabled` TINYINT(1) NOT NULL, `AccessFailedCount` INT NOT NULL,
 `DisplayName` LONGTEXT NOT NULL, `IsActive` TINYINT(1) NOT NULL,
 PRIMARY KEY (`Id`), UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
 KEY `EmailIndex` (`NormalizedEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS `AspNetRoles` (
 `Id` VARCHAR(255) NOT NULL, `Name` VARCHAR(256) NULL, `NormalizedName` VARCHAR(256) NULL,
 `ConcurrencyStamp` LONGTEXT NULL, PRIMARY KEY (`Id`), UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS `AspNetUserClaims` (
 `Id` INT NOT NULL AUTO_INCREMENT, `UserId` VARCHAR(255) NOT NULL, `ClaimType` LONGTEXT NULL,
 `ClaimValue` LONGTEXT NULL, PRIMARY KEY (`Id`), KEY `IX_UserClaims_UserId` (`UserId`),
 FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS `AspNetUserLogins` (
 `LoginProvider` VARCHAR(255) NOT NULL, `ProviderKey` VARCHAR(255) NOT NULL,
 `ProviderDisplayName` LONGTEXT NULL, `UserId` VARCHAR(255) NOT NULL,
 PRIMARY KEY (`LoginProvider`,`ProviderKey`), KEY `IX_UserLogins_UserId` (`UserId`),
 FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS `AspNetUserRoles` (
 `UserId` VARCHAR(255) NOT NULL, `RoleId` VARCHAR(255) NOT NULL,
 PRIMARY KEY (`UserId`,`RoleId`), KEY `IX_UserRoles_RoleId` (`RoleId`),
 FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers`(`Id`) ON DELETE CASCADE,
 FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS `AspNetRoleClaims` (
 `Id` INT NOT NULL AUTO_INCREMENT, `RoleId` VARCHAR(255) NOT NULL, `ClaimType` LONGTEXT NULL,
 `ClaimValue` LONGTEXT NULL, PRIMARY KEY (`Id`), KEY `IX_RoleClaims_RoleId` (`RoleId`),
 FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS `AspNetUserTokens` (
 `UserId` VARCHAR(255) NOT NULL, `LoginProvider` VARCHAR(255) NOT NULL,
 `Name` VARCHAR(255) NOT NULL, `Value` LONGTEXT NULL,
 PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
 FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS `TenantUsers` (
 `Id` CHAR(36) NOT NULL, `TenantId` CHAR(36) NOT NULL,
 `UserId` VARCHAR(255) NOT NULL, `IsActive` TINYINT(1) NOT NULL,
 `CreatedAt` DATETIME(6) NOT NULL,
 PRIMARY KEY (`Id`), UNIQUE KEY `UX_TenantUsers` (`TenantId`,`UserId`),
 FOREIGN KEY (`TenantId`) REFERENCES `Tenants`(`Id`) ON DELETE RESTRICT,
 FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers`(`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS `CompanyUsers` (
 `Id` CHAR(36) NOT NULL, `TenantId` CHAR(36) NOT NULL,
 `CompanyId` CHAR(36) NOT NULL, `UserId` VARCHAR(255) NOT NULL,
 `Role` VARCHAR(32) NOT NULL, `IsActive` TINYINT(1) NOT NULL,
 PRIMARY KEY (`Id`), UNIQUE KEY `UX_CompanyUsers` (`TenantId`,`CompanyId`,`UserId`),
 FOREIGN KEY (`TenantId`,`CompanyId`) REFERENCES `Companys`(`TenantId`,`Id`) ON DELETE RESTRICT,
 FOREIGN KEY (`TenantId`,`UserId`) REFERENCES `TenantUsers`(`TenantId`,`UserId`) ON DELETE RESTRICT,
 FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers`(`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
