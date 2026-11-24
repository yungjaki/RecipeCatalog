using System.Diagnostics;
using RecipeCatalog.Core.Data;
using RecipeCatalog.Core.Models;
using RecipeCatalog.Core.Validation;

namespace RecipeCatalog.Core.Services;

/// <summary>Резултат от операция: успех или списък с грешки за показване на потребителя.</summary>
public sealed class OperationResult<T>
{
    private OperationResult(bool success, T? value, IReadOnlyList<ValidationError> errors)
    {
        Success = success;
        Value = value;
        Errors = errors;
    }

    public bool Success { get; }
    public T? Value { get; }
    public IReadOnlyList<ValidationError> Errors { get; }

    public static OperationResult<T> Ok(T value) => new(true, value, Array.Empty<ValidationError>());

    public static OperationResult<T> Fail(IReadOnlyList<ValidationError> errors) => new(false, default, errors);

    public static OperationResult<T> Fail(string field, string message) =>
        new(false, default, new[] { new ValidationError(field, message) });
}

/// <summary>Резултат от търсене заедно с измереното време за обработка.</summary>
public sealed record TimedSearchResult(SearchResult Result, double ElapsedMilliseconds);

/// <summary>Бизнес логика на каталога: валидация + извикване на хранилището.</summary>
public sealed class RecipeService
{
    private readonly IRecipeRepository _repository;

    public RecipeService(IRecipeRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<Category> GetCategories() => _repository.GetCategories();

    public Recipe? GetById(int id) => id > 0 ? _repository.GetById(id) : null;

    public TimedSearchResult Search(SearchQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var stopwatch = Stopwatch.StartNew();
        var result = _repository.Search(query);
        stopwatch.Stop();
        return new TimedSearchResult(result, stopwatch.Elapsed.TotalMilliseconds);
    }

    public OperationResult<int> Create(RecipeInput input)
    {
        var validation = Validate(input);
        if (!validation.IsValid)
        {
            return OperationResult<int>.Fail(validation.Errors);
        }

        return OperationResult<int>.Ok(_repository.Add(input));
    }

    public OperationResult<int> Update(int id, RecipeInput input)
    {
        var validation = Validate(input);
        if (!validation.IsValid)
        {
            return OperationResult<int>.Fail(validation.Errors);
        }

        return _repository.Update(id, input)
            ? OperationResult<int>.Ok(id)
            : OperationResult<int>.Fail(string.Empty, "Рецептата не е намерена.");
    }

    public bool Delete(int id) => id > 0 && _repository.Delete(id);

    public OperationResult<int> Rate(int recipeId, int stars)
    {
        if (!RatingValidator.IsValid(stars))
        {
            return OperationResult<int>.Fail("Stars", "Оценката трябва да е от 1 до 5 звезди.");
        }

        return _repository.AddRating(recipeId, stars)
            ? OperationResult<int>.Ok(stars)
            : OperationResult<int>.Fail(string.Empty, "Рецептата не е намерена.");
    }

    private ValidationResult Validate(RecipeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var categoryIds = _repository.GetCategories().Select(c => c.Id).ToList();
        return RecipeValidator.Validate(input, categoryIds);
    }
}
