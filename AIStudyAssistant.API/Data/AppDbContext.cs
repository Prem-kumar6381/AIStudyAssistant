using Microsoft.EntityFrameworkCore;
using AIStudyAssistant.API.Data.Entities;

namespace AIStudyAssistant.API.Data;

/// <summary>
/// Entity Framework Core database context for the AI Study Assistant.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(256);

            entity.HasIndex(u => u.Email)
                  .IsUnique();

            entity.Property(u => u.DisplayName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(u => u.PasswordHash)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(u => u.CreatedAt)
                  .IsRequired();

            entity.Property(u => u.UpdatedAt)
                  .IsRequired();

            entity.HasMany(u => u.Conversations)
                  .WithOne(c => c.User)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Conversation Configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(c => c.CreatedAt)
                  .IsRequired();

            entity.Property(c => c.UpdatedAt)
                  .IsRequired();

            entity.HasIndex(c => c.UserId);
            entity.HasIndex(c => c.UpdatedAt);

            entity.HasMany(c => c.Messages)
                  .WithOne(m => m.Conversation)
                  .HasForeignKey(m => m.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Message Configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Role)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(m => m.Content)
                  .IsRequired();

            entity.Property(m => m.CreatedAt)
                  .IsRequired();

            entity.HasIndex(m => m.ConversationId);
            entity.HasIndex(m => m.CreatedAt);
        });
    }
}
