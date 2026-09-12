namespace Infrastructure.Storage;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public required string RootPath { get; set; }
}
