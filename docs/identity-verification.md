# Identidad y representación: implementación inicial (NO apta para producción)

## Estado real

Se implementó la **base de datos y la solicitud inicial** de dos vías, NO la verificación de identidad completa:

- `POST /api/identity-verification` con `{"method":"Manual"|"Certificate","subjectKind":"Individual"|"LegalEntity"}` (requiere JWT de tenant válido).
- `GET /api/identity-verification/mine` muestra solicitudes del usuario autenticado en su tenant.
- `GET /api/platform/identity-verifications` muestra solicitudes a la identidad PlatformAdmin, con paginación y filtro de estado.
- `IdentityVerifications` almacena estado, método, tenant, usuario y auditoría mínima, **sin documentos ni certificados privados**.
- Aplicar antes de iniciar el host el script `database/upgrade/11_identity_verifications_<proveedor>.sql` correspondiente, previa copia de seguridad.

Los estados iniciales son `AwaitingEvidence` (manual) y `AwaitingCertificate` (certificado). No existe endpoint que pueda marcar una solicitud como `Verified` en esta fase: impediría una falsa sensación de verificación.

## Para completar de forma segura la vía manual

Diseñar almacenamiento privado cifrado de evidencias, antivirus, límites de tamaño y tipo, política de conservación y borrado, permisos segregados, trazabilidad inmutable de accesos y decisiones, revisión de identidad de la persona y **por separado** de representación de la sociedad. Evitar exposición de DNI/NIE en logs o URLs. Definir proveedor de comprobación documental y base jurídica RGPD antes de recopilar documentación.

## Para completar de forma segura la vía certificado

Configurar autenticación TLS con certificado cliente o integración de proveedor de identificación cualificado/confiable; comprobar cadena de confianza, revocación, vigencia, políticas y correspondencia de la identidad certificada con el usuario y, en su caso, facultades de representación. **No aceptar certificados públicos subidos por el usuario ni cabeceras HTTP arbitrarias como prueba de posesión de clave privada.** En IIS, cualquier terminación TLS o proxy debe garantizar que la identidad del certificado solo procede de infraestructura confiable. Nunca almacenar claves privadas.

## Bloqueos de operaciones

**Todavía no se ha integrado una política de autorización** que bloquee emisión de facturas, acceso fiscal, representación ni integraciones. Tampoco hay pantalla UI, carga de documentos, firma/challenge criptográfico, decisiones de revisión, caducidad/revocación, auditoría completa ni pruebas end-to-end. No presentar este módulo como una verificación KYC funcional ni habilitar producción con la suposición de que protege operaciones.

## Pruebas imprescindibles

Verificar aislamiento por tenant y usuario, tokens de plataforma, solicitudes duplicadas, carreras concurrentes, migraciones en cada motor, acceso administrativo, no exposición de datos y revocación. Ejecutar `dotnet build` y tests de integración antes de desplegar.
