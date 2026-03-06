using Microsoft.AspNetCore.Mvc;
using AIKnowledgeAssistant.Models;
using AIKnowledgeAssistant.Orchestration;
using AIKnowledgeAssistant.Services;

namespace AIKnowledgeAssistant.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KnowledgeController : ControllerBase
{
    private readonly RAGPipeline _pipeline;
    private readonly EmbeddingService _embeddingService;
    private readonly VectorSearchService _vectorSearch;

    public KnowledgeController(
        RAGPipeline pipeline,
        EmbeddingService embeddingService,
        VectorSearchService vectorSearch)
    {
        _pipeline = pipeline;
        _embeddingService = embeddingService;
        _vectorSearch = vectorSearch;
    }

    // ── POST /api/knowledge/ask ───────────────────────────────
    // Main endpoint — runs the full orchestration pipeline
    // UI sends: { question: "...", provider: "openai" }
    // Returns: full AiResponse with answer, decision, docs, etc.
    [HttpPost("ask")]
    public async Task<ActionResult<AiResponse>> Ask(
        [FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question cannot be empty.");

        var result = await _pipeline.RunAsync(request);
        return Ok(result);
    }

    // ── POST /api/knowledge/documents ────────────────────────
    // Insert a new document into the knowledge base
    // UI sends: { title: "...", content: "..." }
    // 1. Generates embedding from content
    // 2. Saves to PostgreSQL with vector
    [HttpPost("documents")]
    public async Task<IActionResult> InsertDocument(
        [FromBody] InsertDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Content cannot be empty.");

        var embedding = await _embeddingService
            .GenerateEmbeddingAsync(request.Content);

        await _vectorSearch.InsertDocumentAsync(
            request.Title,
            request.Content,
            embedding);

        return Ok(new
        {
            message = "Document inserted successfully.",
            title = request.Title
        });
    }

    // ── GET /api/knowledge/documents ─────────────────────────
    // Returns all documents for the UI sidebar
    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments()
    {
        var docs = await _vectorSearch.GetAllDocumentsAsync();
        return Ok(docs);
    }
    // Temporary debug endpoint — remove after fixing
    [HttpGet("debug")]
    public IActionResult Debug()
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        var pgHost = Environment.GetEnvironmentVariable("PGHOST");
        var connString = _vectorSearch.GetConnectionStatus();

        return Ok(new
        {
            DATABASE_URL_exists = !string.IsNullOrEmpty(databaseUrl),
            PGHOST_exists = !string.IsNullOrEmpty(pgHost),
            DATABASE_URL_value = databaseUrl?[..Math.Min(30, databaseUrl?.Length ?? 0)] + "...",
            connString_empty = string.IsNullOrEmpty(connString)
        });
    }
}