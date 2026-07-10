namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Each other creature you control of the chosen type enters with an additional
/// +1/+1 counter on it." — the runtime-chosen-type sibling of
/// <see cref="OtherSubtypeEntersWithCounterRule"/> (Oona's Blackguard's printed
/// subtype), scoped instead by <see cref="ObjectFilter.ChosenCharacteristic"/>
/// (Metallic Mimic shape, paired with "As this creature enters, choose a creature
/// type." and "This creature is the chosen type in addition to its other types.").
///
/// <para>
/// CR 614.12 (verbatim, head): "Some replacement effects modify how a permanent
/// enters the battlefield. (See rules 614.1c-d.) Such effects may come from the
/// permanent itself if they affect only that permanent (as opposed to a general
/// subset of permanents that includes it). They may also come from other sources."
/// Here the source (this permanent) grants the replacement to a GENERAL SUBSET of
/// permanents (other creatures the controller controls of the chosen type) — not to
/// itself — so this is <see cref="StaticTimingKind.AsObjectEnters"/> (CR 614.1d's
/// "[Objects] enter . . ." template), matching
/// <see cref="OtherSubtypeEntersWithCounterRule"/>'s timing reading, distinct from
/// the self-only <see cref="StaticTimingKind.AsThisEnters"/> family
/// (<see cref="EntersWithCountersRule"/>). The timing qualifier and the counter-put
/// effect remain separate composable nodes: the "when" (as each matching object
/// enters) lives on <see cref="StaticAbility.When"/>, never baked into the effect.
/// </para>
///
/// <para>
/// The subject noun phrase "each other creature you control of the chosen type"
/// decomposes onto the effect's <see cref="PutCountersEffect.Target"/> filter exactly
/// like the analogous printed-subtype anthem sibling
/// <see cref="OtherChosenTypeAnthemModifyPTRule"/>: <see cref="ObjectFilter.CardTypes"/>
/// = ["creature"], <see cref="ObjectFilter.Controller"/> = You,
/// <see cref="ObjectFilter.ChosenCharacteristic"/> = <see cref="ChosenCharacteristicKind.CreatureType"/>
/// for the CR 607.1 linked-ability reference to the "As this creature enters, choose
/// a creature type." producer, and <see cref="ObjectFilter.ExcludeSelf"/> = true for
/// the "other" self-exclusion (CR 109.5) — the source permanent's own entry is not
/// affected by its own replacement. The counter is put via the existing
/// <see cref="PutCountersEffect"/> node (Count = 1, CounterType as printed);
/// "additional" describes that the counter stacks with any other counter-granting
/// replacement rather than requiring a separate AST flag (mirrors
/// <see cref="OtherSubtypeEntersWithCounterRule"/>'s reading of the same word).
/// </para>
///
/// <para>
/// New, collision-free file. ANCHORED (^…$) so it cannot steal any sibling oracle
/// line; requires the literal "Each other creature you control of the chosen type"
/// phrase, so it is disjoint from the printed-subtype family
/// (<see cref="OtherSubtypeEntersWithCounterRule"/>, which requires a capitalised
/// subtype token immediately after "other") and from the self-only "[This/Name]
/// enters with . . ." family (<see cref="EntersWithCountersRule"/>).
/// </para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class ChosenTypeEntersWithCounterRule : IStaticRule
{
  // "Each other creature you control of the chosen type enters with an additional
  // <counterType> counter on it." The counter type is either a P/T pair
  // ("+1/+1", "-1/-1") or a named counter word ("loyalty").
  private static readonly Regex _pattern = new(
    @"^\s*Each\s+other\s+creature\s+you\s+control\s+of\s+the\s+chosen\s+type\s+enters\s+with\s+an\s+additional\s+"
    + @"(?<counterType>[+-]\d+/[+-]\d+|[a-zA-Z]+)\s+counters?\s+on\s+it\.?\s*$",
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
                ChosenCharacteristic = ChosenCharacteristicKind.CreatureType,
                ExcludeSelf = true,
              },
            },
            CounterType = counterType,
            Count = LiteralQuantity.Of(1),
          },
        ],
      },
    ];
  }
}
