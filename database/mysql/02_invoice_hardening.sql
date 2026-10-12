-- MySQL 8.0.16+; execute ONLY after backup and preflight. Existing UX_Invoices_0 must remain.
-- Review whether IssuedAt is the definitive issue marker in application logic.
SELECT `Id`,`Status`,`Series`,`Number`,`IssuedAt` FROM `Invoices`
WHERE (`Series` IS NULL) <> (`Number` IS NULL)
 OR (`Series` IS NOT NULL AND TRIM(`Series`)='')
 OR (`Number` IS NOT NULL AND TRIM(`Number`)='')
 OR (`IssuedAt` IS NOT NULL AND (`Series` IS NULL OR `Number` IS NULL));
-- STOP if query returns rows; do not silently repair issued invoices.
ALTER TABLE `Invoices` MODIFY COLUMN `Series` VARCHAR(32) NULL,
 MODIFY COLUMN `Number` VARCHAR(40) NULL;
ALTER TABLE `Invoices` ADD CONSTRAINT `CK_Invoices_NumberPair`
 CHECK ((`Series` IS NULL AND `Number` IS NULL) OR
 (`Series` IS NOT NULL AND `Number` IS NOT NULL AND TRIM(`Series`) <> '' AND TRIM(`Number`) <> ''));
ALTER TABLE `Invoices` ADD CONSTRAINT `CK_Invoices_IssuedNumber`
 CHECK (`IssuedAt` IS NULL OR (`Series` IS NOT NULL AND `Number` IS NOT NULL));
-- No additional unique index needed if UX_Invoices_0 exists and is valid.
-- Immutable issued invoice protection (identifiers, amounts, fiscal data):
DELIMITER $$
CREATE TRIGGER `TR_Invoices_ProtectIssued_Update` BEFORE UPDATE ON `Invoices`
FOR EACH ROW BEGIN
 IF OLD.`IssuedAt` IS NOT NULL AND (
 NOT (OLD.`TenantId` <=> NEW.`TenantId`) OR NOT (OLD.`CompanyId` <=> NEW.`CompanyId`) OR
 NOT (OLD.`Series` <=> NEW.`Series`) OR NOT (OLD.`Number` <=> NEW.`Number`) OR
 NOT (OLD.`IssueDate` <=> NEW.`IssueDate`) OR NOT (OLD.`CustomerId` <=> NEW.`CustomerId`) OR
 NOT (OLD.`TaxableBase` <=> NEW.`TaxableBase`) OR NOT (OLD.`TaxAmount` <=> NEW.`TaxAmount`) OR
 NOT (OLD.`Total` <=> NEW.`Total`) OR NOT (OLD.`IssuedAt` <=> NEW.`IssuedAt`)
 ) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Issued invoice fiscal fields are immutable'; END IF;
END$$
CREATE TRIGGER `TR_Invoices_ProtectIssued_Delete` BEFORE DELETE ON `Invoices`
FOR EACH ROW BEGIN
 IF OLD.`IssuedAt` IS NOT NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Issued invoices cannot be deleted'; END IF;
END$$
DELIMITER ;
