using System.Text;
using Application.Abstractions;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Infrastructure.Categorization;

public class OpenAiCategoryClassifier : ILlmCategoryClassifier
{
    private const string NoneAnswer = "NONE";

    private readonly ChatClient _client;

    public OpenAiCategoryClassifier(IOptions<OpenAiClassifierOptions> options)
    {
        var value = options.Value;
        _client = new ChatClient(value.Model, value.ApiKey);
    }

    public async Task<Guid?> ClassifyAsync(
        string lineItemDescription,
        string? merchantName,
        IReadOnlyList<CategoryOption> availableCategories,
        CancellationToken cancellationToken = default)
    {
        if (availableCategories.Count == 0)
        {
            return null;
        }

        var messages = new ChatMessage[]
        {
            ChatMessage.CreateSystemMessage(BuildSystemPrompt(availableCategories)),
            ChatMessage.CreateUserMessage(BuildUserPrompt(lineItemDescription, merchantName))
        };

        var completion = await _client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        var answer = completion.Value.Content.Count > 0 ? completion.Value.Content[0].Text.Trim() : NoneAnswer;

        var match = availableCategories.FirstOrDefault(c => string.Equals(c.Name, answer, StringComparison.OrdinalIgnoreCase));
        return match?.Id;
    }

    private static string BuildSystemPrompt(IReadOnlyList<CategoryOption> availableCategories)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You categorize a single line item from a shopping receipt.");
        builder.AppendLine($"Respond with exactly one of these category names, or \"{NoneAnswer}\" if none fit:");
        foreach (var category in availableCategories)
        {
            builder.AppendLine($"- {category.Name}");
        }
        builder.AppendLine("Respond with the category name only, nothing else.");
        return builder.ToString();
    }

    private static string BuildUserPrompt(string lineItemDescription, string? merchantName) =>
        merchantName is null
            ? $"Item: {lineItemDescription}"
            : $"Merchant: {merchantName}\nItem: {lineItemDescription}";
}
