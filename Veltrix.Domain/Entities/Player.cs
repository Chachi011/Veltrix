using System;
using System.Collections.Generic;
using System.Text;
using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities
{
    public class Player
    {
        public Guid Id { get; set; }
        public Guid OperatorId { get; set; }
        public string ExternalPlayerId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public PlayerStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
