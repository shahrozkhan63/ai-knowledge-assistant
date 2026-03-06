using AIKnowledgeAssistant.Models;
using AIKnowledgeAssistant.Orchestration;
using AIKnowledgeAssistant.Services;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace AIKnowledgeAssistant.Agents;

// ─────────────────────────────────────────────────────────────
// THIS IS THE AI AGENT — The most important class in the project
// 
// Instead of always searching the database blindly,
// the Agent THINKS first and decides the best strategy:
//
//   Question: "What is microservices?"
//   Agent → DATABASE_SEARCH (it's in our knowledge base)
//
//   Question: "What is 2 + 2?"
//   Agent → DIRECT_ANSWER (no need to search DB)
//
//   Question: "What is today's weather in Dubai?"
//   Agent → EXTERNAL_API (needs live data)
//
// This is called AI Orchestration.
// This is what separates AI Engineers from developers
// who just call OpenAI API directly.
// ─────────────────────────────────────────────────────────────

public class AgentDecisionLayer
{
    private readonly IAIProvider _provider;

    public AgentDecisionLayer(IAIProvider provider)
    {
        _provider = provider;
    }

    // This is the prompt we send to AI to make the routing decision
    private const string DecisionPrompt = """
        You are an AI routing agent. 
        Given a user question, decide the best strategy to answer it.
        
        Strategies:
        - DATABASE_SEARCH: The question is about specific technical topics 
          that may be stored in a knowledge base.
          Examples: microservices, cloud infrastructure, CI/CD, 
          database indexing, API concepts, software architecture.
        
        - DIRECT_ANSWER: The question is general knowledge, math, 
          common facts, greetings, or something you can answer 
          confidently without any documents.
          Examples: "What is 2+2?", "Hello", "Explain recursion briefly"
        
        - EXTERNAL_API: The question requires real-time data.
          Examples: weather, stock prices, live news, current events.
        
        Respond with ONLY one of these exact words on the first line:
        DATABASE_SEARCH, DIRECT_ANSWER, EXTERNAL_API
        
        Then on the second line write a short one-sentence reason.
        
        Question: {QUESTION}
        """;

    public async Task<(AgentDecision Decision, string Reason)> DecideAsync(string question)
    {
        // Step 1: Send question to AI for routing decision
        var prompt = DecisionPrompt.Replace("{QUESTION}", question);
        var response = await _provider.CompleteAsync(prompt);

        // Step 2: Parse the response
        var lines = response.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var decisionText = lines[0].Trim().ToUpper();
        var reason = lines.Length > 1
            ? lines[1].Trim()
            : "Agent made a routing decision.";

        // Step 3: Map text to enum
        var decision = decisionText switch
        {
            "DATABASE_SEARCH" => AgentDecision.DatabaseSearch,
            "EXTERNAL_API" => AgentDecision.ExternalApi,
            _ => AgentDecision.DirectAnswer
        };

        return (decision, reason);
    }
}
/*

```

### How the Agent works step by step:
```
User asks: "What is microservices architecture?"
          │
          ▼
Agent sends to OpenAI:
"Given this question, which strategy: 
 DATABASE_SEARCH, DIRECT_ANSWER, or EXTERNAL_API?"
          │
          ▼
OpenAI replies:
"DATABASE_SEARCH
 This question is about a technical topic
 likely stored in the knowledge base."
          │
          ▼
Agent returns: (AgentDecision.DatabaseSearch, "reason...")
          │
          ▼
RAGPipeline executes the DB search path

*/