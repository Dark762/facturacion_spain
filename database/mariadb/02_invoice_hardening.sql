-- =====================================================================
-- Facturacion - Endurecimiento de facturas (MariaDB / MySQL 8)
-- Database First: este script es la fuente de verdad del endurecimiento.
--
-- Aplicar DESPUES de 01_full_schema.sql:
--   mariadb -u root -p facturacion_saas < 02_invoice_hardening.sql
--
-- IDEMPOTENTE: puede reejecutarse sin error.
--
-- Criterio de "factura emitida" (usado por TODAS las protecciones):
--   Status = 1 (Issued)  OR  IssuedAt IS NOT NULL
-- Se comprueban ambos porque una factura puede tener Status=Issued sin
-- IssuedAt informado, y en ese caso sus datos fiscales serian mutables.
--
-- Alcance de la proteccion: cabecera Y detalle. Un trigger sobre Invoices
-- NO protege InvoiceLines ni InvoiceTaxBreakdowns: sin los triggers de
-- este script, el detalle de una factura emitida se puede alterar y borrar
-- mientras la cabecera permanece intacta, dejando totales sin respaldo.
--
-- REGLA DE DISENO: se congela TODO SALVO Status.
-- No se mantiene una lista de "campos fiscales" porque esa lista deja
-- libre por olvido cualquier columna que se anada a Invoices en el futuro.
-- Aqui el unico campo mutable de una factura emitida es Status, y aun asi
-- solo hacia adelante (anulada/rectificada): nunca de vuelta a borrador.
-- Si alguna vez hace falta otra columna mutable, se documenta aqui.
-- =====================================================================

-- ---------------------------------------------------------------------
-- 1. Serie y numero opcionales en borrador
-- ---------------------------------------------------------------------
-- En MySQL/MariaDB cada NULL es distinto dentro de un indice UNIQUE, de
-- modo que varios borradores pueden coexistir sin numerar. MySQL permite
-- NULL repetidos en indices unicos; SQL Server NO (ver su script).
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
  CONCAT('ALTER TABLE `Invoices` DROP CONSTRAINT `CK_Invoices_NumberPair`'));
PREPARE s1 FROM @sql1; EXECUTE s1; DEALLOCATE PREPARE s1;

SET @ck2 = (SELECT CONSTRAINT_NAME FROM information_schema.CHECK_CONSTRAINTS
            WHERE CONSTRAINT_SCHEMA = DATABASE() AND CONSTRAINT_NAME = 'CK_Invoices_IssuedNumber');
SET @sql2 = IF(@ck2 IS NULL, 'SELECT 1',
  CONCAT('ALTER TABLE `Invoices` DROP CONSTRAINT `CK_Invoices_IssuedNumber`'));
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
