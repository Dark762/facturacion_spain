# Instalación de Aider + Ollama para FacturaciónES

**Entorno:** Windows 10/11 · PowerShell · Visual Studio 2022 · .NET 8  
**Repositorio:** `Dark762/facturacion_spain` · **rama de trabajo:** `develop`  
**Ruta local habitual:** `D:\proyectos\Facturacion_ESPN`

> Esta guía interpreta «aida» como **Aider**, el asistente de programación por terminal. Si te refieres a otro programa llamado AIDA, sus pasos serán distintos.
>
> Los comandos se proponen para una instalación local. Revisa las páginas oficiales por si cambian versiones, opciones o requisitos.

## 1. Antes de instalar

1. Abre **PowerShell** o **Windows Terminal**.
2. Comprueba que Git y Python estén disponibles:

   ```powershell
   git --version
   py --version
   ```

3. Comprueba que .NET 8 esté instalado:

   ```powershell
   dotnet --info
   ```

4. Si falta Python, instálalo desde https://www.python.org/downloads/windows/ (marca la opción de añadir Python al PATH si aparece). Si falta Git, utiliza https://git-scm.com/download/win.
5. Para modelos locales conviene disponer de memoria RAM suficiente. Un modelo de 7B cuantizado puede requerir varios GB de RAM; deja margen adicional para Windows, Visual Studio y la solución. Una GPU compatible puede mejorar el rendimiento, pero no es obligatoria.

## 2. Instalar Ollama en Windows

1. Descarga el instalador oficial desde **https://ollama.com/download/windows** y ejecútalo. Como alternativa, si el identificador sigue disponible en tu instalación de WinGet:

   ```powershell
   winget install --id Ollama.Ollama -e
   ```

2. Cierra y vuelve a abrir PowerShell para actualizar el PATH.
3. Comprueba la instalación:

   ```powershell
   ollama --version
   ```

4. Inicia Ollama desde el menú Inicio si todavía no se está ejecutando. En Windows normalmente queda disponible en segundo plano.
5. Verifica que responde el servidor local:

   ```powershell
   Invoke-RestMethod http://127.0.0.1:11434/api/tags
   ```

   Si no responde, abre Ollama y vuelve a probar. En instalaciones donde no esté iniciado automáticamente, puedes ejecutar `ollama serve` en una terminal dedicada; evita arrancar un segundo servidor si ya está funcionando.

## 3. Descargar un modelo para programar

Para comenzar con un modelo razonablemente ligero:

```powershell
ollama pull qwen2.5-coder:7b
```

Comprueba que se descargó:

```powershell
ollama list
```

Pruébalo directamente:

```powershell
ollama run qwen2.5-coder:7b
```

Escribe una pregunta breve y usa `/bye` para salir.

**Elección del modelo:** si el equipo tiene poca memoria, puedes probar `qwen2.5-coder:3b`; si dispone de más recursos, evalúa modelos mayores. Los modelos pequeños pueden cometer errores al modificar proyectos grandes. El tamaño disponible y las etiquetas concretas pueden cambiar: consulta https://ollama.com/library.

## 4. Instalar Aider

Aider necesita Python. La opción recomendada por el proyecto es su instalador:

```powershell
py -m pip install --upgrade aider-install
py -m aider_install
```

Si el segundo comando no funciona en tu entorno, consulta las instrucciones oficiales en https://aider.chat/docs/install.html y utiliza el método vigente que indiquen.

Cierra y vuelve a abrir PowerShell. Comprueba:

```powershell
aider --version
```

**Alternativa con pipx:** si utilizas `pipx`, puedes instalar Aider en un entorno aislado. No mezcles varias instalaciones sin necesidad.

## 5. Preparar el repositorio sin perder cambios

Abre PowerShell en la carpeta del proyecto:

```powershell
cd D:\proyectos\Facturacion_ESPN
git status --short
git branch --show-current
```

**Debes estar en `develop`.** Si hay cambios sin guardar o sin confirmar, revísalos antes de cambiar de rama. No ejecutes `git reset --hard`, `git clean -fd` ni `git push --force`.

Si el árbol está limpio y necesitas cambiar de rama:

```powershell
git switch develop
```

Actualiza únicamente cuando sea seguro:

```powershell
git pull --ff-only origin develop
```

Verifica que existe el archivo de contexto del proyecto:

```powershell
Test-Path .\AGENTS.md
```

Si devuelve `False`, recupera `AGENTS.md` desde `develop` antes de continuar.

## 6. Conectar Aider con Ollama

Asegúrate de que Ollama esté abierto y que el modelo aparezca en `ollama list`.

En PowerShell, configura la dirección del servidor **solo para esta sesión**:

```powershell
$env:OLLAMA_API_BASE = 'http://127.0.0.1:11434'
```

