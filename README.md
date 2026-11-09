# Facturación Spain

Plataforma **SaaS de facturación comercial para autónomos, pymes y empresas en España**, desarrollada en **C# y .NET 8**. El objetivo es gestionar la operativa de facturación y hacer evolucionar el producto independientemente de los cambios regulatorios.

> **Estado:** proyecto en desarrollo. **No está listo para emitir facturas fiscales reales ni para afirmar cumplimiento VERI*FACTU**: el motor fiscal bloquea la emisión mientras falten componentes obligatorios. No utilizar en producción sin completar y validar el ciclo fiscal, la operativa y la seguridad.

## Principios del producto

- **La facturación comercial es el producto principal**: borradores, clientes, empresas, facturas, usuarios, roles, suscripciones e integraciones.
- **VERI*FACTU es una capacidad de cumplimiento normativo**, separada del modelo comercial. Un aplazamiento, modificación o derogación de la obligación no debe inutilizar el SaaS.
- Separación entre dominio, casos de uso, infraestructura, API, motor fiscal e integraciones externas.
- **Multi-tenant**: la identidad del tenant procede del contexto autenticado y se aplica en consultas y escrituras, no de un identificador arbitrario en el cuerpo HTTP.
- Los estados del código determinan la funcionalidad disponible; tener entidades, contratos o esquemas no implica que una integración esté operativa.

## Solución

| Proyecto | Responsabilidad actual |
| --- | --- |
| [Facturacion.Api](src/Facturacion.Api/README.md) | API ASP.NET Core, JWT, controladores, CORS y resolución del tenant |
| [Facturacion.Application](src/Facturacion.Application/README.md) | Casos de uso, DTO, resultados, interfaces y orquestación de facturas |
| [Facturacion.Domain](src/Facturacion.Domain/README.md) | Entidades, enumeraciones e invariantes de negocio |
| [Facturacion.Infrastructure](src/Facturacion.Infrastructure/README.md) | EF Core, repositorios, proveedores de base de datos, Identity, alta SaaS, correo e invitaciones |
| [Facturacion.Verifactu](src/Facturacion.Verifactu/README.md) | Canonicalización, huellas, cadena fiscal y estado explícito de componentes normativos |
| [Facturacion.B2B](src/Facturacion.B2B/README.md) | Proyecto reservado para una futura integración B2B; actualmente contiene una clase plantilla |
| [Facturacion.Payments.Stripe](src/Facturacion.Payments.Stripe/README.md) | Adaptador de Stripe Checkout y verificación de webhooks |
| [Facturacion.Workers](src/Facturacion.Workers/README.md) | Host de tareas en segundo plano; actualmente solo registra un heartbeat |
| [Pruebas de integración](test/Facturacion.Integration.Test/README.md) | Pruebas de API, aislamiento de tenants, emisión, seguridad, Stripe y motor fiscal |

La solución está definida en `Facturacion.slnx`. El directorio `database/` incluye scripts de esquema para **MariaDB, MySQL, PostgreSQL y SQL Server**, además de scripts de actualización fiscal. `docs/verifactu/` contiene referencias técnicas y XSD; su presencia no demuestra que el motor XML esté implementado.

## Funcionalidad y límites actuales

- **Disponible en código:** autenticación JWT con ASP.NET Identity, pertenencia a tenants y empresas, permisos por empresa, creación/consulta de borradores, alta SaaS, invitaciones y preparación de Checkout con Stripe.
- **Emisión:** existe un caso de uso transaccional para numeración definitiva, cadena fiscal y Outbox, pero falla de manera segura cuando `IFiscalEngine.IsReadyForIssuance` es falso.
- **Fiscal:** canonicalización y SHA-256 contrastados mediante vectores oficiales según las pruebas incluidas; generación de XML, validación XSD y QR siguen registrados como no disponibles. **La emisión está bloqueada por XML/XSD.**
- **AEAT y B2B:** hay contratos, servicios que encolan mensajes y modelos de persistencia; no hay consumidor operativo del Outbox ni envío externo completo.
- **Worker:** todavía no procesa la cola Outbox.
- **B2B:** el ensamblado específico es por ahora una plantilla; parte de la lógica preparatoria reside en Application/Infrastructure.

No confundir **Queued/Processing** con presentación, aceptación o confirmación por una plataforma externa.

## Requisitos y arranque para desarrollo

1. Instalar **SDK .NET 8** y un servidor de base de datos compatible con el proveedor elegido.
2. Preparar una base de datos de desarrollo aislada. Revisar los scripts en `database/<proveedor>/01_full_schema.sql` y los scripts de `database/upgrade/` antes de aplicarlos; **no ejecutarlos a ciegas sobre datos existentes**.
3. Configurar la API mediante variables de entorno o secretos de usuario, sin incluir credenciales en el repositorio. Son esenciales `Database__Provider`, `ConnectionStrings__Default`, `Jwt__SigningKey` (mínimo 32 bytes), `Jwt__Issuer` y `Jwt__Audience`. El correo SMTP y Stripe requieren credenciales reales para sus flujos.
4. Desde la raíz del repositorio:

```bash
dotnet restore Facturacion.slnx
dotnet build Facturacion.slnx
dotnet run --project src/Facturacion.Api/Facturacion.Api.csproj
```

Swagger se habilita solamente en entorno `Development`. La sección `Cors:AllowedOrigins` determina los orígenes permitidos; una lista vacía no autoriza ningún origen. Consultar [la documentación de la API](src/Facturacion.Api/README.md) para las rutas.

Configuración típica, mediante variables de entorno (valores ilustrativos):

```text
Database__Provider=MySql
ConnectionStrings__Default=<cadena de conexión privada>
Jwt__SigningKey=<secreto aleatorio de al menos 32 bytes>
Jwt__Issuer=Facturacion.Api
Jwt__Audience=Facturacion.Client
```

No registrar secretos, tokens, datos de facturación o credenciales en logs. **No iniciar emisión real** hasta completar validaciones normativas y pruebas con el entorno correspondiente.

## Pruebas

```bash
dotnet test test/Facturacion.Integration.Test/Facturacion.Integration.Test.csproj
```

Las pruebas incluyen escenarios de infraestructura y requieren revisar las condiciones de cada fixture. No se ha verificado la compilación ni se han ejecutado pruebas durante esta actualización documental.

## Directrices de evolución

- Mantener la factura comercial y los datos del negocio independientes del transporte y de los formatos fiscales.
- Rechazar operaciones fiscales incompletas antes de asignar numeración definitiva; conservar atomicidad y aislamiento de tenant.
- Completar XML/XSD/QR, envío AEAT, procesamiento Outbox, integración B2B y pruebas extremo a extremo antes de prometer disponibilidad.
- Antes de cambiar el diseño, **inspeccionar las implementaciones reales**; no dar por existentes funcionalidades solo porque aparezcan interfaces, DTO o referencias.
- Revisar periódicamente la normativa vigente en fuentes oficiales antes de publicar declaraciones de cumplimiento.

## Alcance de la documentación

Cada README describe **el estado observado en el código**, no una garantía de producción ni una certificación fiscal. La fecha de actualización documental no sustituye la revisión de especificaciones oficiales.
