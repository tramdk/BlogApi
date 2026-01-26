using System.Text.RegularExpressions;

namespace BlogApi.Domain.ValueObjects;

/// <summary>
/// Value object representing an email address.
/// Immutable by design with email validation.
/// </summary>
public record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        var trimmedValue = value.Trim().ToLowerInvariant();
        
        if (!EmailRegex.IsMatch(trimmedValue))
            throw new ArgumentException("Invalid email format", nameof(value));

        Value = trimmedValue;
    }

    public static implicit operator string(Email email) => email.Value;
    public static explicit operator Email(string value) => new(value);

    public override string ToString() => Value;
}
