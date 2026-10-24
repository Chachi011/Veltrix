using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class StaticPageConfiguration : IEntityTypeConfiguration<StaticPage>
{
    public void Configure(EntityTypeBuilder<StaticPage> builder)
    {
        builder.ToTable("StaticPages", t =>
            t.HasCheckConstraint("CK_StaticPages_Language", "[Language] IN ('en', 'ka')"));

        builder.Property(x => x.Slug).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Content).IsRequired();

        // The same page exists once per language
        builder.HasIndex(x => new { x.Slug, x.Language }).IsUnique();

        builder.HasData(
            new StaticPage
            {
                Id = 1,
                Slug = "rules",
                Language = "en",
                Title = "Crash game rules",
                Content = """
                    1. During **betting** you place a bet with virtual VLX points.
                    2. The round starts and the multiplier grows from **1.00x**.
                    3. Press **Cash out** at any moment: you get *bet × current multiplier*.
                    4. If the round **crashes** before you cash out, the bet is lost.
                    5. The crash point is decided **before** the round starts. Check it on the Provably Fair page.

                    VLX points have no monetary value and cannot be bought or withdrawn.
                    """,
                UpdatedAt = SeedData.CreatedAt
            },
            new StaticPage
            {
                Id = 2,
                Slug = "no-winning-strategy",
                Language = "en",
                Title = "Why there is no winning strategy",
                Content = """
                    Every round's crash point is random and independent of previous rounds.
                    A long series of low crashes does **not** make a high crash more likely.

                    The game has a built-in house edge, so over many rounds **every** strategy
                    (martingale, always 2x, waiting for "patterns") loses points on average.

                    Play for fun, use the limits and the session reminder, and take breaks.
                    """,
                UpdatedAt = SeedData.CreatedAt
            });
    }
}
