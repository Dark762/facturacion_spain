# Facturacion.Client

## Responsabilidad
Cliente HTTP tipado reutilizable por Web.App y otros frontends. `ApiClient` interpreta respuestas `ApiResponse<T>` y respuestas JSON antiguas; los clientes específicos encapsulan rutas de autenticación, clientes, productos y facturas.

## Configuración
El host consumidor registra `AddFacturacionApiClient(new Uri(apiBaseUrl))` y proporciona una URL HTTP(S) absoluta. En Web.App la clave es `Facturacion:ApiBaseUrl` en `Hosts/Web.App/appsettings*.json`.

## Seguridad
No guardar contraseñas, JWT ni secretos Stripe en esta biblioteca. La autenticación y el almacenamiento seguro del token son responsabilidad del host. No enviar tokens a URLs externas; las rutas de `ApiClient` deben ser relativas.

## Integración
`AuthenticationApiClient.LoginAsync(email,password,tenantId)` usa el login de la API; `ApiResult<T>` proporciona estado HTTP y error. Revisar compatibilidad de contratos al cambiar DTO o `ApiResponse<T>`.
