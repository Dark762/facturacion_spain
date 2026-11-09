# Facturacion.Domain

Modelo del negocio de **Facturación Spain** (.NET 8). Contiene entidades, enumeraciones e interfaz común para datos asociados a tenant.

## Entidades incluidas

- SaaS y seguridad: `Tenant`, `TenantUser`, `Company`, `CompanyUser`.
- Facturación comercial: `Customer`, `Invoice`, `InvoiceLine`, `InvoiceTaxBreakdown`, `InvoiceSequence`, `InvoicePayment`, `InvoiceStatusEvent`.
- Trazabilidad y cumplimiento: `FiscalRecord`, `FiscalChain`, `AeatSubmission`, `AuditEvent`.
- Documentación y comunicación: `ElectronicDocument`, `B2BTransmission`, `OutboxMessage`.

Incluye los enums `InvoiceStatus`, `InvoiceType`, `FiscalRecordType`, `SubmissionStatus` y `ElectronicDocumentDirection`, además de `ITenantEntity`.

## Criterio de diseño

El **dato comercial de la factura debe sobrevivir a cambios de la obligación VERI*FACTU**. No convertir la normativa ni el proveedor de pagos en el centro del dominio; separar los artefactos de cumplimiento y transmisión de la operación de facturación.

El dominio define estructuras; **la existencia de entidades no garantiza casos de uso completos, validación fiscal ni envío AEAT/B2B**. El filtrado y la comprobación de cambios de tenant están implementados en `BillingDbContext` de Infrastructure, no aquí.

```bash
dotnet build src/Facturacion.Domain/Facturacion.Domain.csproj
```

Ver [README principal](../../README.md).
