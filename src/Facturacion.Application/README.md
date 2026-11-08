# Facturacion.Application

## Responsabilidad
Casos de uso, DTO, interfaces de repositorio y servicios, resultados `Result` / `ApiResponse<T>`, reglas de orquestación de facturación, registro SaaS y aprovisionamiento.

## Configuración
No hospeda aplicación ni carga archivos appsettings. Recibe dependencias y opciones desde los hosts mediante inyección de dependencias. Los secretos se configuran en el host que ejecuta el caso de uso.

## Integración
`Facturacion.Api` registra casos de uso con `AddBillingApplication()`; Infrastructure implementa persistencia y servicios externos. Los errores de negocio pueden utilizar `BusinessException` con código estable y estado HTTP gestionado por la API.

## Seguridad y límites
Propagar el tenant y empresa validados, no aceptar un TenantId arbitrario para acceder a datos. `ApiResponse<T>` es contrato de respuesta, no sustituto de autorización. Consultar los contratos antes de cambiar endpoints.

## Pruebas
Validar casos de uso, fallos de negocio, pertenencia a tenant y compatibilidad de DTO. Compilar con `dotnet build Facturacion.slnx` desde la raíz.
