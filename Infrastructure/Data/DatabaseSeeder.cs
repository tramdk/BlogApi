using BlogApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UUIDNext;
using BlogApi.Application.Common.Constants;

namespace BlogApi.Infrastructure.Data;

/// <summary>
/// Database seeding service to initialize default data.
/// Separates seeding logic from Program.cs for better maintainability.
/// </summary>
public class DatabaseSeeder
{
    private readonly AppDbContext _context;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<AppUser> _userManager;

    public DatabaseSeeder(
        AppDbContext context,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<AppUser> userManager)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Ensure the database schema matches the model (Create tables).
    /// </summary>
    public async Task EnsureCreatedAsync()
    {
        // Apply migrations to ensure database schema is up to date
        // This is preferred over EnsureCreatedAsync as it supports schema evolution
        await _context.Database.MigrateAsync();
    }

    /// <summary>
    /// Seed default data.
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            await SeedRolesAsync();
            var adminUser = await SeedAdminUserAsync();
            await SeedProductCategoriesAndProductsAsync();
            await SeedPostsAsync(adminUser);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "An error occurred while seeding the database.");
        }
    }

    private async Task SeedRolesAsync()
    {
        if (!await _roleManager.RoleExistsAsync(RoleConstants.Admin))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(RoleConstants.Admin));
        }

        if (!await _roleManager.RoleExistsAsync(RoleConstants.User))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(RoleConstants.User));
        }
    }

    private async Task<AppUser> SeedAdminUserAsync()
    {
        var adminEmail = "admin@blogapi.com";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Admin",
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(adminUser, "Admin123!");
            await _userManager.AddToRoleAsync(adminUser, RoleConstants.Admin);
        }

        return adminUser;
    }

    private async Task SeedProductCategoriesAndProductsAsync()
    {
        if (_context.ProductCategories.Any())
        {
            // Categories exist, only check products
            if (!_context.Products.Any())
            {
                await SeedProductsWithoutCategoryAsync();
            }
            return;
        }

        var techCategory = new ProductCategory
        {
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            Name = "Technology",
            Description = "Electronic gadgets and devices",
            CreatedAt = DateTime.UtcNow
        };

        var audioCategory = new ProductCategory
        {
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            Name = "Audio",
            Description = "Headphones, speakers and more",
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductCategories.AddRange(techCategory, audioCategory);
        await _context.SaveChangesAsync();

        if (!_context.Products.Any())
        {
            _context.Products.AddRange(new List<Product>
            {
                new Product
                {
                    Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                    Name = "Smartphone X",
                    Description = "Latest model with stunning display",
                    Price = 999.99m,
                    Stock = 50,
                    CreatedAt = DateTime.UtcNow,
                    CategoryId = techCategory.Id
                },
                new Product
                {
                    Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                    Name = "Laptop Pro 16",
                    Description = "Powerful machine for creative professionals",
                    Price = 2499.99m,
                    Stock = 15,
                    CreatedAt = DateTime.UtcNow,
                    CategoryId = techCategory.Id
                },
                new Product
                {
                    Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                    Name = "Wireless Buds",
                    Description = "Crystal clear sound with noise cancellation",
                    Price = 159.50m,
                    Stock = 100,
                    CreatedAt = DateTime.UtcNow,
                    CategoryId = audioCategory.Id
                }
            });
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedProductsWithoutCategoryAsync()
    {
        _context.Products.AddRange(new List<Product>
        {
            new Product
            {
                Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                Name = "Smartphone X",
                Description = "Latest model with stunning display",
                Price = 999.99m,
                Stock = 50,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                Name = "Laptop Pro 16",
                Description = "Powerful machine for creative professionals",
                Price = 2499.99m,
                Stock = 15,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                Name = "Wireless Buds",
                Description = "Crystal clear sound with noise cancellation",
                Price = 159.50m,
                Stock = 100,
                CreatedAt = DateTime.UtcNow
            }
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedPostsAsync(AppUser adminUser)
    {
        if (_context.Posts.Any()) return;

        _context.Posts.AddRange(new List<Post>
        {
            new Post
            {
                Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                Title = "Refactoring to UUID v7",
                Content = "The move to UUID v7 has significantly improved our database sortability while maintaining uniqueness.",
                AuthorId = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                CategoryId = "blog"
            },
            new Post
            {
                Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                Title = "Clean Architecture with .NET 8",
                Content = "Modern web APIs benefit greatly from a decoupled, maintenance-friendly architecture.",
                AuthorId = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                CategoryId = "blog"
            },
            new Post
            {
                Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                Title = "Real-time Notifications with SignalR",
                Content = "Keep your users engaged with instant updates delivered straight to their devices.",
                AuthorId = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                CategoryId = "blog"
            }
        });
        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Extension methods for WebApplication to seed database.
/// </summary>
public static class DatabaseSeederExtensions
{
    /// <summary>
    /// Initialize the database schema (Create Tables).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = ActivatorUtilities.CreateInstance<DatabaseSeeder>(scope.ServiceProvider);
        await seeder.EnsureCreatedAsync();
    }

    /// <summary>
    /// Seed the database with default data.
    /// </summary>
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = ActivatorUtilities.CreateInstance<DatabaseSeeder>(scope.ServiceProvider);
        await seeder.SeedAsync();
    }
}
