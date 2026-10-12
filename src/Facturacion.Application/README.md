# Facturacion.Application (.NET 8)

Capa de aplicación para el modelo híbrido multi-tenant desarrollado anteriormente.

## Incluye
- Result, Result<T>, Error, ErrorType y ApiResponse<T>.
- Contratos de usuario, tenant y autorización por empresa.
- Contratos para repositorio de facturas, VERI*FACTU y B2B.
- DTO de empresas, clientes, facturas y resultados de integraciones.
- Caso de uso funcional para crear y consultar **borradores** de facturas.

## Instalación
1. Copia esta carpeta al mismo nivel que `Facturacion.Domain`.
2. Asegúrate de que `Facturacion.Domain/Facturacion.Domain.csproj` existe.
3. `dotnet sln add Facturacion.Application/Facturacion.Application.csproj`
4. `dotnet build Facturacion.Application/Facturacion.Application.csproj`

## Integración obligatoria
- Implementar `IInvoiceRepository` en Infrastructure usando `BillingDbContext`.
- Implementar y registrar `ITenantContext` seguro y `ICompanyAuthorizationService` (NO usar implementaciones que concedan todos los permisos).
- Registrar `IInvoiceApplicationService` con `InvoiceApplicationService` en la API.
- Configurar JWT, autenticación, autorización, middleware y traducción HTTP de Result en Facturacion.Api.
- El `ApiResponse<T>` es un contrato de transporte: la extensión `ToActionResult` pertenece a Api, no a Application.

## Limitaciones intencionales
- No implementa emisión fiscal, rectificación, cálculo fiscal completo ni integración real AEAT/B2B.
- `CreateDraftAsync` admite solo EUR e IVA 0, 4, 10 y 21 por simplificación. IVA 0 no equivale por sí solo a exención o no sujeción.
- Las validaciones fiscales y de direcciones se completarán antes de emitir.
- La seguridad multi-tenant requiere autenticación real, autorización por empresa, filtros y restricciones de persistencia.
- No se ha podido ejecutar `dotnet build` en este entorno porque el SDK .NET no está instalado.
