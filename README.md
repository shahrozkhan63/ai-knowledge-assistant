# 🧠 AI Knowledge Assistant
### RAG Pipeline + AI Agent Orchestration · C# + PostgreSQL + OpenAI + Claude

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-316192?style=flat&logo=postgresql)
![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4o-412991?style=flat&logo=openai)
![Claude](https://img.shields.io/badge/Anthropic-Claude-D97757?style=flat&logo=anthropic)

---

## 🎯 What This Project Does

An intelligent knowledge assistant that uses **Retrieval Augmented Generation (RAG)** 
combined with an **AI Agent Decision Layer** to answer questions intelligently.

Instead of blindly searching the database for every question, 
the AI Agent **thinks first** and decides the best strategy:
```
"What is microservices?"     → DATABASE_SEARCH  (search knowledge base)
"What is 2 + 2?"             → DIRECT_ANSWER    (answer from AI knowledge)
"What is today's weather?"   → EXTERNAL_API     (requires live data)
```

---

## 🏗️ Architecture
```
User Question
     │
     ▼
┌─────────────────────────┐
│   AI Agent Decision     │ ← AI decides HOW to answer first
│   Layer                 │
└──────────┬──────────────┘
           │
    ┌──────┴──────────────┐
    ▼                     ▼
DATABASE SEARCH      DIRECT ANSWER
    │
    ▼
EmbeddingService     ← OpenAI text-embedding-3-small
    │
    ▼
VectorSearch         ← PostgreSQL cosine similarity
    │
    ▼
PromptBuilder        ← Injects documents as context
    │
    ▼
IAIProvider          ← OpenAI GPT-4o-mini or Claude Haiku
    │
    ▼
AI Answer + Real Confidence Score
```

---

## ✨ Key Features

- 🧠 **AI Agent Orchestration** — Agent decides strategy before answering
- 📚 **RAG Pipeline** — Retrieves relevant documents from PostgreSQL
- 🔀 **Multi-AI Provider** — Switch between OpenAI and Claude in the UI
- 📐 **Vector Search** — Cosine similarity search in PostgreSQL
- 📊 **Real Confidence Score** — Calculated from actual document similarity
- 💡 **Prompt Engineering** — 3 specialized prompt templates
- 🎨 **Modern UI** — Dark theme with document expand + modal viewer
- 📖 **Swagger API** — Full API documentation at `/swagger`

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8 Web API |
| Database | PostgreSQL + cosine similarity |
| AI Providers | OpenAI GPT-4o-mini, Anthropic Claude Haiku |
| Embeddings | OpenAI text-embedding-3-small (1536 dimensions) |
| Frontend | Vanilla HTML/CSS/JS |
| ORM | Entity Framework Core 8 |

---

## 📁 Project Structure
```
AIKnowledgeAssistant/
│
├── Agents/
│   └── AgentDecisionLayer.cs     ← AI routing agent
│
├── Controllers/
│   └── KnowledgeController.cs    ← REST API endpoints
│
├── Data/
│   └── AppDbContext.cs           ← EF Core + PostgreSQL
│
├── Models/
│   ├── Document.cs               ← Document entity
│   └── AiResponse.cs            ← Request/Response models
│
├── Orchestration/
│   └── RAGPipeline.cs            ← Coordinates full pipeline
│
├── Prompts/
│   └── PromptTemplates.cs        ← Prompt engineering templates
│
├── Services/
│   ├── AIProviders.cs            ← OpenAI + Claude + Factory
│   ├── EmbeddingService.cs       ← Vector generation
│   └── VectorSearchService.cs    ← PostgreSQL vector search
│
├── wwwroot/
│   └── index.html                ← Web UI
│
└── Program.cs                    ← DI registration
```

---

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL
- OpenAI API Key

### 1. Clone the repository
```bash
git clone https://github.com/YOUR_USERNAME/ai-knowledge-assistant.git
cd ai-knowledge-assistant
```

### 2. Setup PostgreSQL
```sql
CREATE DATABASE RAGDemo;

CREATE TABLE documents (
    id SERIAL PRIMARY KEY,
    "Title" TEXT NOT NULL DEFAULT '',
    content TEXT NOT NULL,
    embedding double precision[],
    "CreatedAt" TIMESTAMP DEFAULT NOW()
);

CREATE OR REPLACE FUNCTION cosine_similarity(a double precision[], b double precision[])
RETURNS double precision AS $$
DECLARE
    dot float8 := 0;
    mag_a float8 := 0;
    mag_b float8 := 0;
    i int;
BEGIN
    FOR i IN 1..array_length(a,1) LOOP
        dot := dot + a[i] * b[i];
        mag_a := mag_a + a[i] * a[i];
        mag_b := mag_b + b[i] * b[i];
    END LOOP;
    IF mag_a = 0 OR mag_b = 0 THEN
        RETURN 0;
    END IF;
    RETURN dot / (sqrt(mag_a) * sqrt(mag_b));
END;
$$ LANGUAGE plpgsql;
```

### 3. Configure API Keys
```bash
cp appsettings.Example.json appsettings.json
```
Edit `appsettings.json` and add your keys.

### 4. Run
```bash
dotnet run
```

Open: `http://localhost:5000`
Swagger: `http://localhost:5000/swagger`

---

## 📡 API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/knowledge/ask` | Ask a question — full pipeline |
| POST | `/api/knowledge/documents` | Insert document into knowledge base |
| GET | `/api/knowledge/documents` | List all documents |

---

## 💡 How the Agent Works
```
Question: "Describe a fintech transaction workflow"
     │
     ▼
Agent sends to OpenAI:
"Which strategy: DATABASE_SEARCH, DIRECT_ANSWER, or EXTERNAL_API?"
     │
     ▼
OpenAI: "DATABASE_SEARCH — technical topic likely in knowledge base"
     │
     ▼
Pipeline: Generate embedding → Search PostgreSQL → Build prompt → Call LLM
     │
     ▼
Answer with real confidence score based on document similarity
```

---

## 🔑 Environment Variables

| Key | Description |
|---|---|
| `OpenAI:ApiKey` | Your OpenAI API key |
| `Anthropic:ApiKey` | Your Anthropic Claude API key |
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |

---

## 👨‍💻 Author

**Muhammad Shahroz Khan**
Senior .NET Developer / AI Engineer
[LinkedIn](https://www.linkedin.com/in/muhammadshahrozkhan63) · [GitHub](https://github.com/shahrozkhan63/)