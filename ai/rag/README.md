# RAG local para facturacion_spain

Implementación sin dependencias de terceros: Python 3.10+, Ollama para embeddings y SQLite como índice vectorial. Funciona **localmente** y expone una herramienta MCP de **solo lectura** para Cline.

## 1. Preparar embeddings

Desde PowerShell en la raíz del repositorio:

```powershell
ollama pull nomic-embed-text
python ai/rag/rag.py index
python ai/rag/rag.py search "¿Cómo se calcula la huella de un registro de alta?"
```

Ollama debe estar ejecutándose en `http://localhost:11434`. La primera indexación puede tardar varios minutos. Se indexan `docs/`, `src/`, `test/` y `ai/knowledge/` (solo archivos de texto con extensiones permitidas); archivos grandes, secretos, binarios y directorios de compilación se omiten. La base local queda en `ai/.local/rag.sqlite3` y no se sube a Git.

Para refrescar después de un cambio de código o documentación, repite `python ai/rag/rag.py index`. El indexador detecta cambios por SHA-256 y elimina entradas de archivos borrados. **No descarga automáticamente normas del BOE o la AEAT.** Incorpora únicamente documentación oficial autorizada y registra su procedencia en `ai/knowledge/sources.md`.

## 2. Conectar a Cline por MCP

En la configuración de servidores MCP de Cline, añade un servidor como este. Sustituye `C:/ruta/facturacion_spain` por la ruta absoluta a tu clon y utiliza el ejecutable de Python correcto:

```json
{
  "mcpServers": {
    "facturacion-rag": {
      "command": "python",
      "args": ["C:/ruta/facturacion_spain/ai/rag/rag.py", "serve"],
      "disabled": false
    }
  }
}
```

La ubicación y el formato del archivo de configuración de Cline pueden variar según su versión; añade este objeto al fichero MCP que Cline muestre en su interfaz, sin sobrescribir otros servidores.

Herramienta disponible: `search_fiscal_knowledge(query, limit)`. Solo devuelve fragmentos con rutas y puntuación de similitud. **No edita archivos ni consulta internet**. El agente debe contrastar vigencia, fuentes y versiones de requisitos legales antes de generar código fiscal.

Prueba sugerida en Cline: «Usa search_fiscal_knowledge para localizar cómo se calcula la huella y dime qué archivos del repositorio la implementan. No edites nada».

## 3. Diagnóstico

- Si falla `ollama pull`, verifica Ollama y tu conexión durante la descarga inicial.
- Si `index` informa de error de embeddings, comprueba `ollama list` y el modelo `nomic-embed-text`.
- Si Cline no detecta la herramienta, revisa ruta absoluta, intérprete Python y registro MCP.
- Si no aparecen resultados nuevos, vuelve a ejecutar `index`.
- Si cambias `FACTURACION_EMBED_MODEL`, reindexa. Solo se buscan vectores del modelo configurado.
- Opcional: configura `OLLAMA_HOST`, `FACTURACION_EMBED_MODEL` o `FACTURACION_RAG_DB` como variables de entorno.

## Seguridad y límites

El indexador excluye patrones comunes de secretos, pero **no puede garantizar detectar datos personales dentro de un archivo de código o documento**. Revisa qué incorporas antes de indexar. No añadas facturas reales, credenciales ni certificados. El índice no es una fuente de verdad jurídica, no realiza comprobación automática de vigencia y no reemplaza las pruebas XSD y fiscales. Las puntuaciones son similitudes, no niveles de certeza jurídica.
