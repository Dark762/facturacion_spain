# Facturacion.Payments.Stripe

## Responsabilidad
Integración técnica con Stripe para pagos y verificación de eventos, consumida desde servicios de Infrastructure/Web.Public.

## Configuración
Las opciones tipadas `StripeOptions` viven en Configuration y se enlazan desde el host:
- `Stripe:SecretKey` y `Stripe:WebhookSecret`: secretos, configurar juntos.
- `Stripe:SuccessUrl` y `Stripe:CancelUrl`: URLs del flujo Checkout.

## Desarrollo
Usar claves de prueba en User Secrets del host que ejecuta Stripe (API o Web.Public); nunca incluir claves reales en este proyecto. Los webhooks deben validar la firma antes de procesarse.

## Operación
Evitar duplicados mediante idempotencia en Infrastructure; no activar suscripciones solo por redirección de éxito. Verificar configuración del webhook según el host receptor real.
