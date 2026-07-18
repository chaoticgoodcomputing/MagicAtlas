namespace MagicAST.Tests.Tests;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Keywords;
using MagicAST.Parsing;
using MagicAST.Parsing.Parsers;
using MagicAST.Tests.Infrastructure;
using MagicAtlas.Ast.Tests.Flows.SpanWitness.Steps;

/// <summary>
/// Self-verifying coverage check for <see cref="SpanWitnessStep"/>'s hand-curated
/// <c>AnchorsFor</c> alias table (design review 2026-07-18). <c>AnchorsFor</c> maps a port's
/// (role, kind) label to the oracle-text word(s) its span must contain; for most families that
/// word IS the mechanism's own vocabulary ("sacrifice" → sac, "enters" → etb), but a KEYWORD
/// mechanic often mints a port whose label bears no textual resemblance to the keyword's own
/// printed name (Firebending mints <c>emit:mana</c>; Modular mints an <c>ltb</c> dies-trigger;
/// Embalm mints <c>emit:token</c>) — <c>AnchorsFor</c> folds in an explicit alias for exactly
/// these so the keyword's own port isn't flagged as an unexplained "semantic suspect".
///
/// <para>
/// This is a hand-curated mapping (a defect-of-drift per the governing principle: anything
/// hand-rolled that can silently drift from reality must be made self-*verifying*). This suite
/// makes the gap self-verifying two ways:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Keyword_MintedPorts_AreAnchorCovered"/> — for every keyword
///   with a canonical-printed-form gold (<c>Fixtures/KeywordExpansions/*.json</c>, the same data
///   <see cref="KeywordExpansionTests"/> already trusts), runs the REAL production pipeline
///   (<see cref="OracleParser"/> → <see cref="PortWalk"/>, exactly <c>CardAtlasShared.Project</c>'s
///   parse→project idiom) over that printed form and asserts every checkable minted port label is
///   covered by the keyword's own name or an explicit <c>AnchorsFor</c> alias. Going through the
///   full parser (not just the keyword's own <see cref="IKeyword.Combinator"/> in isolation) is
///   load-bearing: some keywords (Modular, Dash, Reconfigure, …) decompose into MORE abilities via
///   a higher-priority <c>[StaticRule]</c> that supersedes the bare combinator (see
///   <c>ModularStaticRule</c>) — probing the combinator alone would silently under-report what the
///   keyword actually mints on a real card.</description></item>
///   <item><description><see cref="EveryKeywordType_IsCoveredOrADocumentedGap"/> — reflects every
///   <c>[Keyword]</c>-attributed type (mirroring <see cref="RuleRegistry.Discover{TRule,TAttr}"/>,
///   the same discovery <see cref="KeywordRegistry"/> uses) and asserts the set that lacks a
///   printed-form gold is EXACTLY <see cref="KnownAnchorCoverageGaps"/> — named, not silent. A new
///   keyword with no fixture fails this test until it is either given a fixture (promoting it into
///   the real check above) or explicitly added to the gap list with a reason; a stale whitelist
///   entry (fixture added, keyword deleted) fails it the other way.</description></item>
/// </list>
/// </summary>
[TestFixture]
public class KeywordAnchorCoverageTests
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static readonly OracleParser Parser = new();

  private static readonly MethodInfo AnchorsForMethod =
    typeof(SpanWitnessStep).GetMethod("AnchorsFor", BindingFlags.NonPublic | BindingFlags.Static)
    ?? throw new InvalidOperationException(
      "SpanWitnessStep.AnchorsFor was not found by reflection — has it been renamed or moved? "
        + "Update KeywordAnchorCoverageTests's reflection lookup to match."
    );

  /// <summary>Invokes the REAL, private <c>SpanWitnessStep.AnchorsFor</c> — never a re-implemented
  /// copy, so this suite can never itself drift from the production anchor table.</summary>
  private static string[]? AnchorsFor(string label) =>
    (string[]?)AnchorsForMethod.Invoke(null, [label]);

  /// <summary>Strips the "Keyword" suffix every <c>[Keyword]</c> type carries by convention
  /// (<c>FirebendingKeyword</c> → <c>Firebending</c>), matching <c>Fixtures/KeywordExpansions/</c>
  /// filenames (see <see cref="KeywordExpansionTestCase.Name"/>).</summary>
  private static string StemOf(string typeName) =>
    typeName.EndsWith("Keyword", StringComparison.Ordinal) ? typeName[..^"Keyword".Length] : typeName;

  /// <summary>
  /// KNOWN, NAMED gap list — keyword types with no <c>Fixtures/KeywordExpansions/*.json</c>
  /// canonical-printed-form gold, so <see cref="Keyword_MintedPorts_AreAnchorCovered"/> has no
  /// representative text to feed the production parser and cannot statically determine what ports
  /// they mint. NOT a silent skip: <see cref="EveryKeywordType_IsCoveredOrADocumentedGap"/> asserts
  /// this set is EXACTLY the keyword types lacking a fixture — add a fixture to lift a keyword out
  /// of this list into real anchor-coverage checking (the preferred fix), or update this list's
  /// membership if the gap is triaged some other way. Never delete an entry to silence a failure
  /// without addressing why it's here.
  /// </summary>
  private static readonly IReadOnlySet<string> KnownAnchorCoverageGaps = new HashSet<string>(
    StringComparer.Ordinal
  )
  {
    "Aftermath",
    "Amplify",
    "Awaken",
    "Backup",
    "Bargain",
    "Cascade",
    "Casualty",
    "Champion",
    "Cipher",
    "Cleave",
    "Compleat",
    "Craft",
    "Crew",
    "CumulativeUpkeep",
    "Devour",
    "Disguise",
    "Disturb",
    "Emerge",
    "Encore",
    "EquipNonManaCost",
    "EquipQuality",
    "Escape",
    "Extort",
    "Fabricate",
    "Fading",
    "Foretell",
    "ForMirrodin",
    "Gift",
    "Graft",
    "Harmonize",
    "Haunt",
    "Hideaway",
    "Impending",
    "JobSelect",
    "JumpStart",
    "LivingWeapon",
    "Megamorph",
    "Miracle",
    "Mobilize",
    "Morph",
    "Mutate",
    "Myriad",
    "Plot",
    "Prepared",
    "Prototype",
    "Provoke",
    "Rebound",
    "Recover",
    "Reinforce",
    "Renown",
    "Retrace",
    "Riot",
    "Ripple",
    "Saddle",
    "Soulbond",
    "Spree",
    "StartYourEngines",
    "Storm",
    "Sunburst",
    "Suspend",
    "TotemArmor",
    "Transmute",
    "Tribute",
    "Typecycling",
    "UmbraArmor",
    "Unearth",
    "Vanishing",
    "Ward",
    "Warp",
    "WebSlinging",
  };

  /// <summary>Every stem discovered via <c>[Keyword]</c> reflection (the same discovery
  /// <see cref="KeywordRegistry"/> performs internally).</summary>
  private static IReadOnlySet<string> DiscoveredKeywordStems() =>
    RuleRegistry
      .Discover<IKeyword, KeywordAttribute>("Keyword")
      .Select(d => StemOf(d.Rule.GetType().Name))
      .ToHashSet(StringComparer.Ordinal);

  /// <summary>The stems with a canonical-printed-form gold under <c>Fixtures/KeywordExpansions</c>.</summary>
  private static IReadOnlySet<string> FixturedKeywordStems() =>
    KeywordExpansionTestCaseLoader
      .GetAllTestCases()
      .Select(tc => tc.Name)
      .ToHashSet(StringComparer.Ordinal);

  /// <summary>One test case per keyword this suite CAN statically check (has a fixture).</summary>
  public static IEnumerable<TestCaseData> GetCheckableKeywordCases()
  {
    var discovered = DiscoveredKeywordStems();
    foreach (var tc in KeywordExpansionTestCaseLoader.GetAllTestCases())
    {
      if (!discovered.Contains(tc.Name))
        continue; // a fixture with no live [Keyword] type behind it — not this suite's concern
      yield return new TestCaseData(tc).SetName($"KeywordAnchorCoverage/{tc.Name}");
    }
  }

  /// <summary>
  /// Significant lowercase word-stems from a keyword's printed display name ("Choose a
  /// Background" → ["choose", "background"]; "Partner with" → ["partner", "with"]). Words of
  /// length ≤ 2 are dropped (stopword noise — "a", "of") so they can't produce spurious matches.
  /// </summary>
  private static IReadOnlyList<string> SignificantWords(string keywordDisplayName) =>
    Regex
      .Matches(keywordDisplayName, "[A-Za-z]+")
      .Select(m => m.Value.ToLowerInvariant())
      .Where(w => w.Length > 2)
      .ToList();

  /// <summary>
  /// True iff some anchor word in <paramref name="anchors"/> is a case-insensitive stem-match
  /// (exact, or a prefix either direction — "sacrific" vs "sacrifice"-family inflections) for some
  /// significant word of the keyword's own printed name. This is exactly the "keyword's own name,
  /// word-stem-matched, OR an explicit AnchorsFor alias" test the design review asked for: the
  /// existing aliases (firebending, modular, embalm) are literal lowercase copies of the keyword's
  /// name folded into the anchor list, so a single stem-match check catches both the "the keyword's
  /// own vocabulary already IS the anchor" case and the "an alias was added" case uniformly.
  /// </summary>
  private static bool AnchorsCoverKeyword(IReadOnlyList<string> anchors, string keywordDisplayName)
  {
    var words = SignificantWords(keywordDisplayName);
    foreach (var anchor in anchors)
    {
      var a = anchor.ToLowerInvariant();
      foreach (var w in words)
        if (a == w || a.StartsWith(w, StringComparison.Ordinal) || w.StartsWith(a, StringComparison.Ordinal))
          return true;
    }
    return false;
  }

  /// <summary>
  /// For every keyword with a canonical-printed-form gold: parse that printed form through the REAL
  /// production <see cref="OracleParser"/>, project it through the REAL <see cref="PortWalk"/>
  /// (mirroring <c>CardAtlasShared.Project</c> — the exact idiom <c>CardPortsStep</c> uses to build
  /// the <c>CardPortRow</c> index <see cref="SpanWitnessStep"/> consumes), and assert every
  /// checkable minted port label (one <see cref="SpanWitnessStep"/>'s <c>AnchorsFor</c> returns
  /// non-null for) is covered by the keyword's own name or an explicit alias.
  /// </summary>
  [TestCaseSource(nameof(GetCheckableKeywordCases))]
  public void Keyword_MintedPorts_AreAnchorCovered(KeywordExpansionTestCase testCase)
  {
    var parseResult = Parser.Parse(testCase.PrintedForm);
    var abilities = parseResult.Output.Abilities;
    Assert.That(
      abilities,
      Is.Not.Empty,
      $"Keyword '{testCase.Name}': the production OracleParser produced NO abilities for its own "
        + $"canonical printed form '{testCase.PrintedForm}' — cannot determine what ports it mints. "
        + "This is a parser regression, not an AnchorsFor gap."
    );

    var abilitiesNode = JsonSerializer.SerializeToNode(abilities, MagicASTJsonOptions.Strict);
    PortGraph graph;
    try
    {
      graph = new PortWalk(Ontology).Project($"__keyword_probe__::{testCase.Name}", abilitiesNode);
    }
    catch (Exception ex)
    {
      Assert.Fail(
        $"Keyword '{testCase.Name}': PortWalk.Project threw projecting the printed form "
          + $"'{testCase.PrintedForm}' ({ex.GetType().Name}: {ex.Message}). Cannot determine minted "
          + "ports — investigate the projection, not AnchorsFor."
      );
      return;
    }

    var checkableLabels = graph
      .Ports.Select(p => p.Label)
      .Distinct(StringComparer.Ordinal)
      .Select(label => (Label: label, Anchors: AnchorsFor(label)))
      .Where(x => x.Anchors is not null)
      .Select(x => (x.Label, Anchors: (IReadOnlyList<string>)x.Anchors!))
      .ToList();

    var uncovered = checkableLabels
      .Where(x => !AnchorsCoverKeyword(x.Anchors, testCase.Keyword))
      .ToList();

    if (uncovered.Count == 0)
      return;

    var message = string.Join(
      "\n",
      uncovered.Select(x =>
        $"Keyword '{testCase.Name}' mints a '{x.Label}' port but neither the word "
          + $"'{testCase.Keyword.ToLowerInvariant()}' nor any AnchorsFor alias covers that label's "
          + $"anchor set ({string.Join(" | ", x.Anchors)}) — add an alias to SpanWitnessStep.AnchorsFor "
          + $"or span-witness will report every {testCase.Name} port as an unexplained semantic suspect."
      )
    );
    Assert.Fail(message);
  }

  /// <summary>
  /// Self-verification: every <c>[Keyword]</c>-attributed type is EITHER checked for real by
  /// <see cref="Keyword_MintedPorts_AreAnchorCovered"/> (has a fixture) OR named in
  /// <see cref="KnownAnchorCoverageGaps"/> — never silently neither. Fails loudly in both
  /// directions: a new keyword with no fixture and no whitelist entry, or a whitelist entry that no
  /// longer needs to be there.
  /// </summary>
  [Test]
  public void EveryKeywordType_IsCoveredOrADocumentedGap()
  {
    var discovered = DiscoveredKeywordStems();
    var fixtured = FixturedKeywordStems();
    var undetermined = discovered.Except(fixtured, StringComparer.Ordinal).ToList();

    var missingFromWhitelist = undetermined
      .Except(KnownAnchorCoverageGaps, StringComparer.Ordinal)
      .OrderBy(s => s, StringComparer.Ordinal)
      .ToList();
    var staleInWhitelist = KnownAnchorCoverageGaps
      .Except(undetermined, StringComparer.Ordinal)
      .OrderBy(s => s, StringComparer.Ordinal)
      .ToList();

    Assert.Multiple(() =>
    {
      Assert.That(
        missingFromWhitelist,
        Is.Empty,
        "New keyword type(s) with no Fixtures/KeywordExpansions/*.json gold AND not in "
          + $"KnownAnchorCoverageGaps: {string.Join(", ", missingFromWhitelist)}. Either add a "
          + "canonical-printed-form fixture (promoting it into real AnchorsFor coverage checking) or "
          + "add it to KnownAnchorCoverageGaps with a reason — an undetermined keyword must never be "
          + "silent."
      );
      Assert.That(
        staleInWhitelist,
        Is.Empty,
        "Keyword type(s) in KnownAnchorCoverageGaps that no longer need to be there (a fixture now "
          + $"exists, or the keyword type was removed): {string.Join(", ", staleInWhitelist)}. Remove "
          + "them from the whitelist — they now get (or no longer need) real anchor-coverage checking."
      );
    });
  }
}
