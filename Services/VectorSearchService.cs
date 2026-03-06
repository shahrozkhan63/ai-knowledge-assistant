using Npgsql;
using NpgsqlTypes;

namespace AIKnowledgeAssistant.Services;

public class VectorSearchService
{
    private readonly string _connString;

    public string GetConnectionStatus() => _connString;
    public VectorSearchService(IConfiguration config)
    {
        // Try DATABASE_URL first (Railway always provides this)
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrEmpty(databaseUrl))
        {
            // Convert PostgreSQL URL to Npgsql connection string
            // Format: postgresql://user:password@host:port/database
            var uri = new Uri(databaseUrl);
            var host = uri.Host;
            var port = uri.Port;
            var database = uri.AbsolutePath.TrimStart('/');
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo[0];
            var password = userInfo[1];

            _connString = $"Host={host};Port={port};Username={username};Password={password};Database={database};SSL Mode=Require;Trust Server Certificate=true";
            Console.WriteLine($"[DB] Railway DATABASE_URL connected: {host}:{port}/{database}");
        }
        else
        {
            // Local development fallback
            _connString = config.GetConnectionString("DefaultConnection")!;
            Console.WriteLine("[DB] Using local connection string");
        }
    }

    // ── Search: Find top K most similar documents ──────────────
    // Using your existing cosine_similarity function
    public async Task<List<RetrievedDocument>> SearchAsync(float[] queryVector, int topK = 5)
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        var sql = @"
        SELECT content, ""Title"", cosine_similarity(embedding, @q) AS score
        FROM documents
        ORDER BY score DESC
        LIMIT @topK;
    ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("q",
            NpgsqlDbType.Array | NpgsqlDbType.Double,
            queryVector.Select(f => (double)f).ToArray());
        cmd.Parameters.AddWithValue("topK", topK);

        var results = new List<RetrievedDocument>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var content = reader.GetString(0);
            var title = reader.GetString(1);
            var score = reader.IsDBNull(2) ? 0.0 : reader.GetDouble(2);

            Console.WriteLine($"[SCORE] {score:F4} | {title}");

            if (score >= 0.3)
                results.Add(new RetrievedDocument
                {
                    Title = title,
                    Content = content,
                    Score = score
                });
        }

        return results;
    }

    
    // ── Insert: Save document + embedding to PostgreSQL ────────
    public async Task InsertDocumentAsync(string title, string content, float[] embedding)
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        var sql = @"
            INSERT INTO documents (""Title"", content, embedding, ""CreatedAt"")
            VALUES (@title, @content, @embedding, @createdAt);
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("title", title);
        cmd.Parameters.AddWithValue("content", content);

        // Store as double[] exactly like your old project
        cmd.Parameters.AddWithValue("embedding",
            NpgsqlDbType.Array | NpgsqlDbType.Double,
            embedding.Select(f => (double)f).ToArray());

        cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
    }

    // ── List: Get all documents for the UI sidebar ──────────────
    public async Task<List<DocumentSummary>> GetAllDocumentsAsync()
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        var sql = @"
            SELECT id, ""Title"", content, ""CreatedAt""
            FROM documents
            ORDER BY ""CreatedAt"" DESC;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);

        var results = new List<DocumentSummary>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new DocumentSummary
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Content = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            });
        }

        return results;
    }
    // Add this new method
    public async Task<double> GetTopScoreAsync(float[] queryVector)
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        var sql = @"
        SELECT cosine_similarity(embedding, @q) AS score
        FROM documents
        ORDER BY score DESC
        LIMIT 1;
    ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("q",
            NpgsqlDbType.Array | NpgsqlDbType.Double,
            queryVector.Select(f => (double)f).ToArray());

        var result = await cmd.ExecuteScalarAsync();
        return result == DBNull.Value ? 0.0 : Convert.ToDouble(result);
    }
}

public class DocumentSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RetrievedDocument
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
}