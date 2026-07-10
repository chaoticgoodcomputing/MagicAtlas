namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever an opponent casts their first noncreature spell each turn" (Shadow
/// in the Warp) — the ordinal-qualified spell-cast trigger (CR 601: Casting
/// Spells; CR 603.2: the event-match is the trigger). Sibling of
/// <see cref="DrawNthCardEachTurnConditionRule"/>: the oracle text names a
/// specific occurrence within the turn ("their <i>first</i> ... spell") and a
/// per-turn counting window ("each turn"), recorded descriptively on
/// <see cref="TriggerCondition.Ordinal"/>/<see cref="TriggerCondition.PerTurn"/>
/// rather than any turn-state counting machinery — the ordinal merely narrows
/// which cast-event match counts, it does not model the tally itself.
///
/// <para>
/// Distinct from the unqualified <see cref="SpellCastConditionRule"/> ("Whenever
/// [x] casts a spell", no ordinal/per-turn qualifier). Priority 999 (just above
/// SpellCastConditionRule's 998) so this more specific ordinal shape is tried
/// first and doesn't get swallowed by the generic one. "Noncreature" maps to
/// the existing <c>ExcludedCardTypes=["creature"]</c> negation axis, matching
/// how <see cref="SpellCastConditionRule"/> already encodes it.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 999)]
public sealed class CastsNthSpellEachTurnConditionRule : ITriggerConditionRule
{
  // Maps the ordinal words MTG oracle text uses for per-turn cast triggers to
  // their numeric value, mirroring DrawNthCardEachTurnConditionRule's table.
  private static readonly IReadOnlyDictionary<string, int> _ordinals = new Dictionary<
    string,
    int
  >(StringComparer.OrdinalIgnoreCase)
  {
    ["first"] = 1,
    ["second"] = 2,
    ["third"] = 3,
    ["fourth"] = 4,
    ["fifth"] = 5,
  };

  // "[subject] casts (your|their|his|her) <ordinal> [type-qualifier] spell each
  // turn" — the per-turn ordinal spell-cast shape. The "each turn" qualifier is
  // required so the unqualified "[x] casts a spell" (SpellCastConditionRule)
  // keeps owning its shape.
  private static readonly Regex _pattern = new(
    @"\b(?<subject>you|an?\s+opponent|an?\s+player)\s+casts?\s+(?:your|their|his|her)\s+(?<ordinal>\w+)\s+"
      + @"(?:(?<qualifier>noncreature|creature|instant|sorcery|artifact|enchantment|planeswalker|land|battle)\s+)?spell\s+each\s+turn\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("cast") || !lower.Contains("spell") || !lower.Contains("each turn"))
    {
      return null;
    }

    var match = _pattern.Match(triggerText);
    if (!match.Success)
    {
      return null;
    }

    if (!_ordinals.TryGetValue(match.Groups["ordinal"].Value, out var ordinal))
    {
      return null;
    }

    var subject = match.Groups["subject"].Value;
    var controller = subject.Contains("opponent", StringComparison.OrdinalIgnoreCase)
      ? ControllerFilter.Opponent
      : subject.Equals("you", StringComparison.OrdinalIgnoreCase)
        ? ControllerFilter.You
        : ControllerFilter.Any;

    var qualifier = match.Groups["qualifier"].Success
      ? match.Groups["qualifier"].Value.ToLowerInvariant()
      : null;

    var filter = qualifier switch
    {
      "noncreature" => new ObjectFilter
      {
        CardTypes = ["spell"],
        ExcludedCardTypes = ["creature"],
        Controller = controller,
      },
      null => new ObjectFilter { Controller = controller },
      _ => new ObjectFilter { CardTypes = ["spell", qualifier], Controller = controller },
    };

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.SpellCast,
      Filter = filter,
      Ordinal = ordinal,
      PerTurn = true,
    };
  }
}
