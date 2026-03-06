using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AIKnowledgeAssistant.Services;

// ─── Interface ────────────────────────────────────────────────
// Every AI provider must implement this
// This is the IAIProvider abstraction = real AI engineering
public interface IAIProvider
{
    string Name { get; }
    Task<string> CompleteAsync(string prompt);
}

// ─── OpenAI Provider ──────────────────────────────────────────
// This is your old OpenAiService.cs + ChatService.cs combined
public class OpenAIProvider : IAIProvider
{
    private readonly HttpClient _client;
    private readonly string _apiKey;
    public string Name => "OpenAI GPT-4o-mini";

    public OpenAIProvider(IConfiguration config)
    {
        _apiKey = config["OpenAI:ApiKey"]!;
        _client = new HttpClient();
    }

    public async Task<string> CompleteAsync(string prompt)
    {
        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()!;
    }
}

// ─── Claude Provider ──────────────────────────────────────────
// Brand new — this is your second AI provider
public class ClaudeProvider : IAIProvider
{
    private readonly HttpClient _client;
    private readonly string _apiKey;
    public string Name => "Anthropic Claude";

    public ClaudeProvider(IConfiguration config)
    {
        _apiKey = config["Anthropic:ApiKey"]!;
        _client = new HttpClient();
    }

    public async Task<string> CompleteAsync(string prompt)
    {
        var payload = new
        {
            model = "claude-haiku-4-5-20251001",
            max_tokens = 1024,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString()!;
    }
}

// ─── Provider Factory ─────────────────────────────────────────
// Decides which provider to use based on user selection
// User picks "openai" or "claude" in the UI
public class AIProviderFactory
{
    private readonly OpenAIProvider _openAI;
    private readonly ClaudeProvider _claude;

    public AIProviderFactory(OpenAIProvider openAI, ClaudeProvider claude)
    {
        _openAI = openAI;
        _claude = claude;
    }

    public IAIProvider GetProvider(string name) => name.ToLower() switch
    {
        "claude" => _claude,
        _ => _openAI  // default to OpenAI
    };
}