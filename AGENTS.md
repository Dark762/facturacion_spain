# AGENTS.md — Instrucciones para agentes de IA en FacturaciónES

> **Lee este archivo antes de analizar, generar o modificar código.** Estas instrucciones describen el contexto conocido; **el repositorio es la fuente de verdad**. Si algo no coincide, inspecciona el código y comunica la discrepancia.

## 1. Proyecto y propósito

**FacturaciónES** es una plataforma SaaS de facturación electrónica para empresas, autónomos y profesionales de España. Su objetivo es gestionar cuentas, tenants, empresas, usuarios, clientes, productos, facturas, suscripciones y procesos fiscales, con una arquitectura que permita reutilizar código en web, móvil y escritorio.

- Repositorio: `Dark762/facturacion_spain`
- Ruta habitual de desarrollo: `D:\proyectos\Facturacion_ESPN`
- Rama de trabajo **exclusiva: `develop`**. **Nunca modificar, fusionar ni publicar directamente en `main`.**
- IDE habitual: Visual Studio 2022.
- Plataforma principal: .NET 8, C#, Blazor/Razor Components, Entity Framework Core.
- Integraciones/infraestructura presentes o previstas: MySQL/MariaDB, otros proveedores SQL, Stripe, Aspire y herramientas de correo de desarrollo.
- Idioma de trabajo con el usuario: **español**.

El cumplimiento de VERI*FACTU y de la normativa española es un **objetivo**, no una garantía de cumplimiento ya verificado. Comprueba las reglas vigentes y el código existente antes de afirmar conformidad.

## 2. Arquitectura: primero inspeccionar, luego modificar

Revisa `Facturacion.slnx` y los proyectos reales. Entre los componentes conocidos están:

| Proyecto | Responsabilidad |
| --- | --- |
| `Facturacion.Domain` | Entidades, interfaces y reglas del dominio |
| `Facturacion.Application` | Contratos, casos de uso y servicios de aplicación |
| `Facturacion.Infrastructure` | EF Core, persistencia, Identity, autorización e integraciones |
| `Facturacion.Api` | Endpoints HTTP; autenticación, autorización y validación |
| `Facturacion.Client` | Clientes HTTP y DTO reutilizables |
| `Facturacion.UI.Shared` | Componentes Razor y modelos de presentación compartidos |
| `Facturacion.Web.App` | Área privada web; consume la API, no accede directamente a la base de datos |
| `Facturacion.Web.Public` | Experiencia web pública |
| Proyectos de test y otros módulos | Pruebas, configuración, pagos y procesos fiscales |

**Dirección de diseño:** API centralizada → clientes HTTP compartidos → componentes Razor compartidos → hosts web / futuros hosts .NET MAUI Blazor Hybrid.

No asumas que el host MAUI ya existe. Comprueba que `Facturacion.Client` y `Facturacion.UI.Shared` sean realmente compatibles con MAUI: revisa especialmente referencias a `Microsoft.AspNetCore.App` y dependencias exclusivas de servidor. No dupliques lógica ni crees una arquitectura paralela.

## 3. Seguridad y aislamiento SaaS: reglas innegociables

- **Tenant** y **Company** son ámbitos distintos; un tenant puede contener varias empresas.
- Nunca aceptes `TenantId` del navegador como fuente de autoridad. Obtén el tenant del contexto autenticado y validado.
- Verifica la pertenencia del usuario al tenant y sus permisos sobre la empresa **en cada operación**.
- Restringe consultas, escrituras y relaciones por `TenantId` **y** `CompanyId` cuando proceda.
- Conserva los filtros globales de EF Core en `BillingDbContext` y las protecciones de `SaveChanges`.
- No uses `IgnoreQueryFilters()` ni desactives autorización para «arreglar» una funcionalidad sin análisis de seguridad y aprobación.
- Valida en servidor; no dependas solo de validaciones Razor.
- Protege integridad referencial, trazabilidad e inmutabilidad fiscal cuando corresponda.
- Añade pruebas de acceso permitido/denegado y de aislamiento entre tenants y empresas.

La autorización del catálogo utiliza permisos `catalog.read` y `catalog.write` en la implementación reciente. Confirma los roles y su mapeo en el código actual.

## 4. Estado conocido de funcionalidades (verificar antes de actuar)

Se han desarrollado o iniciado:

- Registro SaaS, verificación de correo, planes y procesos relacionados con suscripciones.
- Componentes Razor compartidos: `WorkspaceOverview`, `WorkspaceShell`, `StatusPage`, `NotFoundContent`, `CustomersTable`, `ProductsTable`, `CustomerEditor`, `ProductEditor`.
- Clientes HTTP: `ApiClient`, `SaasApiClient`, `AuthenticationApiClient`, `InvoicesApiClient`, `CustomersApiClient`, `ProductsApiClient`.
- Endpoints CRUD de clientes y productos con restricciones por tenant y empresa.
- Entidad `Product`, configuración EF Core y `DbSet<Product>`.
- Páginas privadas de catálogo: `/catalog/{CompanyId}/customers` y `/catalog/{CompanyId}/products`.
- Pruebas HTTP con manejadores simulados.

