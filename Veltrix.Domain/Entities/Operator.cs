using System;
using System.Collections.Generic;
using System.Text;
using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities
{
    public class Operator
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string? CallbackUrl { get; set; }
        public OperatorStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
