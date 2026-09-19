namespace Application.Abstractions;

public interface ILlmCategoryClassifier
{
    // Returns null when it can't confidently pick one of the given categories.
    Task<Guid?> ClassifyAsync(
        string lineItemDescription,
        string? merchantName,
        IReadOnlyList<CategoryOption> availableCategories,
        CancellationToken cancellationToken = default);
}

public record CategoryOption(Guid Id, string Name);
