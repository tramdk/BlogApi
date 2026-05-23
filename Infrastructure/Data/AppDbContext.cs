using FloraCore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FloraCore.Infrastructure.Data;

/// <summary>
/// Application database context.
/// </summary>
public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for configuring the context.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
    }

    /// <summary>
    /// Gets or sets the posts.
    /// </summary>
    public DbSet<Post> Posts => Set<Post>();

    /// <summary>
    /// Gets or sets the post categories.
    /// </summary>
    public DbSet<PostCategory> PostCategories => Set<PostCategory>();

    /// <summary>
    /// Gets or sets the refresh tokens.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Gets or sets the products.
    /// </summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>
    /// Gets or sets the product categories.
    /// </summary>
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    /// <summary>
    /// Gets or sets the product reviews.
    /// </summary>
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    /// <summary>
    /// Gets or sets the carts.
    /// </summary>
    public DbSet<Cart> Carts => Set<Cart>();

    /// <summary>
    /// Gets or sets the cart items.
    /// </summary>
    public DbSet<CartItem> CartItems => Set<CartItem>();

    /// <summary>
    /// Gets or sets the favorites.
    /// </summary>
    public DbSet<Favorite> Favorites => Set<Favorite>();

    /// <summary>
    /// Gets or sets the chat messages.
    /// </summary>
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    /// <summary>
    /// Gets or sets the notifications.
    /// </summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>
    /// Gets or sets the file metadata.
    /// </summary>
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();

    /// <summary>
    /// Gets or sets the outbox messages.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        /// <summary>
    /// Gets or sets the website info.
    /// </summary>
    public DbSet<WebsiteInfo> WebsiteInfos => Set<WebsiteInfo>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Post>(entity =>
        {
            entity.HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.Property(p => p.ImageUrl).IsRequired(false);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<FileMetadata>(entity =>
        {
            entity.HasOne(f => f.UploadedBy)
                .WithMany()
                .HasForeignKey(f => f.UploadedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ProductReview>(entity =>
        {
            entity.HasOne(pr => pr.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(pr => pr.ProductId);

            entity.HasOne(pr => pr.User)
                .WithMany(u => u.ProductReviews)
                .HasForeignKey(pr => pr.UserId);
        });

        builder.Entity<Cart>(entity =>
        {
            entity.HasOne(c => c.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId);
        });

        builder.Entity<CartItem>(entity =>
        {
            entity.HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId);

            entity.HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId);

            entity.HasIndex(ci => new { ci.CartId, ci.ProductId })
                .IsUnique();
        });

        builder.Entity<Favorite>(entity =>
        {
            entity.HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId);

            entity.HasOne(f => f.Product)
                .WithMany()
                .HasForeignKey(f => f.ProductId);
        });

        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId);
        });
    }
}
