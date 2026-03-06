using System.Diagnostics;
using AIKnowledgeAssistant.Agents;
using AIKnowledgeAssistant.Models;
using AIKnowledgeAssistant.Prompts;
using AIKnowledgeAssistant.Services;

namespace AIKnowledgeAssistant.Orchestration;

// ─────────────────────────────────────────────────────────────
// THIS IS THE CONDUCTOR — It coordinates everything.
//
// Full pipeline flow:
//
// 1. Agent decides strategy        (AgentDecisionLayer)
// 2a. If DATABASE_SEARCH:
//     → Generate embedding         (EmbeddingService)
//     → Search PostgreSQL          (VectorSearchService)
//     → Build RAG prompt           (PromptTemplates)
//     → Call LLM                   (IAIProvider)
// 2b. If DIRECT_ANSWER:
//     → Build direct prompt        (PromptTemplates)
//     → Call LLM                   (IAIProvider)
// 2c. If EXTERNAL_API:
//     → Build polite decline       (PromptTemplates)
//     → Call LLM                   (IAIProvider)
// 3. Return unified AiResponse
//
// This is AI Orchestration — this is what AI Engineers build.
// ─────────────────────────────────────────────────────────────

public class RAGPipeline
{
    private readonly AgentDecisionLayer _agent;
    private readonly EmbeddingService _embeddingService;
    private readonly VectorSearchService _vectorSearch;
    private readonly AIProviderFactory _providerFactory;
    private readonly ILogger<RAGPipeline> _logger;

    public RAGPipeline(
        AgentDecisionLayer agent,
        EmbeddingService embeddingService,
        VectorSearchService vectorSearch,
        AIProviderFactory providerFactory,
        ILogger<RAGPipeline> logger)
    {
        _agent = agent;
        _embeddingService = embeddingService;
        _vectorSearch = vectorSearch;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<AiResponse> RunAsync(AskRequest request)
    {
        // Start timer — we show response time in the UI
        var sw = Stopwatch.StartNew();

        // Get the correct AI provider (OpenAI or Claude)
        var provider = _providerFactory.GetProvider(request.Provider);

        _logger.LogInformation(
            "Pipeline started | Provider: {Provider} | Question: {Question}",
            provider.Name, request.Question);

        // ── STEP 1: Agent decides strategy ───────────────────────
        var (decision, reason) = await _agent.DecideAsync(request.Question);

        _logger.LogInformation(
            "Agent decision: {Decision} | Reason: {Reason}",
            decision, reason);

        // ── STEP 2: Execute the chosen strategy ──────────────────
        string answer;
        var retrievedDocs = new List<RetrievedDocument>();
        float confidence;

        switch (decision)
        {
            // ── PATH A: Database RAG Search ───────────────────────
            case AgentDecision.DatabaseSearch:

                var embedding = await _embeddingService
         .GenerateEmbeddingAsync(request.Question);

                retrievedDocs = await _vectorSearch
                    .SearchAsync(embedding, topK: 5);

                if (retrievedDocs.Count == 0)
                {
                    answer = "I couldn't find relevant information in the " +
                                 "knowledge base for your question.";
                    confidence = 0.1f;
                    break;
                }

                var context = string.Join("\n---\n",
                    retrievedDocs.Select(d => d.Content));

                var ragPrompt = PromptTemplates
                    .BuildRagPrompt(context, request.Question);

                answer = await provider.CompleteAsync(ragPrompt);

                // ── Real confidence = top document similarity score ──
                // If best document is 76% match → confidence is 76%
                var topScore = retrievedDocs.Max(d => d.Score);
                confidence = (float)topScore;
                break;

                    /*```

                    ---

                    ## Now Both Scores Are Honest
                    ```
                    Ask: "What is API rate limiting?"

                    Document match:   76% ← cosine similarity (real)
                    Confidence score: 76% ← same value, now also real
                    ```

                    And if a perfect document exists:
                    ```
                    Document match:   95%
                    Confidence score: 95%
                                */

            // ── PATH B: External API ──────────────────────────────
            case AgentDecision.ExternalApi:

                var extPrompt = PromptTemplates
                    .BuildExternalApiPrompt(request.Question);

                answer = await provider.CompleteAsync(extPrompt);
                confidence = 0.5f;
                break;

            // ── PATH C: Direct Answer ─────────────────────────────
            default:

                var directPrompt = PromptTemplates
                    .BuildDirectPrompt(request.Question);

                answer = await provider.CompleteAsync(directPrompt);
                confidence = 0.88f;
                break;
        }

        sw.Stop();

        _logger.LogInformation(
            "Pipeline completed in {Ms}ms",
            sw.ElapsedMilliseconds);

        // ── STEP 3: Return unified response ──────────────────────
        return new AiResponse
        {
            Answer = answer,
            Decision = decision,
            DecisionReason = reason,
            RetrievedDocuments = retrievedDocs,
            Provider = provider.Name,
            ConfidenceScore = confidence,
            ResponseTimeMs = sw.ElapsedMilliseconds
        };
    }

}

/*
```

---

### How all files connect inside this pipeline:
```
RAGPipeline.RunAsync(request)
         │
         ├─► AgentDecisionLayer.DecideAsync()
         │        └─► IAIProvider.CompleteAsync()  ← asks AI to route
         │
         ├─► [If DatabaseSearch]
         │        ├─► EmbeddingService.GenerateEmbeddingAsync()
         │        ├─► VectorSearchService.SearchAsync()
         │        └─► PromptTemplates.BuildRagPrompt()
         │
         ├─► [If DirectAnswer]
         │        └─► PromptTemplates.BuildDirectPrompt()
         │
         ├─► [If ExternalApi]
         │        └─► PromptTemplates.BuildExternalApiPrompt()
         │
         └─► IAIProvider.CompleteAsync()  ← final answer
                  ├─► OpenAIProvider(if user selected OpenAI)
                  └─► ClaudeProvider(if user selected Claude)

*/