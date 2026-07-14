using Flowthru.Step;
using MagicAST;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;

namespace MagicAtlas.Flows.InteractionTriage.Steps;

/// <summary>
/// Ranks the unparsed <b>hub cards</b> by the combo-popularity value each one gates — the demand-side
/// pick surface (<see cref="ComboAnchorReport"/>). Reuses the exact <c>fullyParsed</c> definition (a card
/// whose every ability parses) so a hub here == a blocking card in the interaction-triage classifier, then
/// joins each hub against the combo DB for its neighborhood: sole-blocker count, co-stars (flagged if they
/// too are unparsed), and the payoffs its blocked combos produce. Joins <see cref="MastCardInput"/> for the
/// type line and to refine the block reason — an empty <c>OracleText</c> is the DFC/MDFC/Room ingest gap
/// (a DATA-layer fix), not a parser family. Measurement/pick surface only — never a gate.
///
/// <para>Promoted from tests/magic-ast-tests/Flows/InteractionTriage/Steps/RankComboAnchorsStep.cs.</para>
/// </summary>
[FlowthruStep]
public static class RankComboAnchorsStep
{
  private const int TopAnchorCount = 50;
  private const int TopCoStarsPerHub = 8;
  private const int TopPayoffsPerHub = 5;

  public static Func<
    (IEnumerable<Combo> Combos, IEnumerable<ParseRecord> Records, IEnumerable<MastCardInput> CardInputs),
    ComboAnchorReport
  > Create() =>
    inputs =>
    {
      var combos = inputs.Combos.ToList();
      var records = inputs.Records.ToList();

      var inCorpus = records.Select(r => r.CardName).ToHashSet(StringComparer.Ordinal);
      var fullyParsed = records
        .Where(r => r.TotalAbilities > 0 && r.TotalAbilities == r.ParsedAbilities)
        .Select(r => r.CardName)
        .ToHashSet(StringComparer.Ordinal);

      // Card text keyed by exact name, with a double-faced first-face fallback ("A // B" -> "A").
      var textByName = new Dictionary<string, CardInputDTO>(StringComparer.Ordinal);
      foreach (var ci in inputs.CardInputs)
        textByName[ci.Input.Name] = ci.Input;

      CardInputDTO? Text(string name)
      {
        if (textByName.TryGetValue(name, out var exact))
          return exact;
        var slash = name.IndexOf(" // ", StringComparison.Ordinal);
        return slash >= 0 && textByName.TryGetValue(name[..slash], out var face) ? face : null;
      }

      // One pass over the combo DB, accumulating per hub. A hub blocks a combo iff it is one of that
      // combo's not-fully-parsed cards; it is the SOLE blocker iff it is the only one.
      var hubs = new Dictionary<string, HubAccumulator>(StringComparer.Ordinal);
      foreach (var combo in combos)
      {
        var blocking = combo.Cards
          .Select(c => c.Name)
          .Where(n => !fullyParsed.Contains(n))
          .Distinct(StringComparer.Ordinal)
          .ToList();
        if (blocking.Count == 0)
          continue;

        var soleBlocker = blocking.Count == 1;
        foreach (var hubName in blocking)
        {
          if (!hubs.TryGetValue(hubName, out var acc))
            hubs[hubName] = acc = new HubAccumulator();

          acc.BlockedComboCount++;
          acc.PopularityMass += combo.Popularity;
          if (combo.Popularity > acc.MaxComboPopularity)
            acc.MaxComboPopularity = combo.Popularity;
          if (soleBlocker)
            acc.SoleBlockerCount++;

          foreach (var result in combo.Results)
            acc.ResultMass[result] = acc.ResultMass.GetValueOrDefault(result) + combo.Popularity;

          foreach (var other in combo.Cards.Select(c => c.Name))
          {
            if (string.Equals(other, hubName, StringComparison.Ordinal))
              continue;
            if (!acc.CoStars.TryGetValue(other, out var co))
              acc.CoStars[other] = co = new CoStarAccumulator { AlsoUnparsed = !fullyParsed.Contains(other) };
            co.SharedCombos++;
            co.SharedPopularity += combo.Popularity;
          }
        }
      }

      // Compose face text the way the parse pipeline does (ParseCorpusStep / CardAtlasShared `\n\n`
      // idiom) before judging emptiness. A double-faced card carries its rules text in CardFaces with a
      // null top-level OracleText, and it PARSES from the composed faces. So "empty-oracle-text" must
      // mean empty AFTER composing — otherwise every DFC parser-family is mislabeled a data gap and
      // wrongly skipped. Post-compose, empty-oracle-text collapses to genuinely textless cards.
      string ReasonFor(string card)
      {
        if (!inCorpus.Contains(card))
          return "missing-from-corpus";
        var dto = Text(card);
        var text = dto?.OracleText;
        if (string.IsNullOrWhiteSpace(text) && dto?.CardFaces is { Count: > 0 })
          text = string.Join("\n\n", dto.CardFaces.Select(f => f.OracleText ?? "").Where(t => t.Length > 0));
        return string.IsNullOrWhiteSpace(text) ? "empty-oracle-text" : "parser-family";
      }

      var anchors = hubs
        .Select(kv => new ComboAnchor
        {
          Card = kv.Key,
          TypeLine = Text(kv.Key)?.TypeLine ?? "",
          BlockReason = ReasonFor(kv.Key),
          BlockedComboCount = kv.Value.BlockedComboCount,
          SoleBlockerCount = kv.Value.SoleBlockerCount,
          PopularityMass = kv.Value.PopularityMass,
          MaxComboPopularity = kv.Value.MaxComboPopularity,
          TopPayoffs = kv.Value.ResultMass
            .OrderByDescending(r => r.Value)
            .Take(TopPayoffsPerHub)
            .Select(r => r.Key)
            .ToList(),
          CoStars = kv.Value.CoStars
            .OrderByDescending(c => c.Value.SharedPopularity)
            .ThenByDescending(c => c.Value.SharedCombos)
            .Take(TopCoStarsPerHub)
            .Select(c => new ComboCoStar
            {
              Card = c.Key,
              SharedCombos = c.Value.SharedCombos,
              SharedPopularity = c.Value.SharedPopularity,
              AlsoUnparsed = c.Value.AlsoUnparsed,
            })
            .ToList(),
        })
        .ToList();

      int CountWhere(string reason) => anchors.Count(a => a.BlockReason == reason);
      long MassWhere(string reason) => anchors.Where(a => a.BlockReason == reason).Sum(a => a.PopularityMass);

      return new ComboAnchorReport
      {
        GeneratedAt = DateTime.UtcNow,
        TotalCombos = combos.Count,
        TotalHubs = anchors.Count,
        ParserFamilyHubs = CountWhere("parser-family"),
        ParserFamilyMass = MassWhere("parser-family"),
        EmptyTextHubs = CountWhere("empty-oracle-text"),
        EmptyTextMass = MassWhere("empty-oracle-text"),
        MissingFromCorpusHubs = CountWhere("missing-from-corpus"),
        MissingFromCorpusMass = MassWhere("missing-from-corpus"),
        TopAnchors = anchors
          .OrderByDescending(a => a.PopularityMass)
          .ThenByDescending(a => a.BlockedComboCount)
          .Take(TopAnchorCount)
          .ToList(),
      };
    };
}

/// <summary>Mutable per-hub accumulator (file-scoped; not a serialized schema).</summary>
file sealed class HubAccumulator
{
  public int BlockedComboCount;
  public int SoleBlockerCount;
  public long PopularityMass;
  public int MaxComboPopularity;
  public Dictionary<string, long> ResultMass { get; } = new(StringComparer.Ordinal);
  public Dictionary<string, CoStarAccumulator> CoStars { get; } = new(StringComparer.Ordinal);
}

file sealed class CoStarAccumulator
{
  public int SharedCombos;
  public long SharedPopularity;
  public bool AlsoUnparsed;
}
