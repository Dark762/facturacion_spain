# SDD — Verificación de identidad y representación fiscal (Facturación Spain)
Versión 1.0 · 17-11-2026 · Rama develop

## 1. Objetivo
Verificar, cuando el riesgo o el trámite lo requiera, la identidad de autónomos y representantes de sociedades, sin convertir KYC en condición general para emitir facturas. VERI*FACTU es una funcionalidad de cumplimiento y no el producto principal.

## 2. Estado de partida
Existen solicitudes Manual/Certificate, consulta de estado, cola administrativa, revisión, rechazo, cancelación y revocación. No existe todavía una verificación real de certificado ni comprobación de representación; readiness es informativo y no habilita emisión.

## 3. Arquitectura
- Facturacion.Api: endpoints autenticados, obtención de certificado desde conexión TLS autenticada; jamás desde JSON, fichero o cabeceras arbitrarias.
- Facturacion.Application: orquestación y políticas de verificación.
- Facturacion.Infrastructure: evidencias mínimas, estados, auditoría y adaptadores AEAT/proveedores.
- Facturacion.Web.App: experiencia de usuario y estados; no toma decisiones de confianza.
- Gateway: terminación mTLS dedicada, confianza restringida y transmisión autenticada de identidad a la API. Si la API no puede comprobar la procedencia de la identidad TLS, deniega.

## 4. Estados separados
IdentityStatus (Pending, Verified, Rejected, Expired, Revoked), FiscalMatchStatus (Unknown, Match, Mismatch, Unavailable), RepresentationStatus (NotApplicable, Pending, Verified, Rejected, Expired), MandateStatus (NotRequired, Pending, Valid, Revoked). Nunca inferir representación o mandato de la identidad personal.

## 5. Etapa 1 — Certificado X.509
1. Aceptar solo certificados cliente presentados mediante un canal TLS autenticado.
2. Verificar cadena de confianza con raíces/intermedias configuradas explícitamente, EKU de autenticación cliente cuando aplique, vigencia, política de revocación online y fallo cerrado.
3. Extraer atributos X.509 (OID 2.5.4.97, 2.5.4.5, 2.5.4.3, 2.5.4.4, 2.5.4.42) sin asumir que un único OID identifica universalmente al sujeto fiscal.
4. Reconocer perfil de certificado mediante políticas explícitas por emisor; los perfiles no reconocidos se revisan, no se aceptan automáticamente.
5. Asociar prueba al usuario/tenant y solicitud vigentes; no conceder Verified únicamente por poseer un certificado.
6. Registrar huella SHA-256, emisor, serie, expiración y resultado de validación, sin guardar claves privadas ni el certificado íntegro por defecto.

### Pruebas etapa 1
Certificado sintético válido bajo raíz de prueba, expirado, no confiable, sin EKU adecuado, revocado, revocación no disponible, atributos ausentes, suplantación de cabeceras, tenant ajeno y reintento concurrente. La raíz sintética solo en entorno de test.

## 6. Etapa 2 — Identidad fiscal y representación
Cotejar NIF certificado con titular fiscal registrado; comprobar separadamente la representación de la sociedad y sus poderes vigentes. No sobrescribir datos fiscales sin trazabilidad y consentimiento/causa adecuada.

## 7. Etapa 3 — AEAT
Implementar adaptador SOAP exclusivamente tras verificar contrato WSDL, autorización de acceso, credenciales y respuestas oficiales. No asumir estados inexistentes ni que una coincidencia censal concede capacidad para facturar. Sandbox simulado aislado de producción.

## 8. Etapa 4 — Mandatos
La autorización para remitir registros a AEAT se documenta y comprueba separadamente; no basta con un booleano ni con firmar cualquier PDF. Revisar régimen de colaboración social, apoderamiento y autorizaciones aplicables.

## 9. Seguridad y privacidad
Cifrado en reposo, mínimo dato necesario, retención y borrado, trazabilidad append-only, no XML AEAT crudo en logs, protección contra replay, idempotencia, autorización por tenant, rate limits y revisión RGPD. No usar certificados de prueba para otorgar estados de producción.

## 10. Criterios de aceptación
Compilación y tests unitarios/integración, TLS verificado en infraestructura real, OCSP/CRL efectivo, matrices de certificados admitidos, pruebas multi-tenant y de representación, validación del contrato AEAT, evidencias auditables y política de despliegue. Sin esos requisitos, no se declara producción lista.

## 11. Plan de entregas
E1 biblioteca de parsing y validación X.509, pruebas, configuración mTLS y endpoint seguro; E2 cotejo fiscal y representación; E3 AEAT; E4 mandato; E5 integración, hardening y pruebas E2E.

## 12. Riesgos y decisiones abiertas
Seleccionar gateway (Kestrel/IIS/Nginx), certificados de confianza y mecanismo de revocación, política de certificados FNMT/DNIe/Camerfirma, acceso AEAT, evidencia de representación y base jurídica de tratamiento. No bloquear emisión comercial por una verificación opcional no disponible.
