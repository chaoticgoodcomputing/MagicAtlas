using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

namespace MagicAtlas.Flows.MagicAstTriage;

/// <summary>
/// Detects <b>lossy-but-clean</b> parses — a clause that produced NO
/// <c>UnparsedAbility</c> (so it looks parsed, and carries no diagnostic) yet
/// silently dropped structure, collapsing several sentences into a simpler AST.
/// This is the failure mode the triage's per-line <c>Patterns</c>/clean-exemplar
/// signal is blind to: a "clean" sibling line can be a lossy collapse, which
/// misleads the fused surface's exemplar ranking on multi-line cards.
/// </summary>
/// <remarks>
/// The reliable signal is a <b>trigger deficit</b>: count sentence-initial trigger
/// openers (When / Whenever / At the beginning / At the end) across the card
/// (reminder text in parentheses stripped; quoted granted abilities INCLUDED),
/// and compare to the number of <see cref="TriggeredAbility"/> /
/// <see cref="DelayedTriggeredAbility"/> nodes the parser actually produced (walked
/// recursively, so a correctly-parsed granted ability's nested trigger balances its
/// quoted opener). A positive deficit is a <em>definitively</em> dropped trigger.
/// It is intentionally CONSERVATIVE (a deficit means a real drop; it does not claim
/// to catch every lossy shape).
///
/// <para>Promoted verbatim from tests/magic-ast-tests/Flows/MagicAstTriage/LossyParseAnalyzer.cs
/// (namespace fixed to MagicAtlas.Flows.*) — a dependency of <c>ParseCorpusStep</c>.</para>
/// </remarks>
public static class LossyParseAnalyzer
{
  private static readonly Assembly AstAssembly = typeof(TriggeredAbility).Assembly;

  // A trigger opener at a clause boundary: start of text, or after a sentence
  // terminator / newline / opening quote (so quoted granted abilities count too).
  private static readonly Regex TriggerOpener = new(
    "(?:^|[.\\n\"“”]\\s*)(?:When\\b|Whenever\\b|At the beginning\\b|At the end\\b)",
    RegexOptions.Compiled
  );

  // Reminder text (parenthetical) is not modeled as abilities, so its "when"s
  // would create phantom deficits — strip it before counting.
  private static readonly Regex ReminderText = new(@"\([^)]*\)", RegexOptions.Compiled);

  public readonly record struct LossySignal(int DroppedTriggers, int TriggerOpeners, int TriggeredNodes)
  {
    /// <summary>True when the parse dropped at least one trigger it should have produced.</summary>
    public bool SuspectedLossy => DroppedTriggers > 0;
  }

  /// <summary>
  /// Analyze a card's oracle text against its parsed abilities for a trigger deficit.
  /// </summary>
  public static LossySignal Analyze(string? oracleText, IEnumerable<object?> abilities)
  {
    if (string.IsNullOrEmpty(oracleText))
    {
      return new LossySignal(0, 0, 0);
    }

    var stripped = ReminderText.Replace(oracleText, " ");
    var openers = TriggerOpener.Matches(stripped).Count;

    var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
    var triggered = 0;
    foreach (var ability in abilities)
    {
      triggered += CountTriggered(ability, seen);
    }

    return new LossySignal(Math.Max(0, openers - triggered), openers, triggered);
  }

  /// <summary>
  /// Recursively counts <see cref="TriggeredAbility"/> and
  /// <see cref="DelayedTriggeredAbility"/> nodes anywhere in the AST subtree —
  /// including granted abilities nested inside token-creation / gain-ability
  /// effects — so quoted triggers balance their openers. Mirrors
  /// <c>ResidualWalker</c>'s reflection walk (AST-assembly reference types +
  /// enumerables only).
  /// </summary>
  private static int CountTriggered(object? node, HashSet<object> seen)
  {
    if (node is null || (!node.GetType().IsValueType && !seen.Add(node)))
    {
      return 0;
    }

    var count = node is TriggeredAbility or DelayedTriggeredAbility ? 1 : 0;

    foreach (var prop in node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
      if (prop.GetIndexParameters().Length > 0)
      {
        continue;
      }
      object? value;
      try
      {
        value = prop.GetValue(node);
      }
      catch
      {
        continue;
      }
      if (value is not null)
      {
        count += CountDescend(value, seen);
      }
    }

    return count;
  }

  private static int CountDescend(object value, HashSet<object> seen)
  {
    if (value is string)
    {
      return 0;
    }

    if (value is IEnumerable sequence)
    {
      var total = 0;
      foreach (var item in sequence)
      {
        if (item is not null)
        {
          total += CountDescend(item, seen);
        }
      }
      return total;
    }

    var type = value.GetType();
    if (type.Assembly == AstAssembly && type is { IsEnum: false, IsValueType: false })
    {
      return CountTriggered(value, seen);
    }

    return 0;
  }
}
