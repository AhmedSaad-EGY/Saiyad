using FluentAssertions;
using Sayiad.Data.Configurations;

namespace Sayiad.Tests.Configurations;

public class TransactionTypeValueConverterTests
{
    private readonly TransactionTypeValueConverter _converter = new();

    [Fact]
    public void ConvertFromProvider_WhenValueIsLegacyHold_ReturnsHoldDeduction()
    {
        var result = _converter.ConvertFromProviderExpression.Compile()("Hold");

        result.Should().Be(TransactionType.HoldDeduction);
    }

    [Theory]
    [InlineData(" hold ")]
    [InlineData("HOLD")]
    public void ConvertFromProvider_WhenLegacyHoldHasDifferentCasingOrWhitespace_ReturnsHoldDeduction(string value)
    {
        var result = _converter.ConvertFromProviderExpression.Compile()(value);

        result.Should().Be(TransactionType.HoldDeduction);
    }

    [Fact]
    public void ConvertFromProvider_WhenValueIsHoldDeduction_ReturnsHoldDeduction()
    {
        var result = _converter.ConvertFromProviderExpression.Compile()("HoldDeduction");

        result.Should().Be(TransactionType.HoldDeduction);
    }

    [Fact]
    public void ConvertToProvider_WhenValueIsHoldDeduction_WritesCurrentName()
    {
        var result = _converter.ConvertToProviderExpression.Compile()(TransactionType.HoldDeduction);

        result.Should().Be("HoldDeduction");
    }

    [Fact]
    public void ConvertFromProvider_WhenValueIsUnsupported_ReturnsUnknown()
    {
        var result = _converter.ConvertFromProviderExpression.Compile()("LegacyUnexpectedType");

        result.Should().Be(TransactionType.Unknown);
    }
}
