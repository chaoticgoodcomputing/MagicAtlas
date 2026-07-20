namespace MagicAST.Interaction.Tests;

using System.Text.Json.Nodes;

/// <summary>
/// The interaction-pipeline <b>sentinel set</b> — <i>derived</i>, never listed.
///
/// <para>This replaces <c>Snapshots/sentinels.json</c>, a hand-curated manifest of "which cards and
/// combos get a pipeline snapshot". That manifest was a human's coverage judgment about the pipeline,
/// which ADR-0004 §1 classifies as neither Evidence nor Derived: it recorded no primary observation
/// about Magic, and nothing recomputed it, so it could only rot. The governing rule for this milestone
/// is that hand-authored input is confined to the MAST loop's own fixtures — parse golds under
/// <c>Fixtures/HandParsedCards/</c> and interaction golds under <c>Fixtures/Interactions/golds/</c>.
/// A snapshot manifest is not one of those.</para>
///
/// <para><b>The selection rule.</b> <i>One sentinel per interaction gold.</i> The gold's <c>id</c> is
/// the sentinel name (it is already a slug, so the snapshot filename is the gold's own identity), and
/// the sentinel's cards are the gold's <c>cards</c> filtered to those that resolve to a hand-parsed gold
/// AST. A gold naming no resolvable card contributes no sentinel.</para>
///
/// <para><b>Why this rule and not the alternatives.</b> The requirement is stability: a selection rule
/// that moves for reasons unrelated to the thing being guarded re-creates, in a new place, exactly the
/// drift this milestone exists to remove.</para>
/// <list type="bullet">
///   <item><i>Top-N by popularity</i> — rejected outright. It keys on the Scryfall/CSB corpus, so an
///     unrelated corpus refresh silently renames and churns the whole snapshot family.</item>
///   <item><i>Greedy family set-cover over the parse golds</i> — deterministic but not stable: adding
///     one unrelated parse gold can flip which card covers a family, renaming many snapshots.</item>
///   <item><i>Every parse gold</i> — maximally stable, but it mirrors ~1,600 golds (and grows with every
///     MAST batch) into a committed snapshot family, making a snapshot regen a mandatory step of every
///     parse batch. The card-level coverage it would buy is already reported corpus-wide by the
///     reporting layer (<c>card-ports.json</c>, <c>port-label-census.json</c>, <c>port-nodes.json</c>).</item>
///   <item><i>Every interaction gold</i> — chosen. The interaction golds are the loop's own record of
///     which interactions have been witnessed and judged, so the guard pins precisely the interactions
///     the project claims to understand. The set moves only when the loop lands an interaction gold: no
///     corpus, rank, or count is an input, and adding a parse gold cannot rename or remove a
///     snapshot.</item>
/// </list>
///
/// <para>Card name → gold-AST path resolution is itself derived (an index over the parse golds' own
/// <c>Output.Name</c>), with an ordinal-least-path tie-break so a card carrying more than one printing's
/// gold resolves deterministically. Gold <c>cards</c> entries that name a non-card port owner
/// ("Treasure", "Equipped Creature", "Snake") simply do not resolve and are dropped — their ports arrive
/// through token resolution on the real card anyway.</para>
/// </summary>
public static class SentinelSet
{
  public sealed record CardRef
  {
    /// <summary>Path to the parse gold, relative to <c>tests/magic-ast-tests/Fixtures</c>.</summary>
    public required string Path { get; init; }

    /// <summary>The card's name, as the parse gold itself records it.</summary>
    public required string Card { get; init; }
  }

  public sealed record Sentinel
  {
    /// <summary>The interaction gold's <c>id</c> — already a slug, and the snapshot's filename stem.</summary>
    public required string Name { get; init; }

    /// <summary>The interaction gold's <c>unit</c>: "single-card", "combo" or "pairwise".</summary>
    public required string Unit { get; init; }

    public required IReadOnlyList<CardRef> Cards { get; init; }

    /// <summary>True when the sentinel exercises the inter-card half of the pipeline.</summary>
    public bool IsMultiCard => Cards.Count > 1;

    public override string ToString() => $"{Unit}:{Name}";
  }

  public static string RepoRoot()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "nx.json")))
      dir = dir.Parent;
    return dir?.FullName
      ?? throw new InvalidOperationException("Could not locate repo root (no nx.json above test dir).");
  }

  public static string FixturesDir() =>
    Path.Combine(RepoRoot(), "tests", "magic-ast-tests", "Fixtures");

  public static string GoldsDir() =>
    Path.Combine(FixturesDir(), "Interactions", "golds");

  public static string ParseGoldsDir() => Path.Combine(FixturesDir(), "HandParsedCards");

  /// <summary>Card name → parse-gold path (relative to Fixtures/, forward-slashed). Built by reading
  /// every parse gold's own recorded name; ties broken by ordinal-least path so the mapping is a pure
  /// function of the fixture set.</summary>
  public static IReadOnlyDictionary<string, string> ParseGoldIndex()
  {
    var fixtures = FixturesDir();
    var index = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (
      var file in Directory
        .EnumerateFiles(ParseGoldsDir(), "*.json", SearchOption.AllDirectories)
        .OrderBy(f => f, StringComparer.Ordinal)
    )
    {
      string? name;
      try
      {
        name = JsonNode.Parse(File.ReadAllText(file))?["Output"]?["Name"]?.ToString();
      }
      catch (System.Text.Json.JsonException)
      {
        continue; // not a parse gold (or deliberately malformed); it cannot back a sentinel either way
      }
      if (string.IsNullOrEmpty(name))
        continue;
      var rel = Path.GetRelativePath(fixtures, file).Replace(Path.DirectorySeparatorChar, '/');
      // Ordinal-least path wins: deterministic when a card carries more than one printing's gold.
      if (!index.TryGetValue(name, out var existing) || string.CompareOrdinal(rel, existing) < 0)
        index[name] = rel;
    }
    return index;
  }

  /// <summary>The derived sentinel set, ordered by gold id.</summary>
  public static IReadOnlyList<Sentinel> Derive()
  {
    var index = ParseGoldIndex();
    var sentinels = new List<Sentinel>();
    foreach (
      var file in Directory
        .EnumerateFiles(GoldsDir(), "*.json", SearchOption.TopDirectoryOnly)
        .OrderBy(f => f, StringComparer.Ordinal)
    )
    {
      var gold = JsonNode.Parse(File.ReadAllText(file))!;
      var id = gold["id"]?.ToString();
      var unit = gold["unit"]?.ToString();
      if (string.IsNullOrEmpty(id) || gold["cards"] is not JsonArray cards)
        continue;

      var refs = cards
        .Select(c => c!.ToString())
        .Where(index.ContainsKey)
        .Distinct(StringComparer.Ordinal)
        .Select(c => new CardRef { Path = index[c], Card = c })
        .ToList();

      if (refs.Count == 0)
        continue;

      sentinels.Add(
        new Sentinel
        {
          Name = id,
          Unit = string.IsNullOrEmpty(unit) ? "single-card" : unit,
          Cards = refs,
        }
      );
    }
    return sentinels;
  }

  /// <summary>Load a parse gold's abilities + mana cost for projection.</summary>
  public static (JsonNode Abilities, JsonNode? ManaCost) LoadGold(string relativePath)
  {
    var gold = JsonNode.Parse(File.ReadAllText(Path.Combine(FixturesDir(), relativePath)))!;
    var manaCost = (gold["Output"]?["Attributes"] as JsonArray)
      ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "manaCost")
      ?["Symbols"];
    return (gold["Output"]!["Oracle"]!["Abilities"]!, manaCost);
  }
}
