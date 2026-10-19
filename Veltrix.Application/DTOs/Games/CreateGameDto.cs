using System;
using System.Collections.Generic;
using System.Text;
using Veltrix.Application.DTOs.Games;
using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Games
{
    public class CreateGameDto
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public GameType Type { get; set; }
        public decimal MinBet { get; set; }
        public decimal MaxBet { get; set; }
        public double RTP { get; set; }
    }
}
