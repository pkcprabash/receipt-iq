namespace Infrastructure.Categorization;

public class OpenAiClassifierOptions
{
    public const string SectionName = "OpenAiClassifier";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
}
