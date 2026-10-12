-- ACTUALIZACION NO DESTRUCTIVA - PostgreSQL
-- Anade la columna ClaimToken a OutboxMessages para coordinacion atomica de workers.
-- Idempotente.
--
--   psql -U facturacion -d facturacion_saas -f upgrade/03_outbox_claim_token_postgresql.sql
--
-- Rollback (si fuera necesario):
--   ALTER TABLE "OutboxMessages" DROP COLUMN IF EXISTS "ClaimToken";

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = CURRENT_SCHEMA()
          AND table_name = 'OutboxMessages'
          AND column_name = 'ClaimToken'
    ) THEN
        ALTER TABLE "OutboxMessages" ADD COLUMN "ClaimToken" character varying(64);
    END IF;
END $$;
