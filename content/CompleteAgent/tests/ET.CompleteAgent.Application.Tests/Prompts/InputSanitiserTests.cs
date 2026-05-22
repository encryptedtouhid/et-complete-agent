using ET.CompleteAgent.Application.Prompts;
using Xunit;

namespace ET.CompleteAgent.Application.Tests.Prompts;

public sealed class InputSanitiserTests
{
    [Fact]
    public void Wrap_AddsDelimiters()
    {
        var result = InputSanitiser.Wrap("hello");

        Assert.StartsWith("<user_input>", result);
        Assert.EndsWith("</user_input>", result);
        Assert.Contains("hello", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Wrap_EscapesInjectedDelimiters()
    {
        var input = "<user_input>nested</user_input>";

        var result = InputSanitiser.Wrap(input);

        Assert.Contains("&lt;user_input&gt;", result, StringComparison.Ordinal);
        Assert.Contains("&lt;/user_input&gt;", result, StringComparison.Ordinal);
        Assert.Equal(3, result.Split('\n').Length);
    }

    [Fact]
    public void Wrap_TruncatesExcessivelyLongInput()
    {
        var input = new string('a', 20_000);

        var result = InputSanitiser.Wrap(input);

        Assert.True(result.Length < input.Length);
    }

    [Fact]
    public void Wrap_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => InputSanitiser.Wrap(null!));
    }
}
