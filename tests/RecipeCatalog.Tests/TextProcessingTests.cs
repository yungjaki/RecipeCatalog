using RecipeCatalog.Core.Models;
using RecipeCatalog.Core.Services;

namespace RecipeCatalog.Tests;

[TestFixture]
public class TextNormalizerTests
{
    [TestCase("  ДОМАТИ  ", "домати")]
    [TestCase("Шопска   Салата", "шопска салата")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void Normalize_LowercasesTrimsAndCollapsesSpaces(string? input, string expected)
    {
        Assert.That(TextNormalizer.Normalize(input), Is.EqualTo(expected));
    }

    [Test]
    public void EscapeLike_EscapesWildcards()
    {
        Assert.That(TextNormalizer.EscapeLike(@"50%_a\b"), Is.EqualTo(@"50\%\_a\\b"));
    }
}

[TestFixture]
public class SearchTermParserTests
{
    [Test]
    public void Parse_SplitsByCommaAndNormalizes()
    {
        var terms = SearchTermParser.Parse("Домати, СИРЕНЕ ; лук");
        Assert.That(terms, Is.EqualTo(new[] { "домати", "сирене", "лук" }));
    }

    [Test]
    public void Parse_RemovesEmptyAndDuplicateTerms()
    {
        var terms = SearchTermParser.Parse("сирене,, Сирене , ");
        Assert.That(terms, Is.EqualTo(new[] { "сирене" }));
    }

    [Test]
    public void Parse_LimitsNumberOfTerms()
    {
        var input = string.Join(",", Enumerable.Range(1, 30).Select(i => "п" + i));
        Assert.That(SearchTermParser.Parse(input), Has.Count.EqualTo(SearchTermParser.MaxTerms));
    }

    [TestCase(null)]
    [TestCase("   ")]
    public void Parse_EmptyInput_ReturnsNoTerms(string? input)
    {
        Assert.That(SearchTermParser.Parse(input), Is.Empty);
    }
}

[TestFixture]
public class IngredientLineParserTests
{
    [Test]
    public void Parse_ReadsNameAndQuantity()
    {
        var result = IngredientLineParser.Parse("Домати - 4 бр.\nСирене - 200 г");
        Assert.That(result, Is.EqualTo(new[] { new Ingredient("Домати", "4 бр."), new Ingredient("Сирене", "200 г") }));
    }

    [Test]
    public void Parse_LineWithoutQuantity_KeepsNameOnly()
    {
        var result = IngredientLineParser.Parse("Сол");
        Assert.That(result.Single(), Is.EqualTo(new Ingredient("Сол", string.Empty)));
    }

    [Test]
    public void Parse_IgnoresEmptyLinesAndBullets()
    {
        var result = IngredientLineParser.Parse("\r\n- Лук - 1 глава\r\n\r\n• Олио\r\n");
        Assert.That(result.Select(i => i.Name), Is.EqualTo(new[] { "Лук", "Олио" }));
    }

    [Test]
    public void FormatThenParse_RoundTrips()
    {
        var original = new[] { new Ingredient("Яйца", "3 бр."), new Ingredient("Сол", "") };
        var parsed = IngredientLineParser.Parse(IngredientLineParser.Format(original));
        Assert.That(parsed, Is.EqualTo(original));
    }
}
