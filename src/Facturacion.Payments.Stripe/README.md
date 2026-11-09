# Facturacion.Payments.Stripe

Adaptador técnico de **Stripe** (.NET 8), utilizado por el flujo de registro y suscripción del SaaS. **Stripe gestiona la suscripción al producto; no la emisión de facturas comerciales de los clientes.**

## Componentes

- `StripeCheckoutGateway`: crea sesiones de Checkout para suscripciones mediante la API de Stripe.
- `StripeWebhookVerifier`: verificación de firmas del webhook.
- `StripeOptions`: parámetros del proveedor.

`Facturacion.Infrastructure/Billing` gestiona la coordinación del registro y los eventos; `Facturacion.Api` ofrece el controlador del webhook. No asumir que un Checkout creado equivale a suscripción activa ni a pago confirmado: verificar el ciclo de eventos y las transacciones.

## Configuración y seguridad

Los valores de `Stripe:SecretKey`, `Stripe:WebhookSecret`, `Stripe:SuccessUrl` y `Stripe:CancelUrl` se encuentran en la configuración de la API; los secretos deben inyectarse desde un almacén seguro o variables de entorno. No almacenar datos sensibles en logs y validar la firma antes de confiar en un evento.

```bash
dotnet build src/Facturacion.Payments.Stripe/Facturacion.Payments.Stripe.csproj
```

Ver [README principal](../../README.md) y [Api](../Facturacion.Api/README.md).
