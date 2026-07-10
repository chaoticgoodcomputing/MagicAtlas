namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Other creatures you control enter with an additional [counter] counter on
/// them for each [filter]." — the per-object VARIABLE-count sibling of
/// <see cref="OtherSubtypeEntersWithCounterRule"/> (a fixed +1 scoped to a
/// chosen/printed subtype): here the counter amount itself scales with a
/// "for each" count rather than being a flat 1, and no subtype restricts the
/// affected creatures. Gev, Scaled Scorch: "Other creatures you control enter
/// with an additional +1/+1 counter on them for each opponent who lost life
/// this turn."
///
/// <para>
/// CR 614.12 (verbatim, head): "Some replacement effects modify how a permanent
/// enters the battlefield. (See rules 614.1c-d.) Such effects may come from the
/// permanent itself if they affect only that permanent (as opposed to a general
/// subset of permanents that includes it). They may also come from other
/// sources." The source (Gev) grants the replacement to a GENERAL SUBSET of
/// permanents (other creatures the controller controls) — not to itself — so
/// this is <see cref="StaticTimingKind.AsObjectEnters"/> (CR 614.1d), matching
/// <see cref="OtherSubtypeEntersWithCounterRule"/>'s own citation. Unlike that
/// sibling, the plural "Other creatures you control" carries no subtype
/// qualifier, so the affected-object filter is a bare
/// <c>CardTypes=["creature"], Controller=You, ExcludeSelf=true</c> — no
/// <see cref="ObjectFilter.Subtypes"/>/<see cref="ObjectFilter.ChosenCharacteristic"/>.
/// </para>
///
/// <para>
/// The "for each opponent who lost life this turn" count reuses
/// <see cref="CountQuantity"/> (a count of objects matching a filter, per
/// <see cref="EntersWithCounterPerFilterRule"/>'s established convention) over a
/// PLAYER-scoped filter: <c>EntityType="player", Controller=Opponent</c> (the
/// <see cref="EnchantRule"/> convention for "player" filters), restricted by the
/// new <see cref="LostLifeThisTurnPredicate"/> backward-looking predicate — CR
/// 119.3: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly." (the same life-loss baseline
/// cited throughout the codebase's other life-loss rules).
/// </para>
///
/// <para>
/// New, collision-free file. ANCHORED (^…$) to the literal "Other creatures you
/// control enter with an additional ... on them for each ..." head, so it
/// cannot steal any sibling oracle line — distinct from the "Each other
/// [Subtype] creature you control enters ... on it." shape
/// (<see cref="OtherSubtypeEntersWithCounterRule"/>, singular "enters"/"it",
/// fixed count) and from the self-only "[This/Name] enters with a ... counter
/// ... for each ..." shape (<see cref="EntersWithCounterPerFilterRule"/>,
/// singular subject/"on it"). Only the single "opponent who lost life this
/// turn" filter phrase is recognised today; an unrecognised filter phrase falls
/// through to the unparsed fallback, matching
/// <see cref="EntersWithCounterPerFilterRule"/>'s own gap-reporting convention.
/// </para>
/// </summary>
[StaticRule(Priority = 974)]
public sealed class OtherCreaturesEnterWithCounterPerOpponentLostLifeRule : IStaticRule
{
  // "Other creatures you control enter with an additional <counterType>
  // counter(s) on them for each <filter>." The counter type is either a P/T
  // pair ("+1/+1", "-1/-1") or a named counter word.
  private static readonly Regex _pattern = new(
    @"^\s*Other\s+creatures\s+you\s+control\s+enter\s+with\s+an\s+additional\s+"
    + @"(?<counterType>[+-]\d+/[+-]\d+|[a-zA-Z]+)\s+counters?\s+on\s+them\s+for\s+each\s+(?<filter>.+?)\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(StaticRuleHelpers.StripReminderText(clause.RawText));
    if (!match.Success)
    {
      return null;
    }

    var counterType = match.Groups["counterType"].Value;
    var filterPhrase = match.Groups["filter"].Value.Trim().ToLowerInvariant();

    ObjectFilter? countOf = filterPhrase switch
    {
      // "opponent who lost life this turn" (Gev, Scaled Scorch).
      "opponent who lost life this turn" => new ObjectFilter
      {
        EntityType = "player",
        Controller = ControllerFilter.Opponent,
        History = new LostLifeThisTurnPredicate(),
      },
      _ => null,
    };

    if (countOf is null)
    {
      // Unrecognised filter phrase — let the fallback record the gap.
      return null;
    }

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsObjectEnters,
        Effects =
        [
          new PutCountersEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Controller = ControllerFilter.You,
                ExcludeSelf = true,
              },
            },
            CounterType = counterType,
            Count = new CountQuantity { CountOf = countOf },
          },
        ],
      },
    ];
  }
}
