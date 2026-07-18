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
  private readonly IReadOnlyDictionary<string, JsonObject> _profileByName;
  private readonly IReadOnlyDictionary<string, string> _fixturePathByName;

  private GoldCorpus(
    IReadOnlyDictionary<string, JsonArray> abilitiesByName,
    IReadOnlyDictionary<string, JsonArray> manaCostByName,
    IReadOnlyDictionary<string, JsonObject> profileByName,
    IReadOnlyDictionary<string, string> fixturePathByName
  )
  {
    _abilitiesByName = abilitiesByName;
    _manaCostByName = manaCostByName;
    _profileByName = profileByName;
    _fixturePathByName = fixturePathByName;
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

  /// <summary>The card's combat profile — <c>{ Types, Power, HasDefender }</c> — for the PortWalk
  /// combat-presence projection (a creature that can attack deals combat damage, CR 510). <c>null</c> for
  /// a card with no creatureStats/type line. Lets the combo-recall bench (the product reconstruction path)
  /// project the structural combat-damage emit; other callers omit it.</summary>
  public JsonObject? CardProfileFor(string cardName) =>
    _profileByName.TryGetValue(cardName, out var p) ? p : null;

  /// <summary>
  /// The card's fixture RELATIVE PATH under <c>Fixtures/HandParsedCards</c> (no extension, forward-slash
  /// separated, e.g. <c>"NPH/SuturePriest"</c>) — the SAME key <c>oracle-text-quarantine.json</c> uses
  /// (<see cref="QuarantineIndex"/>), enabling the <c>Input.Name</c> (display name, e.g. <c>"Suture
  /// Priest"</c>) → fixture-path join item R1 needs. <c>null</c> for a name absent from the corpus.
  /// </summary>
  public string? FixturePathFor(string cardName) =>
    _fixturePathByName.TryGetValue(cardName, out var p) ? p : null;

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
    var profileByName = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
    var fixturePathByName = new Dictionary<string, string>(StringComparer.Ordinal);
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

      // The fixture's relative path under fixturesRoot (no extension, forward-slash), matching the key
      // shape oracle-text-quarantine.json uses and CardTestCaseLoader's testCase.Name — captured
      // regardless of whether Output.Oracle.Abilities parses, so a quarantined-but-malformed fixture
      // still resolves for the FidelityRisk join.
      var relPath = Path.GetRelativePath(fixturesRoot, file);
      relPath = Path.ChangeExtension(relPath, null)!;
      relPath = relPath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
      fixturePathByName[name] = relPath;

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

        // Combat profile (for the PortWalk combat-presence emit): card types + printed power. A fixed
        // creatureStats power is read as an int; a variable/absent power is left null (conservatively
        // "could attack"). Defender is not threaded — a gated combat-presence emit floors to Amber anyway,
        // so a rare >0-power Defender is a harmless over-approximation (never a false GREEN).
        if (root?["Output"]?["TypeLine"]?["Types"] is JsonArray cardTypes && cardTypes.Count > 0)
        {
          var profile = new JsonObject
          {
            ["Types"] = new JsonArray(
              cardTypes.Select(t => (JsonNode)t!.ToString().ToLowerInvariant()).ToArray()
            ),
          };
          var power = (root["Output"]["Attributes"] as JsonArray)
            ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "creatureStats")
            ?["Power"]?["Value"];
          if (power is not null && power.GetValueKind() == System.Text.Json.JsonValueKind.Number)
            profile["Power"] = power.GetValue<int>();
          profileByName[name] = profile;
        }
      }
    }

    return new GoldCorpus(byName, manaByName, profileByName, fixturePathByName);
  }
}
