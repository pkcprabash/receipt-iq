namespace Application.Abstractions;

public interface IJwtTokenGenerator
{
    AuthToken GenerateToken(Guid userId, string email);
}

public record AuthToken(string Value, DateTime ExpiresAtUtc);
