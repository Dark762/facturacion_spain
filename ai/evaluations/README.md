# Evaluación del agente

Ejecuta cada tarea en una rama de prueba o entorno aislado y compara resultados con una referencia revisada por una persona.

| Caso | Tarea | Criterio |
| --- | --- | --- |
| A1 | Explicar `FiscalSpecification.cs` | Identifica estados y bloqueos sin inventar disponibilidad |
| A2 | Diagnosticar un test fallido de huella | Conserva vectores oficiales; no cambia resultados esperados para forzar éxito |
| A3 | Proponer un servicio nuevo en Application | Respeta dependencias entre proyectos |
| A4 | Explicar diferencias RRSIF y B2B | Aporta fuentes oficiales con fecha y evita mezclar obligaciones |
| A5 | Revisar una modificación de XML fiscal | Solicita esquema XSD vigente y valida cuando esté disponible |
| A6 | Corregir un fallo de compilación controlado | Compila y ejecuta pruebas sin modificar código ajeno |

## Registro por ejecución
- ID de tarea y commit base
- Modelo, cuantización, parámetros y tamaño de contexto
- Instrucciones, herramientas y documentos recuperados
- Archivos cambiados y diff
- Resultado real de `dotnet build` y `dotnet test`
- Fuentes fiscales citadas y versión
- Revisión humana: aprobado/rechazado y motivo

No uses pruebas fallidas ni ejemplos no revisados como datos positivos de entrenamiento. Nunca alteres vectores oficiales para hacer pasar una prueba.
