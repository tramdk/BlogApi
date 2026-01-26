using System;

namespace BlogApi.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Jti { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
}
