# Plan de implementación — Facturación comercial

**Estado:** plan técnico inicial basado en la inspección del repositorio (2026-11-09). No implica que las funcionalidades pendientes estén implementadas.

## Objetivo

Convertir Facturación Spain en un SaaS comercial completo para autónomos y pymes de España. El valor comercial del producto debe mantenerse aunque cambien o desaparezcan obligaciones de VERI*FACTU. La emisión definitiva seguirá sometida a las políticas fiscales aplicables: **no desactivar el bloqueo actual para simular emisión legal**.

## Inventario comprobado

| Área | Evidencia en el repositorio | Estado |
| --- | --- | --- |
| Identidad y multi-tenant | AuthController, TenantResolutionMiddleware, BillingDbContext, CompanyAuthorizationService | Base implementada; requiere validación continua |
| Empresas y usuarios | TenantAdministrationController, TenantInvitationsController | Flujos iniciales |
| Clientes | Customer, CustomerRepository, ICustomerRepository | Modelo y persistencia; falta API/CRUD comercial |
| Facturas | Invoice, InvoiceLine, InvoiceApplicationService, InvoicesController | Crear/consultar borradores; no hay CRUD completo |
| Cálculos | InvoiceApplicationService | IVA 0/4/10/21 y redondeo por línea; no es motor tributario completo |
| Emisión | InvoiceIssuanceService, FiscalSpecification | Flujo transaccional, bloqueado por XML/XSD |
| Cobros | InvoicePayment y repositorio | Modelo, sin flujo comercial completo identificado |
| Catálogo | Sin entidades Product/Service identificadas | Pendiente |
| Presupuestos | Sin entidad Quote/Budget identificada | Pendiente |
| Gastos | Sin entidad Expense identificada | Pendiente |
| Informes | Sin servicios de reporting identificados | Pendiente |
| B2B y AEAT | Contratos/Outbox; Worker solo registra heartbeat | Integración externa pendiente |

## Orden de entregas

### FC-01 — Clientes y contactos

- CRUD y búsqueda paginada de clientes por empresa y tenant.
- Validación de identificación fiscal, nombre, direcciones y datos de contacto según el alcance comercial definido.
- Prohibición de acceso cruzado entre empresas y tenants.
- Pruebas de permisos, duplicados, validación, paginación y aislamiento.

**Aceptación:** una empresa autorizada administra sus clientes sin poder consultar ni modificar los de otra.

### FC-02 — Catálogo comercial

- Productos y servicios separados por tipo, con referencias, descripción, unidad, precio, IVA por defecto y estado activo/inactivo.
- Precio y descripción copiados a cada línea de documento (snapshot): cambiar un producto no modifica facturas históricas.
- Índices y restricciones por empresa/tenant; migraciones para los cuatro proveedores de base de datos existentes.
- Pruebas de aislamiento, precios decimales, inactivación y referencias duplicadas.

### FC-03 — Presupuestos

- Borradores, líneas, impuestos, descuentos, caducidad, aceptación/rechazo y conversión a borrador de factura.
- Numeración comercial de presupuestos independiente de la numeración fiscal.
- Conversión idempotente, sin generar factura emitida.
- Pruebas de estados y concurrencia.

### FC-04 — Ciclo comercial de facturas

- Listado, filtros, edición y eliminación controlada de **borradores**, duplicación y vista previa.
- Motor de cálculo comercial reutilizable y probado (precisión, redondeo, impuestos, descuentos y escenarios admitidos).
- Estados comerciales explícitos, diferenciados del estado fiscal y del estado de pago.
- Documentos históricos inmutables tras emisión; rectificación mediante operación específica, no edición destructiva.
- La emisión definitiva debe seguir fallando de forma segura si no existe cumplimiento fiscal operativo.

### FC-05 — Cobros y vencimientos

- Registro de cobros parciales/completos, medios de pago, vencimientos y conciliación manual inicial.
- Saldo pendiente derivado de importes y cobros; sin equiparar «factura emitida» con «factura cobrada».
- Pruebas de cobros duplicados, sobrepagos, concurrencia y aislamiento.

### FC-06 — Gastos y proveedores

- Proveedores, gastos, categorías, adjuntos referenciados de forma segura, impuestos y pagos.
- Separar gastos comerciales de libros y declaraciones tributarias.
- Control de acceso y auditoría de operaciones sensibles.

### FC-07 — Informes y operativa SaaS

- Panel por empresa con ventas, cobros pendientes, antigüedad de saldos, gastos y evolución temporal.
- Filtros por periodo, moneda y estado; agregados siempre restringidos al tenant.
- Exportación CSV y reportes básicos con control de permisos.
- Pruebas de agregados, periodos y aislamiento.

