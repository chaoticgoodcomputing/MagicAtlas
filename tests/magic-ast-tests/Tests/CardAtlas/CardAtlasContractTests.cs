namespace MagicAST.Tests.Tests.CardAtlas;

using System.Text.Json;
using MagicAST;
using MagicAST.Tests.Infrastructure;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.CardAtlas.Steps;
using MagicAtlas.Ast.Tests.Flows.Shared;

/// <summary>
/// Contract GATE for the CardAtlas data layer (D1–D4). Runs the three steps end-to-end on a tiny
/// committed fixture (real oracle text for a known sacrifice combo — Chatterfang × Pitiless Plunderer —
/// plus a sac outlet + a death payoff), then asserts the cross-dataset invariants the API/UI will rely on
/// and a handful of golden facts. Stateless (fixture is committed, no gitignored corpus), fast (three
/// tiny per-combo materializes), and deterministic — a real gate, not a diagnostic.
/// </summary>
[TestFixture]
public class CardAtlasContractTests
{
  private sealed record Fixture(List<FixtureCard> Cards, List<FixtureCombo> Combos);

  private sealed record FixtureCard(
    string Name,
    string ManaCost,
    string TypeLine,
    List<string> ColorIdentity,
    string OracleText
  );

  private sealed record FixtureCombo(string Id, int Popularity, List<string> Results, List<string> Cards);

  private static readonly Fixture Data = Load();
  private static readonly List<MastCardInput> CardInputs = Data
    .Cards.Select(c => new MastCardInput
    {
      ScryfallId = c.Name,
      Input = new CardInputDTO
      {
        Name = c.Name,
        ManaCost = c.ManaCost,
        TypeLine = c.TypeLine,
        ColorIdentity = c.ColorIdentity,
        OracleText = c.OracleText,
      },
    })
    .ToList();
  private static readonly List<Combo> Combos = Data
    .Combos.Select(c => new Combo
    {
      Id = c.Id,
      Popularity = c.Popularity,
      Results = c.Results,
      Cards = c.Cards.Select(n => new ComboCard { Name = n }).ToList(),
    })
    .ToList();

