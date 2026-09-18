using Application.Receipts;

namespace Infrastructure.Tests;

public class MerchantNameNormalizerTests
{
    [Theory]
    [InlineData("WALMART #4521", "Walmart")]
    [InlineData("COSTCO WHOLESALE #123", "Costco Wholesale")]
    [InlineData("TARGET STORE #567", "Target")]
    [InlineData("TRADER JOE'S LLC", "Trader Joe's")]
    [InlineData("ACME CORP.", "Acme")]
    [InlineData("ACME, INC", "Acme")]
    [InlineData("BEST BUY   CO.", "Best Buy")]
    [InlineData("Trader Joe's", "Trader Joe's")]
    [InlineData("  WALMART  ", "Walmart")]
    public void Normalize_ProducesCanonicalForm(string raw, string expected)
    {
        Assert.Equal(expected, MerchantNameNormalizer.Normalize(raw));
    }

    [Fact]
    public void Normalize_DoesNotStripLegitimateNameEndingInLegalSuffixLookingSubstring()
    {
        // "Costco" ends in "co" but has no preceding whitespace/comma before it, so it must survive.
        Assert.Equal("Costco", MerchantNameNormalizer.Normalize("COSTCO"));
    }
}