### FC-08 — Endurecimiento y cierre del núcleo comercial

- Auditoría de autorizaciones, concurrencia, índices, migraciones, observabilidad y tratamiento de errores.
- Pruebas de extremo a extremo y documentación de instalación y operación.
- Comprobación de que el SaaS comercial funciona con el motor fiscal no disponible **sin permitir emisión fiscal ilegal**.
- Criterios de aceptación y regresión completos antes de declarar «terminado».

## Reglas arquitectónicas para todas las entregas

1. Inspeccionar el código real antes de cambiar contratos, entidades o esquema.
2. Mantener responsabilidades en Domain, Application, Infrastructure y Api; no introducir una arquitectura paralela.
3. Tenant del contexto autenticado, permisos por empresa, filtros de lectura y validación de escritura.
4. Cambios de esquema compatibles con MySQL, MariaDB, PostgreSQL y SQL Server; scripts/migraciones revisados.
5. Operaciones financieras transaccionales, idempotentes cuando corresponda, y con pruebas de concurrencia.
6. No alterar el comportamiento fail-closed de `InvoiceIssuanceService` ni falsificar aceptación de AEAT/B2B.
7. Mantener separadas factura comercial, registro fiscal, documento B2B, estado de transmisión y estado de cobro.
8. Entregas pequeñas mediante pull requests revisables; no fusionar automáticamente a `main`.

## Actualización FC-01 tras revisar `develop` (2026-11-09)

La rama `develop` **ya incluye** `CustomersController` con GET listado/detalle, POST, PUT y DELETE; `CustomersApiClient` con los cinco métodos; permisos `catalog.read` y `catalog.write` por empresa; comprobación de tenant en consultas; validación básica de campos y bloqueo de borrado de clientes con facturas. Por tanto, el CRUD básico **no está pendiente de implementar desde cero**. Esta información prevalece sobre el inventario inicial, realizado sobre una rama anterior.

**Pendiente para cerrar FC-01:** verificar pruebas de API y UI existentes, añadir cobertura de autorización/aislamiento de lectura y escritura, duplicados y concurrencia; revisar paginación y búsqueda del listado; validar reglas de identificación fiscal y correo según alcance acordado; comprobar que el borrado respeta todas las referencias y que la experiencia de UI cubre los casos comerciales. No afirmar que FC-01 está completo hasta ejecutar y pasar las pruebas.

## Avance FC-01 en `develop` (2026-11-09)

- El documento ya existía en `develop` al comenzar esta revisión; se ha preservado su versión más reciente.
- El CRUD básico de clientes ya estaba implementado en `CustomersController` y existe una pantalla web inicial.
- Se añadió detección de identificación fiscal duplicada por empresa en creación y actualización, con respuesta HTTP 409. La protección frente a concurrencia simultánea requiere aún una restricción única en base de datos y tratamiento de errores específico.
- **Pendiente:** búsqueda y paginación, pruebas de integración de API (permisos y aislamiento), validación formal de identificadores/correo, revisión de borrado y comprobación de build/test.
- **Sin validar:** no se han ejecutado `dotnet build` ni `dotnet test` desde el conector GitHub.

## Avance de FC-01 (2026-11-09)

Se añadió a `CustomersController` búsqueda opcional por nombre/NIF y paginación (`page`, `pageSize`, máximo 100), comprobación previa de duplicados de identificación fiscal por empresa y validación básica de correo y código de país. La respuesta del listado sigue siendo un array para mantener compatibilidad con `CustomersApiClient`; por tanto, todavía no proporciona recuento total ni metadatos de paginación. El cliente web aún no expone controles de búsqueda y páginas.

**Limitaciones pendientes:** la prevención de duplicados no es segura frente a concurrencia sin una restricción única en base de datos; revisar datos históricos y esquemas de los cuatro motores antes de introducirla. No se han ejecutado compilación ni pruebas. Faltan pruebas de API, validación fiscal específica de NIF/NIE/CIF, casos de eliminación con referencias y seguridad multi-tenant; FC-01 permanece abierto.

## Próximo cambio de código

**FC-01**: inspeccionar `Customer`, `ICustomerRepository`, `CustomerRepository`, autorizaciones, DTO y esquema SQL; implementar primero la API y los casos de uso de clientes con pruebas multi-tenant. No comenzar catálogo/presupuestos antes de estabilizar este contrato.

## Validación

Comandos previstos desde la raíz:

```bash
dotnet build Facturacion.slnx
dotnet test test/Facturacion.Integration.Test/Facturacion.Integration.Test.csproj
```

Este documento no afirma que esos comandos se hayan ejecutado.
