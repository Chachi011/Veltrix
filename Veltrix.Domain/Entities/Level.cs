namespace Veltrix.Domain.Entities;

/// <summary>Reference table: how much XP each level needs.</summary>
public class Level
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public int XpRequired { get; set; }
}
