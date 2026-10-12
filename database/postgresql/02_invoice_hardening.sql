-- PostgreSQL 13+; run on a maintenance window; existing schema from previous package.
BEGIN;
-- Abort if legacy data violates invariants (CHECK additions below will fail).
ALTER TABLE "Invoices" ALTER COLUMN "Series" DROP NOT NULL;
ALTER TABLE "Invoices" ALTER COLUMN "Number" DROP NOT NULL;
ALTER TABLE "Invoices" ADD CONSTRAINT "CK_Invoices_NumberPair"
 CHECK (("Series" IS NULL AND "Number" IS NULL) OR
 ("Series" IS NOT NULL AND "Number" IS NOT NULL AND btrim("Series") <> '' AND btrim("Number") <> ''));
ALTER TABLE "Invoices" ADD CONSTRAINT "CK_Invoices_IssuedNumber"
 CHECK ("IssuedAt" IS NULL OR ("Series" IS NOT NULL AND "Number" IS NOT NULL));
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Invoices_IssuedNumber"
 ON "Invoices" ("TenantId","CompanyId","Series","Number")
 WHERE "Series" IS NOT NULL AND "Number" IS NOT NULL;
CREATE OR REPLACE FUNCTION facturacion_protect_issued_invoice() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_OP='DELETE' THEN
   IF OLD."IssuedAt" IS NOT NULL THEN RAISE EXCEPTION 'Issued invoices cannot be deleted'; END IF;
   RETURN OLD;
 END IF;
 IF OLD."IssuedAt" IS NOT NULL AND
  (OLD."TenantId",OLD."CompanyId",OLD."Series",OLD."Number",OLD."IssueDate",OLD."CustomerId",OLD."TaxableBase",OLD."TaxAmount",OLD."Total",OLD."IssuedAt")
  IS DISTINCT FROM
  (NEW."TenantId",NEW."CompanyId",NEW."Series",NEW."Number",NEW."IssueDate",NEW."CustomerId",NEW."TaxableBase",NEW."TaxAmount",NEW."Total",NEW."IssuedAt")
 THEN RAISE EXCEPTION 'Issued invoice fiscal fields are immutable'; END IF;
 RETURN NEW;
END $$;
CREATE TRIGGER "TR_Invoices_ProtectIssued" BEFORE UPDATE OR DELETE ON "Invoices"
 FOR EACH ROW EXECUTE FUNCTION facturacion_protect_issued_invoice();
COMMIT;
