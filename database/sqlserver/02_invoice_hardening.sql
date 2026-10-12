-- SQL Server 2019+; review table schema dbo and status mapping before execution.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
ALTER TABLE dbo.Invoices ALTER COLUMN Series varchar(32) NULL;
ALTER TABLE dbo.Invoices ALTER COLUMN Number varchar(40) NULL;
ALTER TABLE dbo.Invoices WITH CHECK ADD CONSTRAINT CK_Invoices_NumberPair CHECK (
 (Series IS NULL AND Number IS NULL) OR
 (Series IS NOT NULL AND Number IS NOT NULL AND LEN(LTRIM(RTRIM(Series))) > 0 AND LEN(LTRIM(RTRIM(Number))) > 0));
ALTER TABLE dbo.Invoices WITH CHECK ADD CONSTRAINT CK_Invoices_IssuedNumber CHECK (
 IssuedAt IS NULL OR (Series IS NOT NULL AND Number IS NOT NULL));
CREATE UNIQUE INDEX UX_Invoices_IssuedNumber ON dbo.Invoices(TenantId,CompanyId,Series,Number)
 WHERE Series IS NOT NULL AND Number IS NOT NULL;
COMMIT TRANSACTION;
GO
CREATE OR ALTER TRIGGER dbo.TR_Invoices_ProtectIssued ON dbo.Invoices AFTER UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 -- Deleting any issued invoice is forbidden; prevent updates to fiscal columns.
 IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id
  WHERE d.IssuedAt IS NOT NULL AND i.Id IS NULL)
 BEGIN THROW 51001, 'Issued invoices cannot be deleted', 1; END;
 IF EXISTS (SELECT 1 FROM deleted d INNER JOIN inserted i ON i.Id=d.Id
  WHERE d.IssuedAt IS NOT NULL AND (
   EXISTS (SELECT d.TenantId,d.CompanyId,d.Series,d.Number,d.IssueDate,d.CustomerId,d.TaxableBase,d.TaxAmount,d.Total,d.IssuedAt
           EXCEPT SELECT i.TenantId,i.CompanyId,i.Series,i.Number,i.IssueDate,i.CustomerId,i.TaxableBase,i.TaxAmount,i.Total,i.IssuedAt)))
 BEGIN THROW 51002, 'Issued invoice fiscal fields are immutable', 1; END;
END;
GO
