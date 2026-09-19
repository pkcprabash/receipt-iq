using Application.Abstractions;

namespace Infrastructure.Categorization;

// Used when no OpenAI API key is configured — items the rule engine misses just
// fall through to Uncategorized rather than the pipeline failing.
public class NoOpCategoryClassifier : ILlmCategoryClassifier
{
    public Task<Guid?> ClassifyAsync(
        string lineItemDescription,
        string? merchantName,
        IReadOnlyList<CategoryOption> availableCategories,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(null);
}
