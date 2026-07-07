using System.Text.Json;

namespace MagicAtlas.Ast.Tests.Flows.Common;

/// <summary>
/// Per-card downstream combo value, joined in from the InteractionTriage flow's
/// <c>allComboBlockingCards</c> overlay (keyed by card name). The shared unit of
/// the "value" axis: how many Commander Spellbook combos a card gates, and their
/// total popularity mass. Consumed by the MagicAstTriage yield-cluster ranking
/// (the parse pick surface's <c>fusedScore</c>) and the LabelCensus projection-gap
/// ranking (the projection pick surface) — so both effort types are prioritized
/// against the same downstream objective.
/// </summary>
public sealed record CardComboValue(int BlockedComboCount, long PopularityMass);

/// <summary>
/// Loads the per-card combo-value map from a prior InteractionTriage run's
/// <c>interaction-triage-report.json</c> (the <c>allComboBlockingCards</c> array).
/// Tolerant of a missing / field-less / malformed file — returns an empty map so
/// the consuming ranking degrades to its value-free order rather than failing.
/// Reading a sibling flow's committed output by path is intentional loose
/// coupling (combo popularity is stable across cycles), mirroring how the ratchet
/// baseline is read.
/// </summary>
public static class CardComboValueLoader
{
  public static IReadOnlyDictionary<string, CardComboValue> Load(string? path)
  {
    var map = new Dictionary<string, CardComboValue>(StringComparer.Ordinal);
    if (string.IsNullOrEmpty(path) || !File.Exists(path))
    {
      return map;
    }

    try
    {
      using var doc = JsonDocument.Parse(File.ReadAllText(path));
      if (
        !doc.RootElement.TryGetProperty("allComboBlockingCards", out var gaps)
        || gaps.ValueKind != JsonValueKind.Array
      )
      {
        return map;
      }

      foreach (var gap in gaps.EnumerateArray())
      {
        if (!gap.TryGetProperty("card", out var cardEl) || cardEl.ValueKind != JsonValueKind.String)
        {
          continue;
        }
        var name = cardEl.GetString()!;
        var blocked =
          gap.TryGetProperty("blockedComboCount", out var bc) && bc.TryGetInt32(out var bcv)
            ? bcv
            : 0;
        var mass = gap.TryGetProperty("popularityMass", out var pm) && pm.TryGetInt64(out var pmv)
          ? pmv
          : 0L;
        // Keep the strongest signal if a name somehow repeats.
        if (!map.TryGetValue(name, out var existing) || mass > existing.PopularityMass)
        {
          map[name] = new CardComboValue(blocked, mass);
        }
      }
    }
    catch (JsonException)
    {
      return new Dictionary<string, CardComboValue>(StringComparer.Ordinal);
    }

    return map;
  }
}
