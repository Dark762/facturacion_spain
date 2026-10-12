-- ACTUALIZACION NO DESTRUCTIVA - SQL Server
-- No dropea tablas ni modifica datos fiscales historicos. Solo anade indices
-- y restricciones de integridad que falten. Idempotente.
--
--   sqlcmd -S localhost -d facturacion_saas -i upgrade/02_fiscal_integrity_sqlserver.sql
--
-- SET QUOTED_IDENTIFIER ON es obligatorio: hay indices filtrados.
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_FiscalRecords_InvoiceId_RecordType' AND object_id = OBJECT_ID('FiscalRecords'))
    CREATE UNIQUE INDEX [UX_FiscalRecords_InvoiceId_RecordType] ON [FiscalRecords] ([InvoiceId], [RecordType]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_FiscalChains_CompanyId' AND object_id = OBJECT_ID('FiscalChains'))
    CREATE UNIQUE INDEX [UX_FiscalChains_CompanyId] ON [FiscalChains] ([CompanyId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OutboxMessages_Status_NextAttemptAt' AND object_id = OBJECT_ID('OutboxMessages'))
    CREATE INDEX [IX_OutboxMessages_Status_NextAttemptAt] ON [OutboxMessages] ([Status], [NextAttemptAt]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_FiscalRecords_Hash')
    ALTER TABLE [FiscalRecords] WITH CHECK
        ADD CONSTRAINT [CK_FiscalRecords_Hash]
        CHECK ([Hash] = '' OR [Hash] NOT LIKE '%[^0-9a-f]%' AND LEN([Hash]) = 64);
GO