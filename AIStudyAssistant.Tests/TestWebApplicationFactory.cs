using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AIStudyAssistant.API.Data;
using AIStudyAssistant.API.Services;

namespace AIStudyAssistant.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;
    public Mock<IChatService> MockChatService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing AppDbContext registration
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Create and open an in-memory SQLite connection that persists for the lifetime of the factory
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Replace IChatService with the mock
            var chatServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IChatService));
            if (chatServiceDescriptor != null)
            {
                services.Remove(chatServiceDescriptor);
            }

            // Setup default mock response
            MockChatService
                .Setup(s => s.GetAnswerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string prompt, CancellationToken _) => $"AI Answer to: {prompt}");

            services.AddSingleton(MockChatService.Object);

            // Build service provider and ensure database schema is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Close();
        _connection?.Dispose();
    }
}
