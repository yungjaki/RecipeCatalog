using RecipeCatalog.Core.Models;
using RecipeCatalog.Core.Validation;

namespace RecipeCatalog.Tests;

[TestFixture]
public class RecipeValidatorTests
{
    private static readonly int[] Categories = { 1, 2, 3 };

    private static RecipeInput ValidInput() => TestDatabase.Recipe("Шопска салата", 1, "Домати", "Сирене");

    private static IEnumerable<string> FieldsWithErrors(RecipeInput input) =>
        RecipeValidator.Validate(input, Categories).Errors.Select(e => e.Field);

    [Test]
    public void Validate_CorrectInput_IsValid()
    {
        Assert.That(RecipeValidator.Validate(ValidInput(), Categories).IsValid, Is.True);
    }

    [TestCase("")]
    [TestCase("ab")]
    [TestCase("   ")]
    public void Validate_TooShortName_Fails(string name)
    {
        var input = ValidInput();
        input.Name = name;
        Assert.That(FieldsWithErrors(input), Does.Contain(nameof(RecipeInput.Name)));
    }

    [Test]
    public void Validate_TooLongName_Fails()
    {
        var input = ValidInput();
        input.Name = new string('а', RecipeValidator.NameMax + 1);
        Assert.That(FieldsWithErrors(input), Does.Contain(nameof(RecipeInput.Name)));
    }

    [Test]
    public void Validate_UnknownCategory_Fails()
    {
        var input = ValidInput();
        input.CategoryId = 99;
        Assert.That(FieldsWithErrors(input), Does.Contain(nameof(RecipeInput.CategoryId)));
    }

    [Test]
    public void Validate_NoIngredients_Fails()
    {
        var input = ValidInput();
        input.Ingredients.Clear();
        Assert.That(FieldsWithErrors(input), Does.Contain(nameof(RecipeInput.Ingredients)));
    }

    [Test]
    public void Validate_ShortInstructions_Fails()
    {
        var input = ValidInput();
        input.Instructions = "Смесете.";
        Assert.That(FieldsWithErrors(input), Does.Contain(nameof(RecipeInput.Instructions)));
    }

    [TestCase(0)]
    [TestCase(-5)]
    [TestCase(1441)]
    public void Validate_PrepTimeOutOfRange_Fails(int minutes)
    {
        var input = ValidInput();
        input.PrepTimeMinutes = minutes;
        Assert.That(FieldsWithErrors(input), Does.Contain(nameof(RecipeInput.PrepTimeMinutes)));
    }

    [Test]
    public void Validate_EmptyInput_ReportsAllErrors()
    {
        var result = RecipeValidator.Validate(new RecipeInput(), Categories);
        Assert.That(result.Errors, Has.Count.EqualTo(5));
    }
}

[TestFixture]
public class RatingValidatorTests
{
    [TestCase(1, true)]
    [TestCase(5, true)]
    [TestCase(0, false)]
    [TestCase(6, false)]
    [TestCase(-1, false)]
    public void IsValid_AcceptsOnlyOneToFive(int stars, bool expected)
    {
        Assert.That(RatingValidator.IsValid(stars), Is.EqualTo(expected));
    }
}

[TestFixture]
public class ImageFileValidatorTests
{
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0 };
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0 };

    [Test]
    public void Validate_RealPng_IsAccepted()
    {
        Assert.That(ImageFileValidator.Validate("photo.png", 1000, Png), Is.Null);
    }

    [Test]
    public void Validate_RealJpegWithUppercaseExtension_IsAccepted()
    {
        Assert.That(ImageFileValidator.Validate("PHOTO.JPG", 1000, Jpeg), Is.Null);
    }

    [TestCase("script.exe")]
    [TestCase("page.html")]
    [TestCase("noextension")]
    public void Validate_ForbiddenExtension_IsRejected(string fileName)
    {
        Assert.That(ImageFileValidator.Validate(fileName, 1000, Png), Is.Not.Null);
    }

    [Test]
    public void Validate_FileLargerThanLimit_IsRejected()
    {
        Assert.That(ImageFileValidator.Validate("a.png", ImageFileValidator.MaxBytes + 1, Png), Is.Not.Null);
    }

    [Test]
    public void Validate_ContentDoesNotMatchExtension_IsRejected()
    {
        Assert.That(ImageFileValidator.Validate("fake.png", 1000, Jpeg), Is.Not.Null);
    }
}
