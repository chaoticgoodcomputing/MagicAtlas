namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "exile target [filter] [an opponent controls] with mana value N or less/greater
/// until this [type] leaves the battlefield." — the mana-value-gated variant of the
/// Oblivion-Ring / Hieromancer's Cage ETB exile family (Portable Hole: "exile target
/// nonland permanent an opponent controls with mana value 2 or less until this artifact
/// leaves the battlefield").
///
/// <para>
/// This is the sibling of <see cref="ExileUntilLeavesTriggeredRule"/>: identical
/// temporary-exile shape (Rule 611.1 — a continuous effect with a duration; Rule 406.1 —
/// the exile zone; the return when this permanent leaves the battlefield is the linked
/// engine behaviour, not a separate emitted effect), but with a mandatory
/// "with mana value N or less/greater" qualifier (Rule 202.3 — mana value) sitting
/// between the exile-target filter and the "until this ... leaves the battlefield" tail.
/// That interior qualifier makes the plain family rule's <c>ParseTargetFilter</c> decline
/// (the "or" in "N or less" defeats its card-type split), so this dedicated rule closes
/// the gap. The threshold lands on <see cref="ObjectFilter.ManaValueComparison"/>.
/// </para>
///
/// The whole pattern is anchored (^…$) and the mana-value clause is mandatory, so it
/// cannot substring-match a sibling that lacks the qualifier (which
/// <see cref="ExileUntilLeavesTriggeredRule"/> continues to own). Rule 701.13 (exile);
/// Rule 603.6 (the ETB zone-change trigger that carries this effect).
/// </summary>
[TriggeredRule]
public sealed class ExileTargetByManaValueUntilLeavesTriggeredRule : ITriggeredRule
{
  // "exile target <filter> [an opponent controls] with mana value N or less/greater
  //  until this <type> leaves the battlefield". <filter> is captured non-greedily; the
  //  optional controller clause, the mana-value threshold, and the self-reference type
  //  are captured separately.
  private static readonly Regex Pattern = new(
    @"^exile\s+target\s+(?<filter>.+?)(?<oppctrl>\s+an\s+opponent\s+controls)?"
    + @"\s+with\s+mana\s+value\s+(?<n>\d+)\s+or\s+(?<dir>less|fewer|greater|more)"
    + @"\s+until\s+this\s+(?<type>creature|artifact|enchantment|permanent|aura|land|vehicle)"
    + @"\s+leaves\s+the\s+battlefield$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var filter = ParseTargetFilter(m.Groups["filter"].Value.Trim());
    if (filter is null)
    {
      return false;
    }

    if (m.Groups["oppctrl"].Success)
    {
      filter = filter with { Controller = ControllerFilter.Opponent };
    }

    var n = int.Parse(m.Groups["n"].Value);
    var dir = m.Groups["dir"].Value.ToLowerInvariant();
    var op = dir is "less" or "fewer"
      ? ComparisonOperator.LessThanOrEqual
      : ComparisonOperator.GreaterThanOrEqual;
    filter = filter with
    {
      ManaValueComparison = new Comparison { Operator = op, Value = n },
    };

    var selfType = m.Groups["type"].Value.ToLowerInvariant();

    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = filter,
      },
      Duration = new UntilLeavesBattlefieldDuration
      {
        Object = $"this {selfType}",
      },
    };
    return true;
  }

  // Bare card-type tokens accepted in the exile-target filter (Rule 205.2 — card types).
  private static readonly HashSet<string> CardTypeTokens = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature",
    "artifact",
    "enchantment",
    "planeswalker",
    "permanent",
    "land",
    "battle",
  };

  /// <summary>
  /// Resolves the exile-target filter phrase between "target" and the mana-value
  /// qualifier. Handles "nonland permanent" (the Oblivion-Ring shape) and a single
  /// bare card type. Returns null for any other shape so this rule declines rather
  /// than over-approximating.
  /// </summary>
  private static ObjectFilter? ParseTargetFilter(string phrase)
  {
    var lower = phrase.ToLowerInvariant();

    // "nonland permanent" — the Oblivion-Ring shape (permanent, with land excluded).
    if (lower == "nonland permanent")
    {
      return new ObjectFilter
      {
        CardTypes = ["permanent"],
        ExcludedCardTypes = ["land"],
      };
    }

    // Single bare card type ("creature", "artifact", "permanent", …).
    if (CardTypeTokens.Contains(lower))
    {
      return new ObjectFilter { CardTypes = [lower] };
    }

    return null;
  }
}
