using System.Text.Json;
using System.Text.RegularExpressions;

namespace Veltrix.Tests.Api;

/// <summary>
/// The API answers in English; the Angular client shows its messages in Georgian using the "server" section of
/// Veltrix.Web/src/i18n/ka.json (the one Georgian file). These tests keep the two in step: every user-facing message
/// in the C# code has a Georgian version, and ka.json has no messages the code no longer sends.
/// A changing part of a message ({amount}, {date}, ...) is compared as "{}".
/// </summary>
public class GeorgianTranslationTests
{
    /// <summary>Messages that never reach a player's screen (startup configuration errors).</summary>
    private static readonly HashSet<string> NotUserFacing = new()
    {
        "Connection string 'DefaultConnection' is missing.",
        // Domain invariants that only a programming error can break
        "The level table has no level for 0 XP.",
        "XP cannot be taken away.",
        "Achievement {} pays VLX, so the wallet must be loaded."
    };

    /// <summary>
    /// Texts the server sends that the patterns below cannot find: ledger reasons, the system actor, the level titles
    /// and the seeded game description (database seed), messages chosen with "? :" and messages built from two
    /// concatenated strings.
    /// </summary>
    private static readonly string[] KnownServerTexts =
    {
        "Welcome balance", "Bet on round #{}", "Cash-out at {}x on round #{}", "Auto cash-out at {}x", "Round #{} cancelled",
        "System", "The multiplier grows from 1.00x. Cash out before it crashes!",
        "Server restarted during the round", "Cancelled by operator",
        // Achievements: the ledger reason and the seeded titles / descriptions of the active ones
        "Achievement: {}",
        "Try Crash", "Play your first round of Crash.", "First cash-out", "Cash out before the crash for the first time.",
        "Plan ahead", "Place a bet with an auto cash-out target.", "Know the rules",
        "Read the game rules and why Crash has no winning strategy.", "Check a round",
        "Verify a finished round on the Provably fair page.", "Set your limit", "Set a daily bet limit on the Responsible play page.",
        // Old titles (renamed or retired achievements): still named by old ledger rows and notifications
        "First bet", "Set and forget", "To the moon", "Regular", "Big win", "Sharp eye", "Rookie", "Spotter", "Climber", "Pilot", "Navigator", "Ace", "Veteran", "Elite", "Legend", "Mythic",
        "Daily bonus available",
        "Your daily bonus of {} VLX is ready. Claim it on the wallet page.",
        "Your account was blocked",
        "An administrator blocked your account.",
        "An administrator blocked your account. Reason: {}",
        "An administrator added {} VLX. Reason: {}. New balance: {} VLX.",
        "An administrator removed {} VLX. Reason: {}. New balance: {} VLX.",
        "Betting is paused until {} UTC. You can still sign in and see your history. A self-exclusion cannot be cancelled or shortened.",
        "An unexpected error occurred.",
        "The request body is too large."
    };

    private static readonly string Root = FindRepositoryRoot();

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Veltrix.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repository root (Veltrix.slnx) not found.");
    }

    private static readonly Regex Placeholder = new(@"\{[^{}]*\}", RegexOptions.Compiled);

    private static string Normalize(string text) => Placeholder.Replace(text, "{}");

    /// <summary>
    /// String literals passed to an exception, to WithMessage(...), to a notification, as a ledger reason or as a
    /// ProblemDetails detail. Interpolated literals ($"...") keep "{}" where a value goes.
    /// </summary>
    private static HashSet<string> MessagesInCode()
    {
        var literal = @"(\$?)""((?:[^""\\]|\\.)*)""";
        var patterns = new[]
        {
            new Regex(@"new\s+\w*Exception\(\s*" + literal),
            new Regex(@"WithMessage\(\s*" + literal),
            new Regex(@":\s*base\(\s*" + literal),                       // custom exception classes
            new Regex(@"base\(\s*\d{3},\s*""[^""]*"",\s*" + literal),     // AppException(status, title, message)
            new Regex(@"const string \w*[Mm]essage\s*=\s*" + literal),
            new Regex(@"UserNotifications\.Add\([^""]*" + literal + @",\s*\r?\n?\s*" + literal),
            new Regex(@"Notification\.Create\([^""]*" + literal),
            new Regex(@"WriteProblemAsync\([^""]*""[^""]*"",\s*\r?\n?\s*" + literal),
            new Regex(@"Create\(StatusCodes\.Status\d+\w*,\s*""[^""]*"",\s*\r?\n?\s*" + literal)
        };

        var messages = new HashSet<string>();
        foreach (var project in new[] { "Veltrix.Domain", "Veltrix.Application", "Veltrix.Infrastructure", "Veltrix.API" })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(Root, project), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                    file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                    file.Contains("Migrations"))
                    continue;

                var source = File.ReadAllText(file);
                foreach (var pattern in patterns)
                {
                    foreach (Match match in pattern.Matches(source))
                    {
                        // A pattern may capture two literals (e.g. a notification's title and message)
                        for (var group = 2; group < match.Groups.Count; group += 2)
                        {
                            if (match.Groups[group].Success && match.Groups[group].Value.Length > 0)
                                messages.Add(Normalize(match.Groups[group].Value.Replace("\\\"", "\"")));
                        }
                    }
                }
            }
        }

        messages.ExceptWith(NotUserFacing);
        // "First part " + "second part": only the first half was captured; the whole message is in KnownServerTexts
        messages.RemoveWhere(m => m.EndsWith(' '));
        messages.UnionWith(KnownServerTexts);
        return messages;
    }

    private static JsonElement GeorgianFile() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Veltrix.Web", "src", "i18n", "ka.json"))).RootElement;

    private static HashSet<string> Keys(JsonElement section) =>
        section.EnumerateObject().Select(p => Normalize(p.Name)).ToHashSet();

    [Fact]
    public void Every_user_facing_server_message_has_a_Georgian_version()
    {
        var file = GeorgianFile();
        var translated = Keys(file.GetProperty("server"));
        translated.UnionWith(Keys(file.GetProperty("ui"))); // a few texts are shared with the UI ("Balance adjusted")

        var missing = MessagesInCode().Where(m => !translated.Contains(m)).OrderBy(m => m).ToList();

        Assert.True(missing.Count == 0, "No Georgian in ka.json \"server\" for:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void The_server_section_has_no_message_the_code_no_longer_sends()
    {
        var inCode = MessagesInCode();

        var stale = Keys(GeorgianFile().GetProperty("server")).Where(k => !inCode.Contains(k)).OrderBy(k => k).ToList();

        Assert.True(stale.Count == 0, "ka.json \"server\" has messages the code no longer sends:\n" + string.Join("\n", stale));
    }

    [Fact]
    public void Every_placeholder_of_a_server_message_is_used_in_its_Georgian_version()
    {
        foreach (var entry in GeorgianFile().GetProperty("server").EnumerateObject())
        {
            var english = Placeholder.Matches(entry.Name).Select(m => m.Value).Order();
            var georgian = Placeholder.Matches(entry.Value.GetString()!).Select(m => m.Value).Order();
            Assert.Equal(english, georgian);
        }
    }
}
