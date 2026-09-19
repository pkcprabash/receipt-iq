using Domain.Entities;

namespace Application.Categorization;

// Merchant match beats line-item keyword match; a user's own rule beats a system-wide
// one at each step. Returns null on a total miss — callers decide the fallback
// (LLM classifier, then Uncategorized).
public static class CategoryRuleEngine
{
    public static Guid? TryMatch(IReadOnlyList<CategoryRule> rules, Guid? merchantId, string lineItemDescription)
    {
        if (merchantId.HasValue)
        {
            var merchantRule = rules
                .Where(r => r.Keyword is null && r.MerchantId == merchantId)
                .OrderBy(r => r.UserId is null ? 1 : 0)
                .FirstOrDefault();

            if (merchantRule is not null)
            {
                return merchantRule.CategoryId;
            }
        }

        var keywordRule = rules
            .Where(r => r.Keyword is not null && lineItemDescription.Contains(r.Keyword, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.UserId is null ? 1 : 0)
            .FirstOrDefault();

        return keywordRule?.CategoryId;
    }
}
