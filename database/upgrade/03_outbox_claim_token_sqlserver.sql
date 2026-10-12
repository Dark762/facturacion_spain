-- ACTUALIZACION NO DESTRUCTIVA - SQL Server
-- Anade la columna ClaimToken a OutboxMessages para coordinacion atomica de workers.
-- Idempotente.
--
--   sqlcmd -S localhost -d facturacion_saas -i upgrade/03_outbox_claim_token_sqlserver.sql
--
-- Rollback (si fuera necesario):
--   ALTER TABLE [OutboxMessages] DROP COLUMN [ClaimToken];
--   GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('OutboxMessages') AND name = 'ClaimToken'
)
BEGIN
    ALTER TABLE [OutboxMessages] ADD [ClaimToken] nvarchar(64) NULL;
END
GO
