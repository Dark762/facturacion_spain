-- =====================================================================
-- Facturacion - Endurecimiento de facturas (SQL Server 2019+/2022)
-- Database First: este script es la fuente de verdad del endurecimiento.
--
-- Aplicar DESPUES de 01_full_schema.sql:
--   sqlcmd -S localhost -d facturacion_saas -i 02_invoice_hardening.sql
--
-- ADVERTENCIA: este script NO ha sido ejecutado contra un servidor
-- SQL Server real. Revisar antes de produccion.
--
-- Criterio de "factura emitida" (usado por TODAS las protecciones):
--   Status = 1 (Issued)  OR  IssuedAt IS NOT NULL
--
-- AVISO SOBRE NULL EN INDICES UNICOS
-- SQL Server NO admite mas de un NULL en un indice UNIQUE, a diferencia de
-- MySQL/MariaDB/PostgreSQL, donde cada NULL se considera distinto. Por eso
-- 01_full_schema.sql crea el indice UNIQUE de forma FILTRADA:
--   CREATE UNIQUE INDEX [IX_Invoices_TenantId_CompanyId_Series_Number]
--     ON [Invoices] (TenantId, CompanyId, Series, Number)
--     WHERE [Series] IS NOT NULL AND [Number] IS NOT NULL
-- El filtro reproduce exactamente la semántica del resto de motores: los
-- borradores sin numerar no colisionan entre si, y toda factura emitida
-- queda cubierta porque CK_Invoices_IssuedNumber obliga a tener serie y numero.
-- Este script NO crea ni modifica ese indice: solo lo deja DROP para poder
-- reejecutarse.
-- =====================================================================

SET XACT_ABORT ON;
GO

-- ---------------------------------------------------------------------
-- 1. Serie y numero opcionales
-- ---------------------------------------------------------------------
IF COL_LENGTH('Invoices', 'Series') IS NOT NULL
    ALTER TABLE [Invoices] ALTER COLUMN [Series] NVARCHAR(32) NULL;
IF COL_LENGTH('Invoices', 'Number') IS NOT NULL
    ALTER TABLE [Invoices] ALTER COLUMN [Number] NVARCHAR(40) NULL;
GO

-- ---------------------------------------------------------------------
-- 2. Objetos previos (idempotencia)
-- ---------------------------------------------------------------------
IF OBJECT_ID('trg_Invoices_ProtectIssued', 'TR') IS NOT NULL DROP TRIGGER [trg_Invoices_ProtectIssued];
IF OBJECT_ID('trg_InvoiceLines_ProtectIssued', 'TR') IS NOT NULL DROP TRIGGER [trg_InvoiceLines_ProtectIssued];
IF OBJECT_ID('trg_InvoiceTaxBreakdowns_ProtectIssued', 'TR') IS NOT NULL DROP TRIGGER [trg_InvoiceTaxBreakdowns_ProtectIssued];
GO

-- ---------------------------------------------------------------------
-- 3. Restricciones CHECK
-- ---------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Invoices_NumberPair')
    ALTER TABLE [Invoices] DROP CONSTRAINT [CK_Invoices_NumberPair];
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Invoices_IssuedNumber')
    ALTER TABLE [Invoices] DROP CONSTRAINT [CK_Invoices_IssuedNumber];
GO

ALTER TABLE [Invoices] WITH CHECK
    ADD CONSTRAINT [CK_Invoices_NumberPair]
    CHECK (([Series] IS NULL AND [Number] IS NULL) OR
           ([Series] IS NOT NULL AND [Number] IS NOT NULL
            AND LTRIM(RTRIM([Series])) <> '' AND LTRIM(RTRIM([Number])) <> ''));

ALTER TABLE [Invoices] WITH CHECK
    ADD CONSTRAINT [CK_Invoices_IssuedNumber]
    CHECK (([Status] <> 1 AND [IssuedAt] IS NULL) OR
           ([Series] IS NOT NULL AND [Number] IS NOT NULL));
GO

