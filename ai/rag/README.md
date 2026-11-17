# RAG local en C# / .NET 10

Esta implementación se ejecuta en el proyecto `src/Facturacion.AI`. Usa **Ollama** para embeddings, **SQLite** para persistencia y **MCP stdio** para que Cline consulte el índice. No necesita Python.

## 1. Instalar y crear el índice

Desde PowerShell en la raíz del repositorio:

```powershell
git switch develop
git pull origin develop
ollama pull nomic-embed-text
dotnet restore src/Facturacion.AI/Facturacion.AI.csproj
dotnet run --project src/Facturacion.AI -- index
```

La primera indexación puede tardar. El índice queda en `ai/.local/rag-dotnet.sqlite3` (ignorado por Git). Para reindexar tras cambios, repite `index`. Se procesan archivos admitidos en `docs/`, `src/`, `test/` y `ai/knowledge/`.

## 2. Buscar información

```powershell
dotnet run --project src/Facturacion.AI -- search "Cómo se calcula la huella VERI*FACTU"
```

Devuelve ruta, fragmento, puntuación de similitud y texto recuperado. La puntuación no demuestra corrección legal.

## 3. Conectar con Cline (MCP)

**Compila antes de configurar MCP** para evitar que `dotnet run` escriba mensajes de compilación en stdout del protocolo:

```powershell
dotnet build src/Facturacion.AI/Facturacion.AI.csproj -c Release
```

Añade el servidor en el fichero MCP que abra Cline, ajustando la ruta absoluta:

```json
{
  "mcpServers": {
    "facturacion-rag": {
      "command": "dotnet",
      "args": ["C:/Proyectos/facturacion_spain/src/Facturacion.AI/bin/Release/net10.0/Facturacion.AI.dll", "serve"],
      "disabled": false
    }
  }
}
```

El servidor local expone `search_fiscal_knowledge`, una herramienta de consulta de solo lectura. No edita el proyecto ni realiza envíos fiscales.

**Importante:** inicia Cline con el repositorio como directorio de trabajo, porque el servidor busca `Facturacion.slnx` desde el directorio actual. Si Cline lo inicia desde otro directorio, configura su campo `cwd` (si está disponible) a la raíz del repositorio, o utiliza un script de arranque que cambie al directorio antes de lanzar el proceso.

## 4. Comprender RAG frente a entrenamiento

1. **Ingesta:** `FilesToIndex()` selecciona archivos y excluye rutas comunes de compilación y secretos.
2. **Chunking:** `Split()` divide el texto en fragmentos solapados.
3. **Embeddings:** `EmbedAsync()` llama a la API local `/api/embed` de Ollama.
4. **Persistencia:** `IndexAsync()` guarda fragmentos y vectores en SQLite.
5. **Retrieval:** `SearchAsync()` calcula similitud coseno y devuelve los fragmentos más cercanos.
6. **Uso por el LLM:** Cline solicita `search_fiscal_knowledge` y utiliza los resultados como contexto.

**RAG no entrena pesos.** Para aprender fine-tuning, reúne tareas aprobadas por una persona, contexto, diffs y resultados de pruebas en un conjunto de evaluación. Después considera entrenar un modelo pequeño (3B–7B) o usar una GPU remota; 30B no es práctico para fine-tuning con una RTX 4060 de 8 GB.

## 5. Seguridad y límites

- La base SQLite y los embeddings se almacenan localmente; no subas facturas reales, secretos, NIF o certificados al índice.
- Los filtros por nombre de archivo **no garantizan** detectar información sensible dentro del contenido.
- El código no descarga ni valida automáticamente normativa actualizada. Consulta el BOE y la AEAT para vigencia y requisitos legales.
- La búsqueda vectorial actual es lineal; para índices grandes conviene introducir búsqueda híbrida y un motor vectorial especializado.
- El servidor MCP usa mensajes JSON-RPC por línea sobre stdio. Verifica la compatibilidad con tu versión de Cline.
- No se han ejecutado compilación ni pruebas desde GitHub; compruébalas en tu equipo con `dotnet build Facturacion.slnx` y `dotnet test Facturacion.slnx`.
