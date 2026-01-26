namespace BlogApi.Domain.Exceptions;

/// <summary>
/// Post-specific domain exceptions.
/// </summary>
public class PostNotFoundException : EntityNotFoundException
{
    public PostNotFoundException(Guid postId) : base("Post", postId) { }
}

public class PostAlreadyRatedException : DomainException
{
    public Guid PostId { get; }
    public Guid UserId { get; }

    public PostAlreadyRatedException(Guid postId, Guid userId)
        : base("POST_ALREADY_RATED", $"User has already rated post with ID '{postId}'.")
    {
        PostId = postId;
        UserId = userId;
    }
}

/// <summary>
/// Product-specific domain exceptions.
/// </summary>
public class ProductNotFoundException : EntityNotFoundException
{
    public ProductNotFoundException(Guid productId) : base("Product", productId) { }
}

public class InsufficientStockException : DomainException
{
    public Guid ProductId { get; }
    public int RequestedQuantity { get; }
    public int AvailableStock { get; }

    public InsufficientStockException(Guid productId, int requestedQuantity, int availableStock)
        : base("INSUFFICIENT_STOCK", 
               $"Insufficient stock for product. Requested: {requestedQuantity}, Available: {availableStock}")
    {
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
        AvailableStock = availableStock;
    }
}

/// <summary>
/// Cart-specific domain exceptions.
/// </summary>
public class CartNotFoundException : EntityNotFoundException
{
    public CartNotFoundException(Guid userId) : base("Cart", $"user:{userId}") { }
}

public class CartItemNotFoundException : EntityNotFoundException
{
    public CartItemNotFoundException(Guid cartId, Guid productId) 
        : base("CartItem", $"cart:{cartId}/product:{productId}") { }
}

/// <summary>
/// User-specific domain exceptions.
/// </summary>
public class UserNotFoundException : EntityNotFoundException
{
    public UserNotFoundException(Guid userId) : base("User", userId) { }
    public UserNotFoundException(string email) : base("User", $"email:{email}") { }
}

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException() 
        : base("INVALID_CREDENTIALS", "The provided credentials are invalid.") { }
}
