using System.Text.RegularExpressions;

namespace Veltrix.Tests.Api;

/// <summary>
/// database/veltrix.sql is the delivered "schema + seed in one script" (generated from the migrations, plus the test
/// accounts). It is a copy, so it can go stale: this fails when a migration is added without regenerating the script.
/// </summary>
public class DatabaseScriptTests
{
    private static readonly string Root = FindRepositoryRoot();

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Veltrix.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repository root (Veltrix.slnx) not found.");
    }

    [Fact]
    public void The_SQL_script_contains_every_migration()
    {
        var script = File.ReadAllText(Path.Combine(Root, "database", "veltrix.sql"));
        var migrations = Directory
            .EnumerateFiles(Path.Combine(Root, "Veltrix.Infrastructure", "Persistence", "Migrations"), "*.cs")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => Regex.IsMatch(name!, @"^\d{14}_\w+$"))
            .ToList();

        Assert.NotEmpty(migrations);
        var missing = migrations.Where(id => !script.Contains($"N'{id}'")).ToList();

        Assert.True(missing.Count == 0,
            "database/veltrix.sql is missing migrations (regenerate it, see the script header):\n" + string.Join("\n", missing));
    }

    [Fact]
    public void The_SQL_script_creates_one_test_account_per_role()
    {
        var script = File.ReadAllText(Path.Combine(Root, "database", "veltrix.sql"));

        Assert.Contains("N'admin@veltrix.dev'", script);
        Assert.Contains("N'operator@veltrix.dev'", script);
        Assert.Contains("N'player1@veltrix.dev'", script);
        Assert.Contains("N'player2@veltrix.dev'", script);
        Assert.Contains("SET QUOTED_IDENTIFIER ON", script); // sqlcmd needs it for the filtered indexes
    }
}
