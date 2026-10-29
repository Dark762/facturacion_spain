-- Upgrade 06: catálogo de productos para MySQL 8 / MariaDB.
-- No elimina datos. Ejecutar una vez sobre la base de facturación con copia de seguridad.
-- La aplicación aplica aislamiento por tenant; FK compuesta protege también la relación con empresa.
CREATE TABLE IF NOT EXISTS `products` (
  `Id` char(36) NOT NULL,
  `TenantId` char(36) NOT NULL,
  `CompanyId` char(36) NOT NULL,
  `Code` varchar(64) NOT NULL,
  `Name` varchar(250) NOT NULL,
  `Description` varchar(1000) NOT NULL,
  `UnitPrice` decimal(18,2) NOT NULL,
  `VatRate` decimal(5,2) NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_Products_TenantId_Id` (`TenantId`, `Id`),
  UNIQUE KEY `IX_Products_TenantId_CompanyId_Code` (`TenantId`, `CompanyId`, `Code`),
  CONSTRAINT `FK_Products_Companys_TenantId_CompanyId`
    FOREIGN KEY (`TenantId`, `CompanyId`)
    REFERENCES `companys` (`TenantId`, `Id`)
    ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
