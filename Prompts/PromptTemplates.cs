namespace AIKnowledgeAssistant.Prompts;

// This is Prompt Engineering — one of the most important 
// skills in AI development.
// Instead of hardcoding prompts everywhere, we centralize them here.

public static class PromptTemplates
{
    // ── Used when Agent decides: DATABASE_SEARCH ──────────────
    // Injects retrieved documents as context
    // Tells AI to answer ONLY from those documents
    public static string BuildRagPrompt(string context, string question) => $"""
        You are an expert AI Knowledge Assistant.
        Answer the question using ONLY the documents provided below.
        If the answer is not in the documents, say: 
        "I don't have enough information in my knowledge base to answer this."
        
        Be concise, accurate, and professional.
        
        DOCUMENTS:
        {context}
        
        QUESTION: {question}
        
        ANSWER:
        """;

    // ── Used when Agent decides: DIRECT_ANSWER ────────────────
    // No documents needed — AI answers from its own knowledge
    public static string BuildDirectPrompt(string question) => $"""
        You are a helpful AI assistant. 
        Answer this question clearly and concisely.
        
        QUESTION: {question}
        
        ANSWER:
        """;

    // ── Used when Agent decides: EXTERNAL_API ────────────────
    // We don't have live data — tell user politely
    public static string BuildExternalApiPrompt(string question) => $"""
        The user asked a question that requires real-time data.
        Explain politely that you cannot fetch live data right now,
        and suggest where they might find the answer.
        
        QUESTION: {question}
        """;
}