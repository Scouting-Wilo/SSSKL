using System.Reflection;
using FluentAssertions;
using SSSKL.Mobile.Presentation.Localization;

namespace SSSKL.Mobile.Test.Presentation.Localization;

/// <summary>
/// The Dutch UI strings: every one must have text (XAML binds them with x:Static, so a blank one only shows up
/// as an empty label), and the formatted ones must show their arguments and use the right singular/plural.
/// </summary>
[TestClass]
public class TextsTests
{
    private static readonly MethodInfo[] Formatters = typeof(Texts)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => m.ReturnType == typeof(string) && !m.IsSpecialName)
        .ToArray();

    [TestMethod]
    public void EveryText_HasText()
    {
        var properties = typeof(Texts).GetProperties(BindingFlags.Public | BindingFlags.Static);
        properties.Should().NotBeEmpty();

        foreach (var property in properties)
        {
            var value = property.GetValue(null) as string;
            value.Should().NotBeNullOrWhiteSpace(property.Name);
            value.Should().Be(value!.Trim(), $"{property.Name} has no leading or trailing whitespace");
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    public void EveryFormattedText_ShowsItsArguments(int number)
    {
        Formatters.Should().NotBeEmpty();

        foreach (var method in Formatters)
        {
            var arguments = method.GetParameters()
                .Select(p => p.ParameterType == typeof(int) ? (object)number : $"«{p.Name}»")
                .ToArray();

            var value = (string)method.Invoke(null, arguments)!;

            value.Should().NotBeNullOrWhiteSpace(method.Name);
            foreach (var argument in arguments)
            {
                value.Should().Contain(argument.ToString(), $"{method.Name} shows its argument");
            }
        }
    }

    [TestMethod]
    public void Counts_UseSingularForOne()
    {
        Texts.Days(1).Should().Be("1 dag");
        Texts.Days(2).Should().Be("2 dagen");
        Texts.Votes(1).Should().Be("1 stem");
        Texts.Votes(5).Should().Be("5 stemmen");
        Texts.CommentCount(1).Should().Be("1 reactie");
        Texts.CommentCount(0).Should().Be("0 reacties");
        Texts.Likes(1).Should().Be("1 duim");
        Texts.Hearts(2).Should().Be("2 hartjes");
        Texts.WinnerCount(1).Should().Be("1 winnaar");
        Texts.WinnerCount(2).Should().Be("2 winnaars");
        Texts.ChooserNeedMore(1).Should().Be("Nog 1 vinger nodig");
        Texts.ChooserNeedMore(2).Should().Be("Nog 2 vingers nodig");
    }

    [TestMethod]
    public void OrderPlacedDetail_ShowsAmountProductsAndTotal()
    {
        Texts.OrderPlacedDetail("Cola", 2, "€ 3,00").Should().Be("2× Cola · € 3,00");
    }
}
