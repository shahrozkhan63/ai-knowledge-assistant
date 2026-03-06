using Microsoft.EntityFrameworkCore;
using AIKnowledgeAssistant.Models;

namespace AIKnowledgeAssistant.Data;

public class AppDbContext : DbContext
{
    public DbSet<Document> Documents { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No pgvector needed — using plain double[] array
        modelBuilder.Entity<Document>()
            .ToTable("documents")
            .Property(d => d.Embedding)
            .HasColumnType("float8[]"); // plain PostgreSQL double array
    }
}