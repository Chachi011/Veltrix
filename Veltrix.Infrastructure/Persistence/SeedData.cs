namespace Veltrix.Infrastructure.Persistence;

/// <summary>
/// HasData() seed values must be constant. If we used DateTime.UtcNow, every
/// "dotnet ef migrations add" would think the data changed and generate a new UPDATE.
/// </summary>
internal static class SeedData
{
    public static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
