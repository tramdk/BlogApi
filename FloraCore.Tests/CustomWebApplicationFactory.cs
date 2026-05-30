using FloraCore.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Data.Common;
using System.Collections.Generic;
using FloraCore.Application.Common.Interfaces;
using System.Linq;

namespace FloraCore.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Initialize and open the persistent connection
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Ensure we don't try to connect to a real Redis server during tests
        Environment.SetEnvironmentVariable("SKIP_REDIS", "true");
        // Ensure roles and initial data are seeded for integration tests
        Environment.SetEnvironmentVariable("SEED_DATA", "true");
        Environment.SetEnvironmentVariable("Jwt__Secret", "YourSuperSecretKeyWithAtLeast32Characters!");
        Environment.SetEnvironmentVariable("Cloudinary__CloudName", "test");
        Environment.SetEnvironmentVariable("Cloudinary__ApiKey", "test");
        Environment.SetEnvironmentVariable("Cloudinary__ApiSecret", "test");
        Environment.SetEnvironmentVariable("MailSettings__Server", "test");
        Environment.SetEnvironmentVariable("MailSettings__SenderName", "test");
        Environment.SetEnvironmentVariable("MailSettings__SenderEmail", "test");
        Environment.SetEnvironmentVariable("MailSettings__Username", "test");
        Environment.SetEnvironmentVariable("MailSettings__Password", "test");
        Environment.SetEnvironmentVariable("PaymentGateways__ApiUrl", "http://localhost:5000");
        Environment.SetEnvironmentVariable("PaymentGateways__FrontendUrl", "http://localhost:3000");
        Environment.SetEnvironmentVariable("PaymentGateways__VnPay__Url", "https://test");
        Environment.SetEnvironmentVariable("PaymentGateways__VnPay__TmnCode", "test");
        Environment.SetEnvironmentVariable("PaymentGateways__VnPay__HashSecret", "test");
        Environment.SetEnvironmentVariable("PaymentGateways__MoMo__Url", "https://test");
        Environment.SetEnvironmentVariable("PaymentGateways__MoMo__PartnerCode", "test");
        Environment.SetEnvironmentVariable("PaymentGateways__MoMo__AccessKey", "test");
        Environment.SetEnvironmentVariable("PaymentGateways__MoMo__SecretKey", "test");
        Environment.SetEnvironmentVariable("PaymentGateways__PayOS__Url", "https://test");
        Environment.SetEnvironmentVariable("PaymentGateways__PayOS__ClientId", "test");
        Environment.SetEnvironmentVariable("PaymentGateways__PayOS__ApiKey", "test");
        Environment.SetEnvironmentVariable("PaymentGateways__PayOS__ChecksumKey", "test");

        builder.UseEnvironment("Testing");
        
        // Configure test configuration (e.g., upload folder)
        builder.ConfigureAppConfiguration((context, config) => {
            config.AddInMemoryCollection(new Dictionary<string, string?> {
                {"FileStorage:UploadFolder", "test_uploads"},
                {"Jwt:Secret", "YourSuperSecretKeyWithAtLeast32Characters!"},
                {"Cloudinary:CloudName", "test"},
                {"Cloudinary:ApiKey", "test"},
                {"Cloudinary:ApiSecret", "test"},
                {"MailSettings:Server", "test"},
                {"MailSettings:SenderName", "test"},
                {"MailSettings:SenderEmail", "test"},
                {"MailSettings:Username", "test"},
                {"MailSettings:Password", "test"}
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration and options
            var descriptors = services.Where(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) || 
                     d.ServiceType == typeof(AppDbContext) ||
                     d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true).ToList();
            
            foreach (var d in descriptors)
            {
                services.Remove(d);
            }

            // Add SQLite in-memory database for testing using the persistent connection
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
                // Suppress the warning for pending model changes in tests
                #pragma warning disable EF1001
                options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                #pragma warning restore EF1001
            });

            // Ensure database is created and seeded
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                
                // We also need to manually seed because Program.cs skips it in Testing
                var seeder = ActivatorUtilities.CreateInstance<DatabaseSeeder>(scope.ServiceProvider);
                seeder.SeedAsync().GetAwaiter().GetResult();
            }

            // Override IFileService with FakeFileService
            services.AddScoped<IFileService, FloraCore.Tests.Mocks.FakeFileService>();

            // Override IEmailService with Mock to prevent SMTP connection during tests
            var mockEmailService = new Moq.Mock<IEmailService>();
            services.AddSingleton(mockEmailService.Object);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Close();
        _connection?.Dispose();
    }
}
