using System.Text.Json;

namespace MagicAtlas.Bench;

/// <summary>
/// Regenerates <c>combo-expected-tiers.json</c>'s mechanistic <c>expected</c> block from a LIVE engine
/// run — never hand-typed (the governing principle: where a value is derivable, derive it, don't
/// hand-maintain it). Reads the CURRENT file tolerant of both the pre-2026-07-18 flat <c>reason</c>
/// schema and the post-migration <c>narrative</c> schema, so it is safe to re-run any time a
/// legitimately-changed verdict needs its pin refreshed (not just once, for the initial migration).
/// Carries <c>cards</c> / <c>expectedTier</c> / the narrative text over VERBATIM — this utility only
/// ever touches the mechanistic half of the schema; retiering a combo (Amber → Green etc.) is still a
/// deliberate, reviewed edit to <c>expectedTier</c>, never something this tool does silently.
/// </summary>
public static class ExpectedTiersMigration
{
  public static void Regenerate(
    string path,
    ComboRecallRunner runner,
    ComboSnapshot snapshot,
    DateOnly today
  )
  {
    var current = runner
      .Run(snapshot)
      .Combos.ToDictionary(c => c.Id, StringComparer.Ordinal);

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    var docComment = doc.RootElement.TryGetProperty("_doc", out var docProp)
      ? docProp.GetString() ?? ""
      : "";

    var pins = new List<ExpectedTierPin>();
    var missing = new List<string>();
    var mismatched = new List<string>();

    foreach (var e in doc.RootElement.GetProperty("combos").EnumerateArray())
    {
      var id = e.GetProperty("id").GetString()!;
      var cards = e.GetProperty("cards").EnumerateArray().Select(x => x.GetString()!).ToList();
      var expectedTier = e.GetProperty("expectedTier").GetString()!;
      var narrative = e.TryGetProperty("narrative", out var n) ? n.GetString() ?? ""
        : e.TryGetProperty("reason", out var r) ? r.GetString() ?? ""
        : throw new InvalidOperationException($"Combo '{id}' has neither 'narrative' nor 'reason' — cannot migrate.");
      // NarrativeVerifiedAt asserts a human verified `narrative` still describes reality. This tool
      // carries `narrative` over VERBATIM (never edits it) — so if the pin already had a verified date,
      // that verification is still current and must be preserved, not silently bumped to today just
      // because the mechanistic `expected` block was regenerated. Only a first-time migration (no prior
      // date — the pin only had the old flat `reason` field) legitimately stamps today.
      var narrativeVerifiedAt = e.TryGetProperty("narrativeVerifiedAt", out var v) && v.GetString() is { } existing
        ? existing
        : today.ToString("yyyy-MM-dd");

      if (!current.TryGetValue(id, out var result))
      {
        missing.Add(id);
        continue;
      }

      if (result.Outcome.ToString() != expectedTier)
      {
        // The pin file's tier must already match the live run (that's ComboExpectedTierTest's job) —
        // regenerating `expected` for a combo whose tier is already out of sync would bake a diagnostics
        // snapshot that doesn't correspond to the pinned tier. Refuse; the tier drift is a separate,
        // deliberate re-pin, not something this schema-migration tool silently papers over.
        mismatched.Add($"{id} (pinned '{expectedTier}', live '{result.Outcome}')");
        continue;
      }

      pins.Add(
        new ExpectedTierPin
        {
          Id = id,
          Cards = cards,
          ExpectedTier = expectedTier,
          Expected = result.Diagnostics is { } d ? ExpectedDiagnostics.FromDiagnostics(d) : null,
          Narrative = narrative,
          NarrativeVerifiedAt = narrativeVerifiedAt,
        }
      );
    }

    if (missing.Count > 0 || mismatched.Count > 0)
      throw new InvalidOperationException(
        "Cannot regenerate combo-expected-tiers.json — the pin file is out of sync with the live run:\n"
          + (missing.Count > 0 ? $"  pinned but not in the live run: [{string.Join(", ", missing)}]\n" : "")
          + (mismatched.Count > 0 ? $"  tier mismatch (fix the pin's expectedTier first): [{string.Join(", ", mismatched)}]\n" : "")
      );

    ExpectedTiersJson.Write(path, new ExpectedTiersDocument { Doc = docComment, Combos = pins });
  }
}