-- ---------------------------------------------------------------------
-- 4. Cabecera: inmutabilidad de la factura emitida
--    Se usa INSTEAD OF porque AFTER llega tarde: el cambio ya estaria
--    escrito cuando el trigger aborta la transaccion.
-- ---------------------------------------------------------------------
CREATE TRIGGER [trg_Invoices_ProtectIssued]
ON [Invoices]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF @@ROWCOUNT = 0 RETURN;   -- sentencia que no afecta a filas

    -- Una factura emitida NO puede volver a borrador: seria deshacer una
    -- numeracion ya comunicada. Esta comprobacion va ANTES de la de
    -- inmutabilidad; si se dejase despues, nunca llegaria a ejecutarse.
    IF UPDATE([Status]) AND EXISTS (
        SELECT 1 FROM inserted i
        JOIN deleted d ON d.[Id] = i.[Id]
        WHERE (d.[IssuedAt] IS NOT NULL OR d.[Status] = 1) AND i.[Status] = 0)
        THROW 50000, 'An issued invoice cannot return to draft', 1;

    -- Todo lo demas congelado, incluido Type, Currency, WithholdingAmount,
    -- IssuerNif/IssuerName y CustomerTaxId/CustomerName: son parte del
    -- documento fiscal. Unico campo mutable: Status. Si se anade una columna
    -- a Invoices, debe anadirse aqui para que quede protegida por defecto.
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.[Id] = i.[Id]
        WHERE (d.[IssuedAt] IS NOT NULL OR d.[Status] = 1)
          AND ( ISNULL(i.[Id] <> d.[Id], 1) = 1
             OR ISNULL(i.[TenantId] <> d.[TenantId], 1) = 1
             OR ISNULL(i.[CompanyId] <> d.[CompanyId], 1) = 1
             OR ISNULL(i.[CustomerId] <> d.[CustomerId], 1) = 1
             OR ISNULL(i.[Series] <> d.[Series], 1) = 1
             OR ISNULL(i.[Number] <> d.[Number], 1) = 1
             OR ISNULL(i.[Type] <> d.[Type], 1) = 1
             OR ISNULL(i.[IssueDate] <> d.[IssueDate], 1) = 1
             OR ISNULL(i.[OperationDate] <> d.[OperationDate], 1) = 1
             OR ISNULL(i.[DueDate] <> d.[DueDate], 1) = 1
             OR ISNULL(i.[Currency] <> d.[Currency], 1) = 1
             OR ISNULL(i.[TaxableBase] <> d.[TaxableBase], 1) = 1
             OR ISNULL(i.[TaxAmount] <> d.[TaxAmount], 1) = 1
             OR ISNULL(i.[WithholdingAmount] <> d.[WithholdingAmount], 1) = 1
             OR ISNULL(i.[Total] <> d.[Total], 1) = 1
             OR ISNULL(i.[IssuerNif] <> d.[IssuerNif], 1) = 1
             OR ISNULL(i.[IssuerName] <> d.[IssuerName], 1) = 1
             OR ISNULL(i.[CustomerTaxId] <> d.[CustomerTaxId], 1) = 1
             OR ISNULL(i.[CustomerName] <> d.[CustomerName], 1) = 1
             OR ISNULL(i.[CorrectedInvoiceId] <> d.[CorrectedInvoiceId], 1) = 1
             OR ISNULL(i.[IssuedAt] <> d.[IssuedAt], 1) = 1 ))
        THROW 50000, 'Issued invoice fiscal fields are immutable', 1;

    IF EXISTS (SELECT 1 FROM deleted d WHERE d.[IssuedAt] IS NOT NULL OR d.[Status] = 1)
    THROW 50000, 'Issued invoices cannot be deleted', 1;

    -- Reejecutar la operacion original
    IF EXISTS (SELECT 1 FROM inserted)
        UPDATE [Invoices]
        SET [TenantId] = i.[TenantId], [CompanyId] = i.[CompanyId],
            [CustomerId] = i.[CustomerId], [Series] = i.[Series], [Number] = i.[Number],
            [Type] = i.[Type], [Status] = i.[Status], [IssueDate] = i.[IssueDate],
            [OperationDate] = i.[OperationDate], [DueDate] = i.[DueDate],
            [Currency] = i.[Currency], [TaxableBase] = i.[TaxableBase],
            [TaxAmount] = i.[TaxAmount], [WithholdingAmount] = i.[WithholdingAmount],
            [Total] = i.[Total], [IssuerNif] = i.[IssuerNif], [IssuerName] = i.[IssuerName],
            [CustomerTaxId] = i.[CustomerTaxId], [CustomerName] = i.[CustomerName],
            [CorrectedInvoiceId] = i.[CorrectedInvoiceId], [IssuedAt] = i.[IssuedAt]
        FROM inserted i
        WHERE [Invoices].[Id] = i.[Id];

    IF EXISTS (SELECT 1 FROM deleted)
        DELETE [Invoices] WHERE [Id] IN (SELECT [Id] FROM deleted);
END;
GO

