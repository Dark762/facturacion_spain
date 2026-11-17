# Biblioteca documental para Facturacion.AI

Esta carpeta contiene documentos que el RAG local puede indexar. Mantén separados **documentos oficiales**, **notas internas** y **código/esquemas técnicos**. No se debe interpretar un archivo indexado como prueba de vigencia legal.

## Organización

- [VERI*FACTU](verifactu/README.md): RRSIF, normativa, especificaciones técnicas, esquemas XSD, preguntas frecuentes y ejemplos.
- [Factura electrónica B2B](facturacion-b2b/README.md): regulación y especificaciones propias de la factura electrónica entre empresas.
- [IVA](iva/README.md): normativa y criterios generales de IVA.
- [Arquitectura](arquitectura/README.md): decisiones técnicas y diseño de la aplicación.
- [Guías internas](guias-internas/README.md): procedimientos y notas de desarrollo, claramente identificadas como no oficiales.

## Antes de añadir documentación

1. Obtén el documento de una fuente legítima, preferiblemente BOE o AEAT.
2. Conserva título, organismo, URL, fecha de publicación, versión y fecha de consulta en [ai/knowledge/sources.md](../ai/knowledge/sources.md).
3. Guarda documentos de texto en UTF-8. El indexador actual admite `.md`, `.txt`, `.xml`, `.xsd`, `.json` y código; **no extrae texto de PDF ni DOCX**.
4. No incluyas datos de clientes, NIF reales, certificados, claves privadas ni facturas reales.
5. Ejecuta `dotnet run --project src/Facturacion.AI -- index` desde la raíz del repositorio.

Los documentos existentes en `docs/verifactu/` se conservan en sus rutas originales para evitar romper referencias.