  private static Fixture Load()
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "CardAtlas",
      "sac-fixture.json"
    );
    return JsonSerializer.Deserialize<Fixture>(
      File.ReadAllText(path),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    )!;
  }

  // Run the pipeline once for the whole fixture (D1 → D4 → D2/D3).
  private static readonly (IEnumerable<CardMetaRow> Meta, IEnumerable<CardPortRow> Ports) D1 =
    CardPortsStep.Create(TestData.OntologyPath)((Combos, CardInputs));
  private static readonly List<CardMetaRow> Meta = D1.Meta.ToList();
  private static readonly List<CardPortRow> Ports = D1.Ports.ToList();
  private static readonly List<ComboInstanceRow> Instances = ReconstructCombosStep
    .Create(TestData.OntologyPath)((Combos, CardInputs))
    .Rows.ToList();
  private static readonly (ResourceGraph Graph, ArchetypeCatalog Catalog) D23 = FamilyRollupStep.Create()(
    (Ports, Instances)
  );

  // ── D1 CardPorts ──────────────────────────────────────────────────────────────────────────────

  [Test]
  public void D1_emits_every_union_card_with_metadata()
  {
    Assert.That(Meta, Is.Not.Empty);
    // All five fixture cards are in a parse-ready combo, so all appear.
    var names = Meta.Select(m => m.Card).ToHashSet(StringComparer.Ordinal);
    Assert.That(names, Does.Contain("Ashnod's Altar"));
    Assert.That(names, Does.Contain("Blood Artist"));
    Assert.That(names, Does.Contain("Chatterfang, Squirrel General"));
  }

  [Test]
  public void D1_derives_mana_value_from_cost()
  {
    Assert.That(Meta.Single(m => m.Card == "Ashnod's Altar").Cmc, Is.EqualTo(3)); // {3}
    Assert.That(Meta.Single(m => m.Card == "Blood Artist").Cmc, Is.EqualTo(2)); // {1}{B}
  }

  [Test]
  public void D1_carries_color_identity()
  {
    Assert.That(Meta.Single(m => m.Card == "Ashnod's Altar").ColorIdentity, Is.EqualTo("")); // colorless
    Assert.That(Meta.Single(m => m.Card == "Blood Artist").ColorIdentity, Is.EqualTo("B"));
  }

  [Test]
  public void D1_detects_the_sac_outlet_port()
  {
    var altar = Ports.Where(p => p.Card == "Ashnod's Altar").ToList();
    var sac = altar.SingleOrDefault(p => p.Label.StartsWith("sac:", StringComparison.Ordinal));
    Assert.That(sac, Is.Not.Null, "Ashnod's Altar must project a sac: consume port");
    Assert.That(sac!.Family, Is.EqualTo("sacrifice"));
    Assert.That(sac.Side, Is.EqualTo("consume"));
    Assert.That(
      altar.Any(p => p.Label.StartsWith("emit:mana", StringComparison.Ordinal) && p.Side == "emit"),
      Is.True,
      "and an emit:mana producer port"
    );
  }

  /// <summary>ADR 0004 #43 — the retired four-valued port tier is split into two orthogonal dimensions:
  /// a conditionality facet and a provenance marker. This pins the invariants the API/UI rely on and,
  /// crucially, that the two are independent — a port's conditionality never encodes its provenance, so a
  /// conditional inferred port (the conflation the enum could not hold) is expressible.</summary>
  [Test]
  public void D1_splits_the_port_tier_into_conditionality_and_provenance()
  {
    Assert.Multiple(() =>
    {
      foreach (var p in Ports)
      {
        // Provenance is one of the three markers; it never leaks into the conditionality axis.
        Assert.That(
          p.Provenance,
          Is.AnyOf("", "Inferred", "Declared"),
          $"{p.Card}/{p.Label} provenance"
        );
        Assert.That(
          p.Conditionality,
          Is.Not.AnyOf("Inferred", "Declared", "Green", "Amber"),
          $"{p.Card}/{p.Label} conditionality must not carry a provenance/legacy-tier value"
        );

        if (string.IsNullOrEmpty(p.Provenance))
          // A parsed port describes its conditionality (unconditional, or a gate list).
          Assert.That(
            p.Conditionality,
            Is.Not.Empty,
            $"parsed port {p.Card}/{p.Label} must carry a conditionality phrase"
          );
        else
          // A backfill marker has no parsed mechanism to describe.
          Assert.That(
            p.Conditionality,
            Is.Empty,
            $"backfill port {p.Card}/{p.Label} carries no conditionality"
          );
      }
    });
  }

  [Test]
  public void D1_parsed_ports_use_the_conditionality_renderer_vocabulary()
  {
    // Every parsed port's conditionality is a phrase PortConditionality can produce — the "green" case
    // (fires unconditionally) or a "·"-joined list of the known gate phrases. No opaque values.
    var known = new HashSet<string>(
      new[]
      {
        MagicAST.Interaction.PortConditionality.Unconditional,
        MagicAST.Interaction.PortConditionality.TapGated,
        MagicAST.Interaction.PortConditionality.RequiresCounter,
        MagicAST.Interaction.PortConditionality.RateLimited,
      },
      StringComparer.Ordinal
    );
    Assert.Multiple(() =>
    {
      foreach (var p in Ports.Where(p => string.IsNullOrEmpty(p.Provenance)))
        foreach (var phrase in p.Conditionality.Split(" · "))
          Assert.That(known, Does.Contain(phrase), $"{p.Card}/{p.Label}: '{phrase}'");
    });
  }

  // ── D4 ComboInstances ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void D4_reconstructs_the_golden_sac_combo_with_named_cards()
  {
    var golden = Instances.Where(i => i.ComboId == "3000-4871").ToList();
    Assert.That(golden, Is.Not.Empty, "Chatterfang × Pitiless Plunderer must reconstruct");
    var sacToken = golden.Single(i =>
      i.FamilySignature.Contains("sacrifice") && i.FamilySignature.Contains("token")
    );
    Assert.That(sacToken.Cards, Does.Contain("Chatterfang, Squirrel General"));
    Assert.That(sacToken.Cards, Does.Contain("Pitiless Plunderer"));
    Assert.That(sacToken.CardCount, Is.GreaterThanOrEqualTo(2));
    Assert.That(sacToken.Results, Does.Contain("mana")); // CSB result carried through
  }

  [Test]
  public void D4_every_row_has_a_valid_tier_and_sorted_signature()
  {
    Assert.That(Instances, Is.Not.Empty);
    foreach (var i in Instances)
    {
      Assert.That(new[] { "Green", "Amber", "Red" }, Does.Contain(i.Tier), $"combo {i.ComboId}");
      var fams = i.FamilySignature.Split(", ");
      Assert.That(
        fams,
        Is.EqualTo(fams.OrderBy(f => f, StringComparer.Ordinal)).AsCollection,
        "family-signature must be sorted (a stable archetype key)"
      );
      Assert.That(i.CardCount, Is.GreaterThanOrEqualTo(2), "no 1-card combo exists in MTG");
    }
  }

  // ── Cross-dataset invariants (the API/UI joins) ─────────────────────────────────────────────────

  [Test]
  public void ComboInstance_cards_all_exist_in_the_card_index()
  {
    var known = Meta.Select(m => m.Card).ToHashSet(StringComparer.Ordinal);
    foreach (var i in Instances)
      foreach (var card in i.Cards.Split(" + "))
        Assert.That(known, Does.Contain(card), $"D4 card '{card}' must be in the D1 index");
  }

  [Test]
  public void D3_archetypes_are_all_realized_and_canonical()
  {
    Assert.That(D23.Catalog.Entries, Is.Not.Empty);
    Assert.That(D23.Catalog.RealizedArchetypes, Is.EqualTo(D23.Catalog.Entries.Count));
    foreach (var e in D23.Catalog.Entries)
    {
      Assert.That(e.RealizingCombos, Is.GreaterThanOrEqualTo(1), "a realized archetype needs ≥1 combo");
      Assert.That(new[] { "Green", "Amber", "Red" }, Does.Contain(e.BestTier));
      Assert.That(e.GreenFraction, Is.InRange(0.0, 1.0));
      foreach (var fam in e.Families.Split(", "))
        Assert.That(ResourceFamilies.Canonical, Does.Contain(fam), $"archetype family '{fam}' must be canonical");
    }
    Assert.That(
      D23.Catalog.Entries.Any(e => e.Families.Contains("sacrifice")),
      Is.True,
      "the sac fixture must yield a sacrifice-bearing archetype"
    );
  }

  [Test]
  public void D2_stations_and_lines_are_canonical_and_sacrifice_is_present()
  {
    Assert.That(D23.Graph.Stations, Is.Not.Empty);
    foreach (var s in D23.Graph.Stations)
      Assert.That(ResourceFamilies.Canonical, Does.Contain(s.Family));
    foreach (var l in D23.Graph.Lines)
    {
      Assert.That(ResourceFamilies.Canonical, Does.Contain(l.From));
      Assert.That(ResourceFamilies.Canonical, Does.Contain(l.To));
      Assert.That(l.RealizingCombos, Is.GreaterThanOrEqualTo(1));
      Assert.That(l.From, Is.Not.EqualTo(l.To), "no station self-loop line");
    }
    Assert.That(D23.Graph.Stations.Any(s => s.Family == "sacrifice"), Is.True);
  }
}