-- ---------------------------------------------------------------------
-- 5. Lineas: sin trigger sobre la cabecera, el detalle de una factura
--    emitida se podria reescribir o borrar por completo.
-- ---------------------------------------------------------------------
CREATE TRIGGER [trg_InvoiceLines_ProtectIssued]
ON [InvoiceLines]
INSTEAD OF INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF @@ROWCOUNT = 0 RETURN;

    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN [Invoices] inv
          ON inv.[TenantId] = i.[TenantId] AND inv.[Id] = i.[InvoiceId]
        WHERE inv.[IssuedAt] IS NOT NULL OR inv.[Status] = 1)
    THROW 50000, 'Invoice lines of an issued invoice are immutable', 1;

    IF EXISTS (
        SELECT 1 FROM deleted d
        JOIN [Invoices] inv
          ON inv.[TenantId] = d.[TenantId] AND inv.[Id] = d.[InvoiceId]
        WHERE inv.[IssuedAt] IS NOT NULL OR inv.[Status] = 1)
    THROW 50000, 'Invoice lines of an issued invoice are immutable', 1;

    IF EXISTS (SELECT 1 FROM inserted)
        UPDATE [InvoiceLines]
        SET [TenantId] = i.[TenantId], [InvoiceId] = i.[InvoiceId],
            [LineNumber] = i.[LineNumber], [Description] = i.[Description],
            [Quantity] = i.[Quantity], [UnitPrice] = i.[UnitPrice],
            [DiscountPercent] = i.[DiscountPercent], [VatRate] = i.[VatRate],
            [NetAmount] = i.[NetAmount], [VatAmount] = i.[VatAmount],
            [TotalAmount] = i.[TotalAmount]
        FROM inserted i WHERE [InvoiceLines].[Id] = i.[Id];

    IF EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted)
        INSERT [InvoiceLines]
            ([TenantId],[Id],[InvoiceId],[LineNumber],[Description],[Quantity],
             [UnitPrice],[DiscountPercent],[VatRate],[NetAmount],[VatAmount],[TotalAmount])
        SELECT i.[TenantId],i.[Id],i.[InvoiceId],i.[LineNumber],i.[Description],i.[Quantity],
               i.[UnitPrice],i.[DiscountPercent],i.[VatRate],i.[NetAmount],i.[VatAmount],i.[TotalAmount]
        FROM inserted i;

    IF EXISTS (SELECT 1 FROM deleted)
        DELETE [InvoiceLines] WHERE [Id] IN (SELECT [Id] FROM deleted);
END;
GO

-- ---------------------------------------------------------------------
-- 6. Desglose de impuestos: misma proteccion que las lineas
-- ---------------------------------------------------------------------
CREATE TRIGGER [trg_InvoiceTaxBreakdowns_ProtectIssued]
ON [InvoiceTaxBreakdowns]
INSTEAD OF INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF @@ROWCOUNT = 0 RETURN;

    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN [Invoices] inv
          ON inv.[TenantId] = i.[TenantId] AND inv.[Id] = i.[InvoiceId]
        WHERE inv.[IssuedAt] IS NOT NULL OR inv.[Status] = 1)
    THROW 50000, 'Tax breakdown rows of an issued invoice are immutable', 1;

    IF EXISTS (
        SELECT 1 FROM deleted d
        JOIN [Invoices] inv
          ON inv.[TenantId] = d.[TenantId] AND inv.[Id] = d.[InvoiceId]
        WHERE inv.[IssuedAt] IS NOT NULL OR inv.[Status] = 1)
    THROW 50000, 'Tax breakdown rows of an issued invoice are immutable', 1;

    IF EXISTS (SELECT 1 FROM inserted)
        UPDATE [InvoiceTaxBreakdowns]
        SET [TenantId] = t.[TenantId], [InvoiceId] = t.[InvoiceId], [TaxCode] = t.[TaxCode],
            [RegimeKey] = t.[RegimeKey], [OperationClassification] = t.[OperationClassification],
            [ExemptionCode] = t.[ExemptionCode], [TaxRate] = t.[TaxRate],
            [TaxableBase] = t.[TaxableBase], [TaxAmount] = t.[TaxAmount],
            [SurchargeRate] = t.[SurchargeRate], [SurchargeAmount] = t.[SurchargeAmount]
        FROM inserted t WHERE [InvoiceTaxBreakdowns].[Id] = t.[Id];

    IF EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted)
        INSERT [InvoiceTaxBreakdowns]
            ([TenantId],[Id],[InvoiceId],[TaxCode],[RegimeKey],[OperationClassification],
             [ExemptionCode],[TaxRate],[TaxableBase],[TaxAmount],[SurchargeRate],[SurchargeAmount])
        SELECT i.[TenantId],i.[Id],i.[InvoiceId],i.[TaxCode],i.[RegimeKey],i.[OperationClassification],
               i.[ExemptionCode],i.[TaxRate],i.[TaxableBase],i.[TaxAmount],i.[SurchargeRate],i.[SurchargeAmount]
        FROM inserted i;

    IF EXISTS (SELECT 1 FROM deleted)
        DELETE [InvoiceTaxBreakdowns] WHERE [Id] IN (SELECT [Id] FROM deleted);
END;
GO
