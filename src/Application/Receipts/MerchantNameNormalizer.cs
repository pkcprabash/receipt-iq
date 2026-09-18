using System.Globalization;
using System.Text.RegularExpressions;

namespace Application.Receipts;

// OCR merchant names are noisy — "WALMART SUPERCENTER #1234", "Trader Joe's LLC" — so
// this collapses them to a canonical form before matching/creating a Merchant row.
public static partial class MerchantNameNormalizer
{
    public static string Normalize(string rawName)
    {
        var name = rawName.Trim();
        if (name.Length == 0)
        {
            return name;
        }

        name = TrailingStoreNumberPattern().Replace(name, string.Empty);
        name = TrailingLegalSuffixPattern().Replace(name, string.Empty);
        name = WhitespacePattern().Replace(name, " ").Trim();

        if (IsAllUpper(name))
        {
            name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.ToLowerInvariant());
            // ToTitleCase treats "'" as a word boundary, so "Joe's" becomes "Joe'S" — fix the common possessive case back.
            name = TrailingPossessiveSPattern().Replace(name, "'s");
        }

        return name;
    }

    private static bool IsAllUpper(string value) =>
        value.Any(char.IsLetter) && value.Where(char.IsLetter).All(char.IsUpper);

    [GeneratedRegex(@"(?:\s*-?\s*(?:STORE\s*)?#\s*\d+|\s+STORE\s+\d+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingStoreNumberPattern();

    [GeneratedRegex(@"[,\s]+(?:L\.?L\.?C\.?|INC\.?|CORP(?:ORATION)?\.?|CO\.?|LTD\.?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingLegalSuffixPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"'S\b")]
    private static partial Regex TrailingPossessiveSPattern();
}
