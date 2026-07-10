namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may pay {E}[{E}…]. When you do, put N +1/+1 counters and a [keyword] counter on
/// target attacking creature. It becomes a[n] [Subtype] in addition to its other types." —
/// the optional pay-energy reflexive on Guide of Souls' "Whenever you attack" trigger.
///
/// <para>Modelled as an <see cref="OptionalEffect"/> ("you may …") whose
/// <see cref="OptionalEffect.Inner"/> is a <see cref="ConditionalPayEffect"/> carrying the
/// <see cref="PayEnergyCost"/>, and whose <see cref="OptionalEffect.IfYouDo"/> holds the
/// consequent — the same shape the energy-pay sibling Lightning Runner produces for
/// "you may pay eight {E}. If you pay, …". The consequent is a
/// <see cref="CompositeEffect"/> of the two counter placements (the +1/+1 counters and the
/// keyword counter, split because <see cref="PutCountersEffect"/> holds one counter type
/// each) plus the additive type grant.</para>
///
/// <para>CR 603.12 (verbatim): "A resolving spell or ability may allow or instruct a player
/// to take an action and create a triggered ability that triggers 'when [a player] [does or
/// doesn't]' take that action …" — the "When you do" reflexive fires on paying the optional
/// {E} cost. CR 107.14 / CR 122.1 (energy counters; counter markers). CR 702.9a (a flying
/// counter grants flying). CR 205.1a: an effect may add a subtype while the object retains
/// its prior types ("in addition to its other types") — modelled by
/// <see cref="AddTypeEffect"/>. The "target attacking creature" set is a combat-state
/// filter (CR 508 — Declare Attackers).</para>
///
/// <para>ANCHORED (^…$): the whole reflexive body is anchored, so it cannot substring-match
/// into any sibling. "It" (CR 113.8b) in the type clause refers to the just-targeted
/// attacking creature.</para>
/// </summary>
[TriggeredRule]
public sealed class MayPayEnergyPutCountersAndBecomeSubtypeReflexiveRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+pay\s+(?<energy>(?:\{E\}\s*)+)\.\s*When\s+you\s+do,\s*"
      + @"put\s+(?<pt>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+\+1/\+1\s+counters?\s+"
      + @"and\s+a(?:n)?\s+(?<kw>[\w+/\-]+)\s+counter\s+on\s+target\s+attacking\s+creature\.\s*"
      + @"It\s+becomes\s+a(?:n)?\s+(?<subtype>[A-Za-z]+)\s+in\s+addition\s+to\s+its\s+other\s+types\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _energySymbol = new(
    @"\{E\}",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var energyCount = _energySymbol.Matches(m.Groups["energy"].Value).Count;
    if (energyCount <= 0)
    {
      return false;
    }

    var ptCount = TriggeredRuleHelpers.ParseWordOrDigitCount(m.Groups["pt"].Value) ?? 1;
    var keywordCounter = m.Groups["kw"].Value.ToLowerInvariant();

    var subtypeRaw = m.Groups["subtype"].Value;
    var subtype = char.ToUpperInvariant(subtypeRaw[0]) + subtypeRaw[1..].ToLowerInvariant();

    var attackingCreature = new ObjectReference
    {
      Kind = ObjectReferenceKind.Target,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Characteristics = [new CombatStateCharacteristic { State = CombatState.Attacking }],
      },
    };

    var consequent = new CompositeEffect
    {
      Effects = new List<Effect>
      {
        new PutCountersEffect
        {
          Target = attackingCreature,
          CounterType = "+1/+1",
          Count = LiteralQuantity.Of(ptCount),
        },
        new PutCountersEffect
        {
          Target = ObjectReference.It(),
          CounterType = keywordCounter,
          Count = LiteralQuantity.Of(1),
        },
        new AddTypeEffect
        {
          Target = ObjectReference.It(),
          AddedSubtypes = [subtype],
        },
      },
    };

    effect = EffectWrap.Optional(
      new ConditionalPayEffect { Cost = new PayEnergyCost { Amount = LiteralQuantity.Of(energyCount) } },
      isOptional: true,
      ifYouDo: consequent
    );
    return true;
  }
}
