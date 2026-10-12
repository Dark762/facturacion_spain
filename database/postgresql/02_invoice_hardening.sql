-- =====================================================================
-- Facturacion - Endurecimiento de facturas (PostgreSQL 13+)
-- Database First: este script es la fuente de verdad del endurecimiento.
--
-- Aplicar DESPUES de 01_full_schema.sql:
--   psql -U facturacion -d facturacion_saas -f 02_invoice_hardening.sql
--
-- ADVERTENCIA: este script NO ha sido ejecutado contra un servidor
-- PostgreSQL real. Revisar antes de produccion.
--
-- Criterio de "factura emitida" (usado por TODAS las protecciones):
--   Status = 1 (Issued)  OR  IssuedAt IS NOT NULL
--
-- Diferencias de dialecto respecto a MySQL/MariaDB:
--  - Las tablas y columnas se citan con comillas dobles porque el baseline
--    las crea respetando mayusculas y PostgreSQL pliega a minusculas sin comillas.
--  - El bloqueo de exception se hace con RAISE EXCEPTION USING ERRCODE.
--  - Los triggers de deteccion se combinan en una funcion por tabla.
-- =====================================================================

ALTER TABLE "Invoices"
  ALTER COLUMN "Series" DROP NOT NULL,
  ALTER COLUMN "Number" DROP NOT NULL;

-- ---------------------------------------------------------------------
-- 1. Objetos previos (idempotencia)
-- ---------------------------------------------------------------------
DROP TRIGGER IF EXISTS "trg_invoices_protect_issued" ON "Invoices";
DROP TRIGGER IF EXISTS "trg_invoicelines_protect_issued" ON "InvoiceLines";
DROP TRIGGER IF EXISTS "trg_invoicetaxbreakdowns_protect_issued" ON "InvoiceTaxBreakdowns";
DROP FUNCTION IF EXISTS "fn_invoice_is_issued"(uuid, uuid);
DROP FUNCTION IF EXISTS "fn_block_issued_invoice_mutation"();
DROP FUNCTION IF EXISTS "fn_invoice_lines_guarded"();
DROP FUNCTION IF EXISTS "fn_invoice_tax_breakdowns_guarded"();

-- ---------------------------------------------------------------------
-- 2. Restricciones CHECK
-- ---------------------------------------------------------------------
ALTER TABLE "Invoices" DROP CONSTRAINT IF EXISTS "CK_Invoices_NumberPair";
ALTER TABLE "Invoices" DROP CONSTRAINT IF EXISTS "CK_Invoices_IssuedNumber";

ALTER TABLE "Invoices"
  ADD CONSTRAINT "CK_Invoices_NumberPair"
  CHECK (("Series" IS NULL AND "Number" IS NULL) OR
         ("Series" IS NOT NULL AND "Number" IS NOT NULL
          AND BTRIM("Series") <> '' AND BTRIM("Number") <> ''));

-- Una factura emitida exige numeracion. Cubre Status=Issued e IssuedAt.
ALTER TABLE "Invoices"
  ADD CONSTRAINT "CK_Invoices_IssuedNumber"
  CHECK (("Status" <> 1 AND "IssuedAt" IS NULL) OR
         ("Series" IS NOT NULL AND "Number" IS NOT NULL));