**No declares estas funcionalidades «terminadas» sin comprobar compilación, integración, seguridad y ejecución real.**

Pendientes conocidos o que deben verificarse: autenticación y propagación de credenciales desde el área privada hacia la API; compatibilidad MAUI de bibliotecas compartidas; validación de integración; ejecución de migraciones; pruebas de seguridad y de base de datos.

## 5. Persistencia y migraciones

Inspecciona primero el proveedor de datos activo, el esquema existente y las configuraciones EF. El repositorio contiene scripts de instalación y actualización para varios motores.

- Script reciente de actualización de productos MySQL/MariaDB: `database/upgrade/06_products_mysql_mariadb.sql`.
- **No asumas que ese script ya se ejecutó.**
- Los scripts `01_full_schema.sql` pueden ser **destructivos**; no los ejecutes sobre una base con datos.
- Prefiere actualizaciones no destructivas, compatibles con el proveedor real y respaldadas por copia de seguridad.
- No alteres o borres datos reales sin autorización explícita.
- Verifica tipos SQL, claves compuestas, índices, nombres de tablas y compatibilidad con el mapeo EF.

## 6. Reglas de Git y cambios

1. Antes de cualquier trabajo ejecuta `git branch --show-current` y `git status --short`.
2. **Solo trabaja en `develop`.** Si no estás en esa rama, detente y solicita o realiza un cambio de rama seguro, sin perder modificaciones.
3. No modifiques `main`, no hagas merge hacia `main` y no hagas push sin autorización.
4. Respeta cambios locales preexistentes; no sobrescribas archivos ajenos.
5. No ejecutes `git reset --hard`, `git clean -fd`, `git push --force` ni acciones destructivas sin aprobación expresa.
6. Mantén cambios pequeños, coherentes y revisables; explica los archivos afectados.
7. No incluyas secretos, tokens, contraseñas ni datos fiscales reales en código, pruebas o logs.

## 7. Estilo de implementación

- Respeta límites entre Domain, Application, Infrastructure, API, Client y UI.
- Usa DI, DTO, contratos HTTP tipados, `CancellationToken` y manejo explícito de errores.
- La interfaz compartida no debe acceder directamente a `DbContext`.
- No introduzcas paquetes NuGet, cambios de proveedor SQL o actualizaciones mayores de framework sin justificarlo.
- Prefiere componentes y servicios reutilizables antes que duplicación.
- Mantén .NET 8 salvo autorización para actualizarlo.
- Corrige las causas de los errores, no ocultes advertencias ni desactives protecciones.
- Verifica que los modos de renderizado Blazor y sus `@using` estén correctamente configurados.

## 8. Validación obligatoria cuando el entorno lo permita

Desde la raíz del repositorio:

```powershell
dotnet restore Facturacion.slnx
dotnet build Facturacion.slnx
dotnet test Facturacion.slnx
```

Añade pruebas pertinentes para cambios en CRUD, permisos, validación, serialización, persistencia y aislamiento multi-tenant. Si no puedes ejecutar herramientas, **indícalo claramente**; nunca inventes resultados ni afirmes que las pruebas pasaron.

## 9. Protocolo de trabajo con el usuario

### Primera interacción con este repositorio

**No modifiques ningún archivo.** Primero:

1. Verifica ruta, rama y estado de Git.
2. Lee `Facturacion.slnx`, estructura de proyectos y referencias.
3. Revisa puntos de entrada de API, Web.App y Web.Public.
4. Inspecciona autenticación, tenant, permisos, `BillingDbContext` y persistencia.
5. Revisa clientes HTTP, UI compartida, migraciones y tests.
6. Entrega un informe en español con: arquitectura real, funcionalidades existentes, pendientes, riesgos, estado de Git y plan de trabajo priorizado.
7. **Espera instrucciones antes de implementar.**

### Tareas posteriores

- **Analiza:** inspecciona archivos relevantes, dependencias, riesgos y estado de Git.
- **Propón:** resume el problema, la solución, los archivos a tocar y cómo verificarás el cambio.
- **Implementa:** solo si el usuario lo solicita claramente o lo autoriza.
- **Valida:** ejecuta compilación, tests y comprobaciones de seguridad pertinentes.
- **Informa:** enumera archivos cambiados, resultados reales, limitaciones y pendientes.

**Principio rector:** preservar integridad fiscal, aislamiento multi-tenant, seguridad, mantenibilidad y compatibilidad multiplataforma. La prioridad no es únicamente compilar: es implementar correctamente y demostrarlo.
