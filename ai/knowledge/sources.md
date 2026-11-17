# Registro de fuentes oficiales (para futura ingesta RAG)

Antes de indexar un documento, registra: **organismo, título, URL, fecha de consulta, versión/fecha de publicación, checksum SHA-256, ámbito y vigencia comprobada**. Una URL no equivale a una norma vigente.

| Ámbito | Fuente inicial | Uso |
| --- | --- | --- |
| RRSIF / VERI*FACTU | https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/informacion-tecnica.html | Documentación técnica AEAT, esquemas y especificaciones |
| Normativa | https://www.boe.es/ | Textos consolidados y disposiciones originales; comprobar vigencia |
| Código y pruebas | `src/Facturacion.Verifactu/` y `test/Facturacion.Integration.Test/` | Comportamiento implementado y pruebas existentes |
| Documentación local | `docs/verifactu/` | Material técnico ya incluido en el repositorio |

## Política de ingesta
1. Descargar manualmente o mediante un proceso auditado desde el organismo oficial.
2. Registrar fecha, versión y checksum antes de fragmentar.
3. Mantener separado el texto legal de comentarios e interpretaciones.
4. Reindexar cuando cambien normas, esquemas o documentación técnica.
5. Exigir cita de fuente en cada respuesta sobre requisitos fiscales.
6. No indexar información personal, certificados, secretos ni datos de producción.

**Estado:** inventario inicial; ninguna fuente externa se ha descargado o validado como vigente en esta configuración.
