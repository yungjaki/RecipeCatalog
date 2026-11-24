namespace RecipeCatalog.Core.Validation;

/// <summary>Резултат от валидация: списък с грешки по име на поле.</summary>
public sealed class ValidationResult
{
    private readonly List<ValidationError> _errors = new();

    public IReadOnlyList<ValidationError> Errors => _errors;
    public bool IsValid => _errors.Count == 0;

    public void Add(string field, string message) => _errors.Add(new ValidationError(field, message));
}

public sealed record ValidationError(string Field, string Message);
