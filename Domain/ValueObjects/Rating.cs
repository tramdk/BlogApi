namespace BlogApi.Domain.ValueObjects;

/// <summary>
/// Value object representing a rating score (1-5).
/// Immutable by design with validation.
/// </summary>
public record Rating
{
    public const int MinValue = 1;
    public const int MaxValue = 5;

    public int Score { get; }

    public Rating(int score)
    {
        if (score < MinValue || score > MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(score), 
                $"Rating must be between {MinValue} and {MaxValue}");

        Score = score;
    }

    public static Rating Minimum => new(MinValue);
    public static Rating Maximum => new(MaxValue);

    public static implicit operator int(Rating rating) => rating.Score;
    public static explicit operator Rating(int score) => new(score);

    public bool IsExcellent => Score >= 4;
    public bool IsPoor => Score <= 2;

    public override string ToString() => $"{Score}/{MaxValue} stars";
}

/// <summary>
/// Value object representing aggregated rating statistics.
/// </summary>
public record RatingStatistics
{
    public double AverageRating { get; }
    public int TotalRatings { get; }

    public RatingStatistics(double averageRating, int totalRatings)
    {
        if (averageRating < 0 || averageRating > Rating.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(averageRating));
        
        if (totalRatings < 0)
            throw new ArgumentOutOfRangeException(nameof(totalRatings));

        AverageRating = averageRating;
        TotalRatings = totalRatings;
    }

    public static RatingStatistics Empty => new(0, 0);

    public RatingStatistics AddRating(int score)
    {
        var newTotal = TotalRatings + 1;
        var newAverage = ((AverageRating * TotalRatings) + score) / newTotal;
        return new RatingStatistics(newAverage, newTotal);
    }

    public override string ToString() => $"{AverageRating:F1}/5 ({TotalRatings} ratings)";
}
