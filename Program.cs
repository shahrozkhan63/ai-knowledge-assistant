using Microsoft.EntityFrameworkCore;
using AIKnowledgeAssistant.Agents;
using AIKnowledgeAssistant.Data;
using AIKnowledgeAssistant.Orchestration;
using AIKnowledgeAssistant.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ── AI Providers ──────────────────────────────────────────────
// Registered as Singleton = created once, reused every request
builder.Services.AddSingleton<OpenAIProvider>();
builder.Services.AddSingleton<ClaudeProvider>();
builder.Services.AddSingleton<AIProviderFactory>();

// ── Core Services ─────────────────────────────────────────────
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<VectorSearchService>();

// ── Agent ─────────────────────────────────────────────────────
// Agent always uses OpenAI to make routing decisions
builder.Services.AddSingleton<AgentDecisionLayer>(sp =>
{
    var factory = sp.GetRequiredService<AIProviderFactory>();
    var openAiProvider = factory.GetProvider("openai");
    return new AgentDecisionLayer(openAiProvider);
});

// ── Pipeline ──────────────────────────────────────────────────
// Scoped = new instance created per HTTP request
builder.Services.AddScoped<RAGPipeline>();

// ── API + Swagger ─────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "AI Knowledge Assistant API",
        Version = "v1",
        Description = "RAG Pipeline + AI Agent Orchestration"
    });
});

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Knowledge Assistant v1");
    c.RoutePrefix = "swagger"; // swagger at /swagger
});

app.UseDefaultFiles();  // serves wwwroot/index.html at /
app.UseStaticFiles();   // serves all wwwroot files
app.UseAuthorization();
app.MapControllers();

app.Run();