Desde la raíz de FacturaciónES, inicia Aider con el proveedor de chat de Ollama:

```powershell
aider --model ollama_chat/qwen2.5-coder:7b --no-auto-commits
```

`--no-auto-commits` evita que Aider cree commits automáticamente; **no impide que edite archivos** cuando se lo pidas. Verifica el comportamiento de tu versión de Aider antes de usarlo en código importante.

Si aparece un error de conexión, comprueba de nuevo `http://127.0.0.1:11434/api/tags`. Si aparece un error de modelo, verifica el nombre exacto con `ollama list` y consulta la documentación de Aider para el prefijo de proveedor compatible con tu versión.

## 7. Hacer que Aider lea las reglas de FacturaciónES

Aider no debe asumir que ha leído automáticamente `AGENTS.md`. Al iniciar una sesión, puedes cargarlo como contexto **de solo lectura** con el comando de Aider:

```text
/read-only AGENTS.md
```

Después envía esta instrucción inicial:

> Lee AGENTS.md y analiza el repositorio FacturaciónES. Comprueba la rama actual y el estado de Git, revisa la solución Facturacion.slnx y explica arquitectura, seguridad multi-tenant, módulos existentes, riesgos y pendientes. **No modifiques archivos, no ejecutes comandos destructivos y no hagas commits ni push hasta que te lo autorice.** Responde en español.

**Importante:** Aider es un agente de edición de código. Para un análisis inicial sin cambios, utiliza sus capacidades de consulta y verifica siempre `git diff` y `git status` al terminar.

## 8. Primera sesión recomendada

1. Abre Ollama y comprueba `ollama list`.
2. Abre PowerShell en `D:\proyectos\Facturacion_ESPN`.
3. Comprueba `git branch --show-current` y `git status --short`.
4. Ejecuta `aider --model ollama_chat/qwen2.5-coder:7b --no-auto-commits`.
5. Carga `AGENTS.md` con `/read-only AGENTS.md`.
6. Pide **solo análisis**, no cambios.
7. Revisa las conclusiones antes de autorizar tareas.

## 9. Flujo seguro para implementar cambios

Antes de cada tarea:

```powershell
git branch --show-current
git status --short
```

Explica a Aider qué funcionalidad debe cambiar y qué archivos puede tocar. Pídele que respete `AGENTS.md`, que no modifique `main` y que no altere migraciones o autenticación sin revisar riesgos.

Después de cada tarea, desde otra terminal:

```powershell
git diff --check
git diff
dotnet restore Facturacion.slnx
dotnet build Facturacion.slnx
dotnet test Facturacion.slnx
```

No des por válida una modificación si falla la compilación o las pruebas. Los tests existentes pueden no cubrir completamente el aislamiento entre tenants y empresas.

## 10. Errores habituales

| Problema | Qué comprobar |
| --- | --- |
| `ollama` no se reconoce | Reabrir terminal, instalación y PATH |
| No conecta con `localhost:11434` | Ollama abierto, servidor activo y puerto |
| `model not found` | `ollama list`, nombre exacto del modelo y descarga completada |
| `aider` no se reconoce | Instalación de Python/Aider, PATH y terminal nueva |
| Respuestas muy lentas | RAM disponible, GPU, tamaño del modelo y otras aplicaciones abiertas |
| Aider no sigue `AGENTS.md` | Cargarlo explícitamente con `/read-only AGENTS.md` y repetir las reglas relevantes |
| Aparecen cambios inesperados | Revisar `git status` y `git diff`; no sobrescribir ni descartar sin entenderlos |
| Fallan `dotnet build` o `dotnet test` | Leer errores concretos, revisar SDK, paquetes y configuración; no afirmar éxito |

## 11. Privacidad y seguridad

- Ollama ejecuta el modelo localmente, pero revisa las opciones de cualquier herramienta que añadas para evitar envíos a servicios externos.
- No pegues claves de Stripe, contraseñas, tokens, certificados ni datos fiscales reales en prompts.
- Evita conceder a agentes permisos automáticos para ejecutar comandos, borrar archivos o hacer push.
- Mantén copias de seguridad antes de migraciones de base de datos.
- Trabaja **solo en `develop`** y comprueba el estado de Git frecuentemente.

## 12. Enlaces oficiales

- Ollama: https://ollama.com/
- Instalación de Ollama: https://ollama.com/download
- Biblioteca de modelos: https://ollama.com/library
- Aider: https://aider.chat/
- Instalación de Aider: https://aider.chat/docs/install.html
- Integración de Aider con Ollama: https://aider.chat/docs/llms/ollama.html

---

**Resultado esperado:** Ollama funcionando en tu equipo, un modelo de programación descargado, Aider conectado al modelo local y el contexto de FacturaciónES cargado desde `AGENTS.md`, sin cambios automáticos en `main`.
