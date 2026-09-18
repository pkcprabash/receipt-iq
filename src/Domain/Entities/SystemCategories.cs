namespace Domain.Entities;

// Fixed IDs so migrations, seed data, and any future rule-engine references
// to e.g. "Uncategorized" stay stable across environments.
public static class SystemCategories
{
    private static readonly DateTime SeededAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly Guid GroceriesId = Guid.Parse("00000000-0000-0000-0001-000000000001");
    public static readonly Guid DiningId = Guid.Parse("00000000-0000-0000-0001-000000000002");
    public static readonly Guid EntertainmentId = Guid.Parse("00000000-0000-0000-0001-000000000003");
    public static readonly Guid HouseholdId = Guid.Parse("00000000-0000-0000-0001-000000000004");
    public static readonly Guid TransportationId = Guid.Parse("00000000-0000-0000-0001-000000000005");
    public static readonly Guid UtilitiesId = Guid.Parse("00000000-0000-0000-0001-000000000006");
    public static readonly Guid HealthId = Guid.Parse("00000000-0000-0000-0001-000000000007");
    public static readonly Guid ShoppingId = Guid.Parse("00000000-0000-0000-0001-000000000008");
    public static readonly Guid TravelId = Guid.Parse("00000000-0000-0000-0001-000000000009");
    public static readonly Guid UncategorizedId = Guid.Parse("00000000-0000-0000-0001-000000000010");

    public static IReadOnlyList<Category> All { get; } =
    [
        new Category { Id = GroceriesId, Name = "Groceries", CreatedAtUtc = SeededAtUtc },
        new Category { Id = DiningId, Name = "Dining & Restaurants", CreatedAtUtc = SeededAtUtc },
        new Category { Id = EntertainmentId, Name = "Entertainment", CreatedAtUtc = SeededAtUtc },
        new Category { Id = HouseholdId, Name = "Household", CreatedAtUtc = SeededAtUtc },
        new Category { Id = TransportationId, Name = "Transportation", CreatedAtUtc = SeededAtUtc },
        new Category { Id = UtilitiesId, Name = "Utilities", CreatedAtUtc = SeededAtUtc },
        new Category { Id = HealthId, Name = "Health & Wellness", CreatedAtUtc = SeededAtUtc },
        new Category { Id = ShoppingId, Name = "Shopping", CreatedAtUtc = SeededAtUtc },
        new Category { Id = TravelId, Name = "Travel", CreatedAtUtc = SeededAtUtc },
        new Category { Id = UncategorizedId, Name = "Uncategorized", CreatedAtUtc = SeededAtUtc }
    ];
}
