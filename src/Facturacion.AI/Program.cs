using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Facturacion.AI;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string Root = FindRoot();
    private static readonly string Database = Environment.GetEnvironmentVariable("FACTURACION_RAG_DB")
        ?? Path.Combine(Root, "ai", ".local", "rag-dotnet.sqlite3");
    private static readonly string Model = Environment.GetEnvironmentVariable("FACTURACION_EMBED_MODEL") ?? "nomic-embed-text";
    private static readonly HttpClient Http = new() { BaseAddress = new Uri((Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? "http://localhost:11434").TrimEnd('/') + "/"), Timeout = TimeSpan.FromMinutes(3) };
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".cs", ".csproj", ".slnx", ".md", ".txt", ".xml", ".xsd", ".json" };
    private static readonly HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase)
        { ".git", ".vs", "bin", "obj", ".local", "TestResults", "node_modules", "artifacts" };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            switch (args.FirstOrDefault()?.ToLowerInvariant())
            {
                case "index":
                    await IndexAsync();
                    return 0;
                case "search" when args.Length >= 2:
                    Console.WriteLine(JsonSerializer.Serialize(await SearchAsync(string.Join(' ', args.Skip(1))), JsonOptions));
                    return 0;
                case "serve":
                    await ServeAsync();
                    return 0;
                default:
                    Console.Error.WriteLine("Uso: dotnet run --project src/Facturacion.AI -- index | search <pregunta> | serve");
                    return 2;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Facturacion.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Ejecuta desde el repositorio que contiene Facturacion.slnx");
    }

    private static SqliteConnection OpenDatabase()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Database))!);
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database }.ToString());
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS chunks (
                path TEXT NOT NULL,
                ordinal INTEGER NOT NULL,
                sha256 TEXT NOT NULL,
                model TEXT NOT NULL,
                body TEXT NOT NULL,
                embedding TEXT NOT NULL,
                PRIMARY KEY (path, ordinal)
            );
            CREATE INDEX IF NOT EXISTS ix_chunks_model ON chunks(model);
            """;
        cmd.ExecuteNonQuery();
        return connection;
    }

    private static IEnumerable<string> FilesToIndex()
    {
        foreach (var folder in new[] { "docs", "src", "test", Path.Combine("ai", "knowledge") })
        {
            var basePath = Path.Combine(Root, folder);
            if (!Directory.Exists(basePath)) continue;
            foreach (var path in Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(Root, path);
                if (relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(Excluded.Contains))
                    continue;
                var name = Path.GetFileName(path);
                if (!Extensions.Contains(Path.GetExtension(path)) ||
                    name.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase) ||
                    new[] { "secret", "credential", "private", "certificate", ".env", "customer-data" }
                        .Any(part => name.Contains(part, StringComparison.OrdinalIgnoreCase)))
                    continue;
                var info = new FileInfo(path);
                if (info.Length > 1_000_000 || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    continue;
                yield return path;
            }
        }
    }

    private static IEnumerable<string> Split(string text)
    {
        text = text.Trim();
        for (var start = 0; start < text.Length; start += 1360)
        {
            yield return text.Substring(start, Math.Min(1600, text.Length - start));
            if (start + 1600 >= text.Length) break;
        }
    }

    private static async Task<float[][]> EmbedAsync(string[] texts)
    {
        using var response = await Http.PostAsJsonAsync("api/embed", new { model = Model, input = texts });
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty("embeddings").EnumerateArray()
            .Select(vector => vector.EnumerateArray().Select(value => value.GetSingle()).ToArray())
            .ToArray();
    }

    private static async Task IndexAsync()
    {
        using var db = OpenDatabase();
        var paths = FilesToIndex().ToArray();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updated = 0;
        foreach (var file in paths)
        {
            var path = Path.GetRelativePath(Root, file).Replace('\\', '/');
            seen.Add(path);
            var bytes = await File.ReadAllBytesAsync(file);
            if (bytes.Contains((byte)0)) continue;
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            using var check = db.CreateCommand();
            check.CommandText = "SELECT sha256, model FROM chunks WHERE path=$path LIMIT 1";
            check.Parameters.AddWithValue("$path", path);
            using var reader = check.ExecuteReader();
            var unchanged = reader.Read() && reader.GetString(0) == hash && reader.GetString(1) == Model;
            reader.Close();
            if (unchanged) continue;

            var parts = Split(Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF')).ToArray();
            var vectors = new List<float[]>();
            for (var i = 0; i < parts.Length; i += 8)
                vectors.AddRange(await EmbedAsync(parts.Skip(i).Take(8).ToArray()));
            if (vectors.Count != parts.Length)
                throw new InvalidOperationException("Ollama devolvió un número inesperado de vectores");

            using var transaction = db.BeginTransaction();
            using (var delete = db.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM chunks WHERE path=$path";
                delete.Parameters.AddWithValue("$path", path);
                delete.ExecuteNonQuery();
            }
            for (var i = 0; i < parts.Length; i++)
            {
                using var insert = db.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO chunks VALUES ($path,$ordinal,$sha,$model,$body,$embedding)";
                insert.Parameters.AddWithValue("$path", path);
                insert.Parameters.AddWithValue("$ordinal", i);
                insert.Parameters.AddWithValue("$sha", hash);
                insert.Parameters.AddWithValue("$model", Model);
                insert.Parameters.AddWithValue("$body", parts[i]);
                insert.Parameters.AddWithValue("$embedding", JsonSerializer.Serialize(vectors[i]));
                insert.ExecuteNonQuery();
            }
            transaction.Commit();
            updated++;
            Console.Error.WriteLine($"Indexado: {path} ({parts.Length} fragmentos)");
        }
        using var stale = db.CreateCommand();
        stale.CommandText = "SELECT DISTINCT path FROM chunks";
        var indexed = new List<string>();
        using (var reader = stale.ExecuteReader())
            while (reader.Read()) indexed.Add(reader.GetString(0));
        foreach (var path in indexed.Where(path => !seen.Contains(path)))
        {
            using var delete = db.CreateCommand();
            delete.CommandText = "DELETE FROM chunks WHERE path=$path";
            delete.Parameters.AddWithValue("$path", path);
            delete.ExecuteNonQuery();
        }
        Console.Error.WriteLine($"Finalizado: {updated} archivos actualizados.");
    }

    private static float Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length) return -1;
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return normA == 0 || normB == 0 ? 0 : (float)(dot / Math.Sqrt(normA * normB));
    }

    private static async Task<object[]> SearchAsync(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var vector = (await EmbedAsync([query]))[0];
        using var db = OpenDatabase();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT path, ordinal, body, embedding FROM chunks WHERE model=$model";
        cmd.Parameters.AddWithValue("$model", Model);
        using var reader = cmd.ExecuteReader();
        var results = new List<(string Source, int Chunk, string Text, float Score)>();
        while (reader.Read())
        {
            var stored = JsonSerializer.Deserialize<float[]>(reader.GetString(3)) ?? [];
            results.Add((reader.GetString(0), reader.GetInt32(1), reader.GetString(2), Cosine(vector, stored)));
        }
        return results.OrderByDescending(item => item.Score).Take(Math.Clamp(limit, 1, 10))
            .Select(item => (object)new {
                source = item.Source, chunk = item.Chunk, score = Math.Round(item.Score, 4),
                text = item.Text, verification = "No acredita vigencia legal; verificar fuentes oficiales"
            }).ToArray();
    }

    private static async Task ServeAsync()
    {
        // MCP stdio: un mensaje JSON-RPC por línea; stdout exclusivamente para respuestas.
        string? line;
        while ((line = await Console.In.ReadLineAsync()) is not null)
        {
            try
            {
                using var message = JsonDocument.Parse(line);
                var root = message.RootElement;
                if (!root.TryGetProperty("id", out var id)) continue;
                var method = root.GetProperty("method").GetString();
                object result;
                switch (method)
                {
                    case "initialize":
                        result = new { protocolVersion = "2024-11-05",
                            capabilities = new { tools = new { } },
                            serverInfo = new { name = "facturacion-rag-dotnet", version = "0.1.0" } };
                        break;
                    case "ping":
                        result = new { };
                        break;
                    case "tools/list":
                        result = new { tools = new[] { new {
                            name = "search_fiscal_knowledge",
                            description = "Busca fragmentos locales de código y documentos; no verifica vigencia legal",
                            inputSchema = new {
                                type = "object",
                                properties = new {
                                    query = new { type = "string" },
                                    limit = new { type = "integer", minimum = 1, maximum = 10 }
                                },
                                required = new[] { "query" }
                            }
                        } } };
                        break;
                    case "tools/call":
                        var parameters = root.GetProperty("params");
                        if (parameters.GetProperty("name").GetString() != "search_fiscal_knowledge")
                            throw new ArgumentException("Herramienta desconocida");
                        var arguments = parameters.GetProperty("arguments");
                        var query = arguments.GetProperty("query").GetString() ?? "";
                        var limit = arguments.TryGetProperty("limit", out var limitProperty) ? limitProperty.GetInt32() : 5;
                        result = new { content = new[] { new { type = "text",
                            text = JsonSerializer.Serialize(await SearchAsync(query, limit), JsonOptions) } } };
                        break;
                    default:
                        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(new {
                            jsonrpc = "2.0", id, error = new { code = -32601, message = "Método no soportado" }
                        }));
                        await Console.Out.FlushAsync();
                        continue;
                }
                await Console.Out.WriteLineAsync(JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result }, JsonOptions));
                await Console.Out.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error MCP: {ex.Message}");
            }
        }
    }
}
