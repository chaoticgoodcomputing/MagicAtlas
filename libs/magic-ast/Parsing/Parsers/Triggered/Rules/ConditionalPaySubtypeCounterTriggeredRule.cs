namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Handles "you may pay {COST}. If you do, put a +1/+1 counter on it. If it's a
/// [Subtype], put two +1/+1 counters on it instead." — the optional-pay triggered
/// ability with a subtype-conditional counter boost (Emiel the Blessed pattern).
///
/// <para>
/// The effect is an <see cref="OptionalEffect"/> wrapping a
/// <see cref="ConditionalPayEffect"/>; the <c>IfYouDo</c> consequence is a
/// <see cref="ConditionalEffect"/> whose <c>Condition</c> is an
/// <see cref="ObjectHasSubtypeCondition"/> (CR 205.3m — creature subtypes).
/// When the subtype matches, two +1/+1 counters are placed (the "instead" branch —
/// the <c>Then</c>); otherwise one counter (the <c>Else</c>). "instead" signals
/// a replacement of the base action for the qualified subtype — modelled as an
/// if-then-else rather than a sequential pair so the engine knows that the
/// one-counter placement does NOT happen for Unicorns.
/// </para>
///
/// <para>
/// Priority 85: higher than <see cref="ConditionalPayTriggeredRule"/> (Priority 80)
/// so this specific subtype-counter shape is claimed first; the generic conditional-pay
/// handler never sees this text. Anchored sub-patterns guard against substring capture
/// on sibling rules that match on "{counter} counter(s)".
/// </para>
///
/// CR 122.1 (verbatim): "A counter is a marker placed on an object or player that
/// modifies its characteristics and/or interacts with a rule, ability, or effect."
/// CR 205.3m (verbatim): "Creature subtypes are always a single word and are listed
/// after a long dash on the card's type line."
/// CR 602.1 / CR 603.2: activated/triggered ability structure.
/// </summary>
[TriggeredRule(Priority = 85)]
public sealed class ConditionalPaySubtypeCounterTriggeredRule : ITriggeredRule
{
  // Full-clause pattern (anchored ^…$):
  // "you may pay {COST}. If you do, put a +1/+1 counter on it.
  //  If it's a [Subtype], put [N] +1/+1 counters on it instead."
  // where {COST} is one or more mana symbols and [Subtype] is a proper-cased word.
  // The base count in the second branch is always two for this pattern; the "instead"
  // marks a replacement. Captures: cost, subtype.
  private static readonly Regex Pattern = new(
    @"^you\s+may\s+pay\s+(?<cost>(?:\{[^}]+\})+)\s*\.\s*If\s+you\s+do,\s+put\s+a\s+\+1/\+1\s+counter\s+on\s+it\s*\.\s*If\s+it'?s\s+an?\s+(?<subtype>[A-Z][a-zA-Z]*),\s+put\s+(?<altcount>two|three|four|five|\d+)\s+\+1/\+1\s+counters?\s+on\s+it\s+instead\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();
    var match = Pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    var costStr = match.Groups["cost"].Value;
    var manaCost = TriggeredRuleHelpers.TryBuildManaCost(costStr);
    if (manaCost is null)
    {
      return false;
    }

    var subtype = match.Groups["subtype"].Value;
    var altCountRaw = match.Groups["altcount"].Value.ToLowerInvariant();
    var altCount = altCountRaw switch
    {
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.TryParse(altCountRaw, out var n) ? n : 2,
    };

    // "If you do" consequence: if it's a [Subtype], put altCount counters (Then),
    // else put 1 counter (Else). The "instead" signals replacement — not additive.
    var subtypeCondition = new ObjectHasSubtypeCondition
    {
      Subtype = subtype,
      Subject = "It",
    };

    var putAltCounters = new PutCountersEffect
    {
      Target = ObjectReference.It(),
      CounterType = "+1/+1",
      Count = LiteralQuantity.Of(altCount),
    };

    var putOneCounter = new PutCountersEffect
    {
      Target = ObjectReference.It(),
      CounterType = "+1/+1",
      Count = LiteralQuantity.Of(1),
    };

    var ifYouDoEffect = new ConditionalEffect
    {
      Condition = subtypeCondition,
      Then = putAltCounters,
      Else = putOneCounter,
    };

    effect = new OptionalEffect
    {
      Inner = new ConditionalPayEffect
      {
        Cost = manaCost,
      },
      IfYouDo = ifYouDoEffect,
    };
    return true;
  }
}
