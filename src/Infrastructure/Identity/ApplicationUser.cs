using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

// Auth identity only. Domain.Entities.User (same Id) holds the app-facing profile.
public class ApplicationUser : IdentityUser<Guid>
{
}
