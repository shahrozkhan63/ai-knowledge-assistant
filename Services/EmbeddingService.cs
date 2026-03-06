using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AIKnowledgeAssistant.Services;

public class EmbeddingService
{
    private readonly HttpClient _client;
    private readonly string _apiKey;
    private const string Model = "text-embedding-3-small";

    public EmbeddingService(IConfiguration config)
    {
        // Try environment variable directly first
        _apiKey = Environment.GetEnvironmentVariable("OpenAI__ApiKey")
               ?? config["OpenAI:ApiKey"]
               ?? config["OpenAI__ApiKey"]!;

        Console.WriteLine($"[OpenAI] Key loaded: {!string.IsNullOrEmpty(_apiKey)}");
        _client = new HttpClient();
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var payload = new
        {
            input = text,
            model = Model,
            encoding_format = "float"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();
    }
}