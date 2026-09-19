using Application.Categorization;
using Domain.Entities;

namespace Infrastructure.Tests;

public class CategoryRuleEngineTests
{
    [Fact]
    public void TryMatch_NoRules_ReturnsNull()
    {
        Assert.Null(CategoryRuleEngine.TryMatch([], Guid.NewGuid(), "Milk"));
    }

    [Fact]
    public void TryMatch_MerchantRule_TakesPriorityOverKeywordRule()
    {
        var merchantId = Guid.NewGuid();
        var merchantCategoryId = Guid.NewGuid();
        var keywordCategoryId = Guid.NewGuid();

        var rules = new List<CategoryRule>
        {
            new() { CategoryId = merchantCategoryId, MerchantId = merchantId },
            new() { CategoryId = keywordCategoryId, Keyword = "milk" }
        };

        Assert.Equal(merchantCategoryId, CategoryRuleEngine.TryMatch(rules, merchantId, "Whole Milk"));
    }

    [Fact]
    public void TryMatch_NoMerchantRule_FallsBackToKeywordMatch()
    {
        var keywordCategoryId = Guid.NewGuid();
        var rules = new List<CategoryRule> { new() { CategoryId = keywordCategoryId, Keyword = "milk" } };

        Assert.Equal(keywordCategoryId, CategoryRuleEngine.TryMatch(rules, Guid.NewGuid(), "Whole Milk 2%"));
    }

    [Fact]
    public void TryMatch_KeywordMatchIsCaseInsensitive()
    {
        var categoryId = Guid.NewGuid();
        var rules = new List<CategoryRule> { new() { CategoryId = categoryId, Keyword = "MILK" } };

        Assert.Equal(categoryId, CategoryRuleEngine.TryMatch(rules, null, "organic milk"));
    }

    [Fact]
    public void TryMatch_UserScopedRuleBeatsSystemWideRuleForSameMerchant()
    {
        var merchantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var systemCategoryId = Guid.NewGuid();
        var userCategoryId = Guid.NewGuid();

        var rules = new List<CategoryRule>
        {
            new() { CategoryId = systemCategoryId, MerchantId = merchantId, UserId = null },
            new() { CategoryId = userCategoryId, MerchantId = merchantId, UserId = userId }
        };

        Assert.Equal(userCategoryId, CategoryRuleEngine.TryMatch(rules, merchantId, "Anything"));
    }

    [Fact]
    public void TryMatch_NoMatch_ReturnsNull()
    {
        var rules = new List<CategoryRule> { new() { CategoryId = Guid.NewGuid(), Keyword = "bread" } };

        Assert.Null(CategoryRuleEngine.TryMatch(rules, Guid.NewGuid(), "Milk"));
    }
}
