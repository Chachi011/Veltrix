namespace Veltrix.Domain.Entities;

public class Wallet
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Virtual VLX points. A DB CHECK constraint guarantees it is never negative.</summary>
    public decimal Balance { get; set; }

    public DateTime? LastDailyBonusAt { get; set; }

    /// <summary>
    /// Optimistic concurrency token. SQL Server changes it on every update, so if two requests
    /// modify the same wallet at the same time, the second one fails instead of overwriting the first.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public User? User { get; set; }
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
