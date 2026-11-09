-- FC-01: MySQL 8 / MariaDB. Ejecutar en ventana de mantenimiento, con backup.
-- PASO 1 (solo lectura): si devuelve filas, DETENER y resolver duplicados
-- por TenantId+CompanyId. No borrar ni fusionar clientes automáticamente:
-- pueden estar referenciados por facturas.
SELECT TenantId, CompanyId, UPPER(TRIM(TaxId)) AS NormalizedTaxId,
       COUNT(*) AS Duplicates, GROUP_CONCAT(Id ORDER BY Id) AS CustomerIds
FROM customers
GROUP BY TenantId, CompanyId, UPPER(TRIM(TaxId))
HAVING COUNT(*) > 1;

-- PASO 2: revisar también identificadores que cambiarían con normalización.
SELECT Id, TenantId, CompanyId, TaxId, UPPER(TRIM(TaxId)) AS NormalizedTaxId
FROM customers
WHERE BINARY TaxId <> BINARY UPPER(TRIM(TaxId));

-- PASO 3: solo después de resolver duplicados y revisar la normalización,
-- ejecutar el DDL siguiente (no se ejecuta automáticamente con este script).
-- ALTER TABLE customers
--   ADD UNIQUE INDEX IX_Customers_TenantId_CompanyId_TaxId (TenantId, CompanyId, TaxId);
--
-- MySQL/MariaDB usan una colación insensible a mayúsculas para esta tabla.
-- Verificar que el índice no existe antes de ejecutar el ALTER:
SELECT INDEX_NAME, NON_UNIQUE
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'customers'
  AND INDEX_NAME = 'IX_Customers_TenantId_CompanyId_TaxId';
