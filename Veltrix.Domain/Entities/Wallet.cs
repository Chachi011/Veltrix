using System;
using System.Collections.Generic;
using System.Text;

namespace Veltrix.Domain.Entities
{
    public class Wallet
    {
        public Guid Id { get; set; }
        public Guid PlayerId { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "GEL";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
