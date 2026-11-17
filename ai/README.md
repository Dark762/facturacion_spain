# Agente local para facturación española

Este directorio prepara un asistente de programación **local** para `facturacion_spain`. No entrena los pesos del modelo ni certifica cumplimiento normativo. Combina reglas del repositorio, recuperación de documentos (RAG, por implementar) y validación automatizada.

## Requisitos
- Ollama con `qwen3-coder:30b` instalado.
- VS Code con Cline (o agente compatible con Ollama).
- SDK .NET correspondiente a los proyectos.

## Inicio rápido
1. Abre la carpeta raíz del repositorio en VS Code.
2. En Cline, selecciona proveedor **Ollama**, URL `http://localhost:11434` y modelo `qwen3-coder:30b`.
3. Conserva aprobación manual de edición y ejecución de comandos.
4. Comprueba la lectura de `.clinerules` y solicita: «Analiza Facturacion.slnx y FiscalSpecification.cs; enumera componentes bloqueantes sin editar archivos».
5. Para probar el proyecto ejecuta, desde la raíz: `dotnet build Facturacion.slnx` y `dotnet test Facturacion.slnx`.

### Personalización opcional en Ollama
Desde la raíz del repositorio:

```powershell
ollama create facturacion-csharp -f ai/Modelfile
ollama run facturacion-csharp
```

Después selecciona `facturacion-csharp` en Cline. El Modelfile solo añade instrucciones; **no es fine-tuning**. Los modelos locales no generan cargos por tokens de API, pero consumen recursos de tu PC.

## Próxima fase: RAG verificable
- Ingerir solo documentos oficiales identificados en `ai/knowledge/sources.md`, conservando URL, versión, fecha y checksum.
- Separar índices de normativa fiscal, esquemas técnicos y código del repositorio.
- Crear búsqueda local (por ejemplo Qdrant + embeddings locales) y exponerla al agente mediante una herramienta MCP de solo lectura.
- Devolver fragmentos citados, con fecha, y negarse a resolver ambigüedades legales sin fuentes vigentes.
- No almacenar datos de clientes, certificados o facturas reales en el índice.

## Evaluación antes de fine-tuning
Usa `ai/evaluations/README.md` como punto de partida. Guarda ejemplos **aprobados por una persona** y parches reproducibles; no entrenes con respuestas autogeneradas sin revisión. Con RTX 4060 de 8 GB, el fine-tuning de 30B no es una opción local práctica; considera un modelo menor o hardware externo.

## Límites
No se ha incorporado todavía un servidor RAG ni una colección documental descargada. La validación jurídica requiere consultar la versión vigente de cada fuente. No actives operaciones fiscales reales con el agente sin revisión.
