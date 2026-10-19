using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Game> Games => Set<Game>();
        public DbSet<Operator> Operators => Set<Operator>();
        public DbSet<Player> Players => Set<Player>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<GameRound> GameRounds => Set<GameRound>();
        public DbSet<Bet> Bets => Set<Bet>();
        public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Game
            modelBuilder.Entity<Game>()
                .Property(x => x.MinBet)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Game>()
                .Property(x => x.MaxBet)
                .HasPrecision(18, 2);

            // GameRound
            modelBuilder.Entity<GameRound>()
                .Property(x => x.CrashPoint)
                .HasPrecision(18, 4);

            modelBuilder.Entity<GameRound>()
                .Property(x => x.CurrentMultiplier)
                .HasPrecision(18, 4);

            // Bet
            modelBuilder.Entity<Bet>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Bet>()
                .Property(x => x.CashoutMultiplier)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Bet>()
                .Property(x => x.WinAmount)
                .HasPrecision(18, 2);

            // Wallet
            modelBuilder.Entity<Wallet>()
                .Property(x => x.Balance)
                .HasPrecision(18, 2);

            // WalletTransaction
            modelBuilder.Entity<WalletTransaction>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<WalletTransaction>()
                .Property(x => x.BalanceBefore)
                .HasPrecision(18, 2);

            modelBuilder.Entity<WalletTransaction>()
                .Property(x => x.BalanceAfter)
                .HasPrecision(18, 2);
        }

    }
}
