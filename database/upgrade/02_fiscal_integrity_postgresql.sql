-- ACTUALIZACION NO DESTRUCTIVA - PostgreSQL
-- No dropea tablas ni modifica datos fiscales historicos. Solo anade indices
-- y restricciones de integridad que falten. Idempotente.
--
--   psql -U facturacion -d facturacion_saas -f upgrade/02_fiscal_integrity.sql

CREATE UNIQUE INDEX IF NOT EXISTS "UX_FiscalRecords_InvoiceId_RecordType"
    ON "FiscalRecords" ("InvoiceId", "RecordType");

CREATE UNIQUE INDEX IF NOT EXISTS "UX_FiscalChains_CompanyId"
    ON "FiscalChains" ("CompanyId");

CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_Status_NextAttemptAt"
    ON "OutboxMessages" ("Status", "NextAttemptAt");

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_FiscalRecords_Hash') THEN
        ALTER TABLE "FiscalRecords"
            ADD CONSTRAINT "CK_FiscalRecords_Hash"
            CHECK ("Hash" ~ '^[0-9a-f]{64}$' OR "Hash" = '');
    END IF;
END $$;