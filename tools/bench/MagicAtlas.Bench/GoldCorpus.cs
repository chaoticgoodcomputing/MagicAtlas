using System.Text.Json.Nodes;

namespace MagicAtlas.Bench;

/// <summary>
/// The hand-parsed gold corpus, indexed by the card's full oracle name
/// (<c>Input.Name</c> in each fixture) → the fixture's gold AST abilities array
/// (<c>Output.Oracle.Abilities</c>). This is the eval corpus the combo-recall bench scopes to: a
/// combo is eligible iff <b>every</b> constituent card resolves here. Reading the committed gold AST
/// directly (rather than re-parsing oracle text) keeps the bench fully offline and independent of the
/// parser's current coverage — the engine is measured over the same trusted ASTs the MAST tests use.
/// </summary>
public sealed class GoldCorpus
{
  private readonly IReadOnlyDictionary<string, JsonArray> _abilitiesByName;
  private readonly IReadOnlyDictionary<string, JsonArray> _manaCostByName;

  private GoldCorpus(
    IReadOnlyDictionary<string, JsonArray> abilitiesByName,
    IReadOnlyDictionary<string, JsonArray> manaCostByName
  )
  {
    _abilitiesByName = abilitiesByName;
    _manaCostByName = manaCostByName;
  }

  /// <summary>The distinct card names present in the corpus.</summary>
  public IReadOnlyCollection<string> CardNames => _abilitiesByName.Keys.ToList();

  public int Count => _abilitiesByName.Count;

  public bool Contains(string cardName) => _abilitiesByName.ContainsKey(cardName);

  /// <summary>The gold AST abilities array for a card, or <c>null</c> if absent.</summary>
  public JsonArray? AbilitiesFor(string cardName) =>
    _abilitiesByName.TryGetValue(cardName, out var ab) ? ab : null;

  /// <summary>The card's printed mana-cost symbols (the manaCost attribute's <c>Symbols</c>), or
  /// <c>null</c>. The recast <c>pay:mana</c> co-cost source for the cast-from-graveyard arm — Gravecrawler
  /// is cast from the graveyard for its own mana cost (CR 601.3e).</summary>
  public JsonArray? ManaCostSymbolsFor(string cardName) =>
    _manaCostByName.TryGetValue(cardName, out var mc) ? mc : null;

  /// <summary>
  /// Load every <c>*.json</c> under <paramref name="fixturesRoot"/> (recursively), keying each by its
  /// <c>Input.Name</c>. DETERMINISTIC: files are visited in ordinal-sorted path order and a duplicate
  /// card name (the same card hand-parsed in several sets — e.g. Mana Leak in 2X2 and M10) resolves to
  /// the FIRST path lexicographically, so the chosen fixture never depends on filesystem enumeration
  /// order. Fixtures that don't parse as the expected shape are skipped silently (a malformed fixture
  /// is not a combo-recall signal).
  /// </summary>
  public static GoldCorpus Load(string fixturesRoot)
  {
    var byName = new Dictionary<string, JsonArray>(StringComparer.Ordinal);
    var manaByName = new Dictionary<string, JsonArray>(StringComparer.Ordinal);
    var files = Directory
      .EnumerateFiles(fixturesRoot, "*.json", SearchOption.AllDirectories)
      .OrderBy(p => p, StringComparer.Ordinal);

    foreach (var file in files)
    {
      JsonNode? root;
      try
      {
        root = JsonNode.Parse(File.ReadAllText(file));
      }
      catch
      {
        continue;
      }

      var name = root?["Input"]?["Name"]?.GetValue<string>();
      if (string.IsNullOrEmpty(name) || byName.ContainsKey(name))
        continue; // first-path-wins keeps the choice deterministic across duplicate-name fixtures

      if (root?["Output"]?["Oracle"]?["Abilities"] is JsonArray abilities)
      {
        byName[name] = (JsonArray)abilities.DeepClone(); // detach from the parsed document
        if (
          (root?["Output"]?["Attributes"] as JsonArray)
            ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "manaCost")
            ?["Symbols"]
          is JsonArray symbols
        )
          manaByName[name] = (JsonArray)symbols.DeepClone();
      }
    }

    return new GoldCorpus(byName, manaByName);
  }
}
