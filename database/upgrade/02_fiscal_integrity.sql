-- =====================================================================
-- ACTUALIZACION NO DESTRUCTIVA - MariaDB / MySQL 8
--
-- NO dropea tablas y NO modifica datos fiscales historicos. Solo anade
-- restricciones de unicidad e integridad que falten.
--
-- Ejecutar DESPUES de una instalacion existente del esquema, y antes de
-- habilitar la emision:
--   mariadb -u root -p facturacion_saas < upgrade/02_fiscal_integrity.sql
--
-- Es idempotente: se puede reaplicar sin error.
--
-- CONTENIDO:
--   1. Un solo registro fiscal por factura y por tipo de registro. Sin esto
--      un reintento de emision podria duplicar el alta.
--   2. La cadena fiscal es unica por empresa emisora.
--   3. La cabecera de la factura queda enlazada a su unico registro fiscal.
--   4. El Outbox se indexa por estado y proximo intento, que es como lo
--      consulta el worker.
-- =====================================================================

-- ---------------------------------------------------------------------
-- 1. Unicidad de registros fiscales
-- ---------------------------------------------------------------------
SET @u1 = (SELECT COUNT(*) FROM information_schema.STATISTICS
           WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'fiscalrecords'
             AND INDEX_NAME = 'UX_FiscalRecords_InvoiceId_RecordType');
SET @s1 = IF(@u1 = 0,
  'ALTER TABLE `fiscalrecords` ADD UNIQUE INDEX `UX_FiscalRecords_InvoiceId_RecordType` (`InvoiceId`, `RecordType`)',
  'DO 0');
PREPARE p1 FROM @s1; EXECUTE p1; DEALLOCATE PREPARE p1;

-- ---------------------------------------------------------------------
-- 2. Una sola cadena por empresa emisora
-- ---------------------------------------------------------------------
SET @u2 = (SELECT COUNT(*) FROM information_schema.STATISTICS
           WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'fiscalchains'
             AND INDEX_NAME = 'UX_FiscalChains_CompanyId');
SET @s2 = IF(@u2 = 0,
  'ALTER TABLE `fiscalchains` ADD UNIQUE INDEX `UX_FiscalChains_CompanyId` (`CompanyId`)',
  'DO 0');
PREPARE p2 FROM @s2; EXECUTE p2; DEALLOCATE PREPARE p2;

-- ---------------------------------------------------------------------
-- 3. Indice del Outbox por estado y proximo intento
-- ---------------------------------------------------------------------
SET @u3 = (SELECT COUNT(*) FROM information_schema.STATISTICS
           WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'outboxmessages'
             AND INDEX_NAME = 'IX_OutboxMessages_Status_NextAttemptAt');
SET @s3 = IF(@u3 = 0,
  'ALTER TABLE `outboxmessages` ADD INDEX `IX_OutboxMessages_Status_NextAttemptAt` (`Status`, `NextAttemptAt`)',
  'DO 0');
PREPARE p3 FROM @s3; EXECUTE p3; DEALLOCATE PREPARE p3;

-- ---------------------------------------------------------------------
-- 4. Huella de 64 caracteres hexadecimales (SHA-256)
-- ---------------------------------------------------------------------
SET @ck = (SELECT CONSTRAINT_NAME FROM information_schema.CHECK_CONSTRAINTS
           WHERE CONSTRAINT_SCHEMA = DATABASE() AND CONSTRAINT_NAME = 'CK_FiscalRecords_Hash');
SET @s4 = IF(@ck IS NULL,
  'ALTER TABLE `fiscalrecords` ADD CONSTRAINT `CK_FiscalRecords_Hash` CHECK (`Hash` REGEXP ''^[0-9a-f]{64}$'' OR `Hash` = '''')',
  'DO 0');
PREPARE p4 FROM @s4; EXECUTE p4; DEALLOCATE PREPARE p4;