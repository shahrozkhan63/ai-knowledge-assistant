using AIKnowledgeAssistant.Services;

namespace AIKnowledgeAssistant.Models;

public class AiResponse
{
    public string Answer { get; set; } = string.Empty;
    public string DecisionReason { get; set; } = string.Empty;
    public AgentDecision Decision { get; set; }
    public List<RetrievedDocument> RetrievedDocuments { get; set; } = new();
    public string Provider { get; set; } = string.Empty;
    public float ConfidenceScore { get; set; }
    public long ResponseTimeMs { get; set; }
}

public class AskRequest
{
    public string Question { get; set; } = string.Empty;
    public string Provider { get; set; } = "openai";
}

public class InsertDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public enum AgentDecision
{
    DirectAnswer,
    DatabaseSearch,
    ExternalApi
}