using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Facturacion.Client;

/// <summary>Resultado HTTP tipado, sin dependencias de Blazor ni del sistema operativo.</summary>
public sealed record ApiResult<T>(T? Value, HttpStatusCode StatusCode, ApiError? Error)
{
    public bool IsSuccess => Error is null;
}

public sealed record ApiError(string Message, string? Code = null, IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ApiClient(HttpClient http) => _http = http;

    public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, ct);

    public Task<ApiResult<T>> PostAsync<T>(string path, object body, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Post, path, body, ct);

    public Task<ApiResult<T>> PutAsync<T>(string path, object body, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Put, path, body, ct);

    public Task<ApiResult<T>> DeleteAsync<T>(string path, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Delete, path, null, ct);

    public async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path) || Uri.TryCreate(path, UriKind.Absolute, out _) || path.StartsWith("//", StringComparison.Ordinal))
            throw new ArgumentException("La ruta debe ser relativa a la API configurada.", nameof(path));

        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
                return new ApiResult<T>(default, response.StatusCode, null);
            // Compatibilidad con endpoints antiguos (JSON directo) y nuevos (ApiResponse<T>).
            // No se exige migrar todos los controladores en el mismo despliegue.
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("success", out var success) &&
                (success.ValueKind == JsonValueKind.True || success.ValueKind == JsonValueKind.False) &&
                root.TryGetProperty("data", out var data) &&
                root.TryGetProperty("errors", out _))
            {
                if (success.ValueKind == JsonValueKind.False)
                {
                    var message = root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String
                        ? msg.GetString() ?? "No se pudo completar la operación."
                        : "No se pudo completar la operación.";
                    var code = root.TryGetProperty("code", out var cod) && cod.ValueKind == JsonValueKind.String
                        ? cod.GetString() : null;
                    return new ApiResult<T>(default, response.StatusCode, new ApiError(message, code));
                }
                return new ApiResult<T>(data.ValueKind == JsonValueKind.Null
                    ? default : data.Deserialize<T>(JsonOptions), response.StatusCode, null);
            }
            return new ApiResult<T>(root.Deserialize<T>(JsonOptions), response.StatusCode, null);
        }

        var message = $"La API devolvió el estado {(int)response.StatusCode}.";
        string? code = null;
        IReadOnlyDictionary<string, string[]>? validation = null;
        try
        {
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                    message = detail.GetString() ?? message;
                else if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    message = title.GetString() ?? message;
                if (root.TryGetProperty("code", out var errorCode) && errorCode.ValueKind == JsonValueKind.String)
                    code = errorCode.GetString();
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    validation = JsonSerializer.Deserialize<Dictionary<string, string[]>>(errors.GetRawText(), JsonOptions);
            }
        }
        catch (JsonException) { /* El servidor puede devolver HTML o texto plano. */ }

        return new ApiResult<T>(default, response.StatusCode, new ApiError(message, code, validation));
    }
}
