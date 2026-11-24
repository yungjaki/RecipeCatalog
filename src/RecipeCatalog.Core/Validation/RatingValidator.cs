namespace RecipeCatalog.Core.Validation;

/// <summary>Рейтингът е цяло число от 1 до 5 звезди.</summary>
public static class RatingValidator
{
    public const int Min = 1;
    public const int Max = 5;

    public static bool IsValid(int stars) => stars >= Min && stars <= Max;
}