-- ---------------------------------------------------------------------
-- 3. Cabecera: inmutabilidad de la factura emitida
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_invoice_is_issued(
  p_tenant_id uuid, p_invoice_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
AS $$
  SELECT EXISTS (
    SELECT 1 FROM "Invoices" i
    WHERE i."TenantId" = p_tenant_id AND i."Id" = p_invoice_id
      AND (i."IssuedAt" IS NOT NULL OR i."Status" = 1));
$$;

CREATE OR REPLACE FUNCTION fn_block_issued_invoice_mutation()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
  v_old_issued boolean;
BEGIN
  v_old_issued := (OLD."IssuedAt" IS NOT NULL OR OLD."Status" = 1);

  IF TG_OP = 'DELETE' THEN
    IF v_old_issued THEN
      RAISE EXCEPTION 'Issued invoices cannot be deleted'
        USING ERRCODE = 'integrity_constraint_violation';
    END IF;
    RETURN OLD;
  END IF;

  -- Una factura emitida NO puede volver a borrador: seria deshacer una
  -- numeracion ya comunicada. Esta comprobacion va ANTES de la de
  -- inmutabilidad; si se dejase despues, nunca llegaria a ejecutarse.
  IF v_old_issued AND NEW."Status" = 0 THEN
    RAISE EXCEPTION 'An issued invoice cannot return to draft'
      USING ERRCODE = 'integrity_constraint_violation';
  END IF;

  -- Todo lo demas congelado, incluido Type, Currency, WithholdingAmount,
  -- IssuerNif/IssuerName y CustomerTaxId/CustomerName: son parte del
  -- documento fiscal. Unico campo mutable: Status. Si se anade una columna
  -- a Invoices, debe anadirse aqui para que quede protegida por defecto.
  IF v_old_issued AND (
       NEW."Id"                 IS DISTINCT FROM OLD."Id"                 OR
       NEW."TenantId"           IS DISTINCT FROM OLD."TenantId"           OR
       NEW."CompanyId"          IS DISTINCT FROM OLD."CompanyId"          OR
       NEW."CustomerId"         IS DISTINCT FROM OLD."CustomerId"         OR
       NEW."Series"             IS DISTINCT FROM OLD."Series"             OR
       NEW."Number"             IS DISTINCT FROM OLD."Number"             OR
       NEW."Type"               IS DISTINCT FROM OLD."Type"               OR
       NEW."IssueDate"          IS DISTINCT FROM OLD."IssueDate"          OR
       NEW."OperationDate"      IS DISTINCT FROM OLD."OperationDate"      OR
       NEW."DueDate"            IS DISTINCT FROM OLD."DueDate"            OR
       NEW."Currency"           IS DISTINCT FROM OLD."Currency"           OR
       NEW."TaxableBase"        IS DISTINCT FROM OLD."TaxableBase"        OR
       NEW."TaxAmount"          IS DISTINCT FROM OLD."TaxAmount"          OR
       NEW."WithholdingAmount"  IS DISTINCT FROM OLD."WithholdingAmount"  OR
       NEW."Total"              IS DISTINCT FROM OLD."Total"              OR
       NEW."IssuerNif"          IS DISTINCT FROM OLD."IssuerNif"          OR
       NEW."IssuerName"         IS DISTINCT FROM OLD."IssuerName"         OR
       NEW."CustomerTaxId"      IS DISTINCT FROM OLD."CustomerTaxId"      OR
       NEW."CustomerName"       IS DISTINCT FROM OLD."CustomerName"       OR
       NEW."CorrectedInvoiceId" IS DISTINCT FROM OLD."CorrectedInvoiceId" OR
       NEW."IssuedAt"           IS DISTINCT FROM OLD."IssuedAt")
  THEN
    RAISE EXCEPTION 'Issued invoice fiscal fields are immutable'
      USING ERRCODE = 'integrity_constraint_violation';
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER "trg_invoices_protect_issued"
  BEFORE UPDATE OR DELETE ON "Invoices"
  FOR EACH ROW EXECUTE FUNCTION fn_block_issued_invoice_mutation();

-- ---------------------------------------------------------------------
-- 4. Lineas: sin trigger sobre la cabecera, el detalle de una factura
--    emitida se podria reescribir o borrar por completo.
--    En UPDATE se comprueban las facturas padron ANTIGUA y NUEVA.
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_invoice_lines_guarded()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
  v_issued boolean := false;
BEGIN
  IF TG_OP <> 'INSERT' AND fn_invoice_is_issued(OLD."TenantId", OLD."InvoiceId") THEN
    v_issued := true;
  END IF;
  IF TG_OP <> 'DELETE' AND fn_invoice_is_issued(NEW."TenantId", NEW."InvoiceId") THEN
    v_issued := true;
  END IF;

  IF v_issued THEN
    RAISE EXCEPTION 'Invoice lines of an issued invoice are immutable'
      USING ERRCODE = 'integrity_constraint_violation';
  END IF;

  IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
  RETURN NEW;
END;
$$;

CREATE TRIGGER "trg_invoicelines_protect_issued"
  BEFORE INSERT OR UPDATE OR DELETE ON "InvoiceLines"
  FOR EACH ROW EXECUTE FUNCTION fn_invoice_lines_guarded();

-- ---------------------------------------------------------------------
-- 5. Desglose de impuestos: misma proteccion que las lineas
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_invoice_tax_breakdowns_guarded()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
  v_issued boolean := false;
BEGIN
  IF TG_OP <> 'INSERT' AND fn_invoice_is_issued(OLD."TenantId", OLD."InvoiceId") THEN
    v_issued := true;
  END IF;
  IF TG_OP <> 'DELETE' AND fn_invoice_is_issued(NEW."TenantId", NEW."InvoiceId") THEN
    v_issued := true;
  END IF;

  IF v_issued THEN
    RAISE EXCEPTION 'Tax breakdown rows of an issued invoice are immutable'
      USING ERRCODE = 'integrity_constraint_violation';
  END IF;

  IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
  RETURN NEW;
END;
$$;

CREATE TRIGGER "trg_invoicetaxbreakdowns_protect_issued"
  BEFORE INSERT OR UPDATE OR DELETE ON "InvoiceTaxBreakdowns"
  FOR EACH ROW EXECUTE FUNCTION fn_invoice_tax_breakdowns_guarded();
