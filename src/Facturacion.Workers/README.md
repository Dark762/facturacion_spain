# Facturacion.Workers

Host .NET 8 para futuros procesos asíncronos de **Facturación Spain**.

## Estado actual

`Program.cs` registra un único `BackgroundService` (`Worker`). El servicio escribe un mensaje de actividad aproximadamente cada segundo y **no** consume la tabla Outbox ni realiza llamadas AEAT, B2B o Stripe.

Esta distinción importa: en Application ya existen operaciones que pueden generar mensajes `fiscal.record.created`, `AEAT_SUBMIT_FISCAL_RECORD` o `B2B_SEND_DOCUMENT`, pero no hay procesamiento efectivo por este Worker.

## Objetivos técnicos pendientes

Al incorporar consumidores, implementar reclamación atómica de mensajes, aislamiento por tenant, idempotencia, reintentos con backoff, seguimiento de estados, trazabilidad segura y gestión de fallos. No marcar un envío como aceptado por AEAT o B2B sin respuesta verificable del destinatario.

```bash
dotnet run --project src/Facturacion.Workers/Facturacion.Workers.csproj
```

Iniciar este host **no** habilita la emisión fiscal. Ver [README principal](../../README.md) y [motor VERI*FACTU](../Facturacion.Verifactu/README.md).
