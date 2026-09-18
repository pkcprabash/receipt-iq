using Domain.Entities;

namespace Domain.Tests;

public class SystemCategoriesTests
{
    [Fact]
    public void All_HasNoDuplicateIdsOrNames()
    {
        Assert.Equal(SystemCategories.All.Count, SystemCategories.All.Select(c => c.Id).Distinct().Count());
        Assert.Equal(SystemCategories.All.Count, SystemCategories.All.Select(c => c.Name).Distinct().Count());
    }

    [Fact]
    public void All_IncludesUncategorizedFallback()
    {
        Assert.Contains(SystemCategories.All, c => c.Id == SystemCategories.UncategorizedId && c.Name == "Uncategorized");
    }
}
