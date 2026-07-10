namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Each other [Subtype] creature you control enters with an additional [counter]
/// counter on it." — a static continuous replacement effect (CR 614.1d) applying to
/// a class of OTHER permanents (a filtered subtype) as each one enters, rather than
/// to the source permanent itself. Canonical card: Oona's Blackguard.
///
/// <para>
/// CR 614.12 (verbatim, head): "Some replacement effects modify how a permanent
/// enters the battlefield. (See rules 614.1c-d.) Such effects may come from the
/// permanent itself if they affect only that permanent (as opposed to a general
/// subset of permanents that includes it). They may also come from other sources."
/// Here the source (this permanent, Oona's Blackguard) grants the replacement to a
/// GENERAL SUBSET of permanents (other Rogues the controller controls) — not to
/// itself — so this is <see cref="StaticTimingKind.AsObjectEnters"/> (CR 614.1d's
/// "[Objects] enter . . ." template), distinct from the self-only
/// <see cref="StaticTimingKind.AsThisEnters"/> family handled by
/// <see cref="EntersWithCountersRule"/>. The timing qualifier and the counter-put
/// effect remain separate composable nodes: the "when" (as each matching object
/// enters) lives on <see cref="StaticAbility.When"/>, never baked into the effect.
/// </para>
///
/// <para>
/// The subject noun phrase "each other [Subtype] creature you control" decomposes
/// onto the effect's <see cref="PutCountersEffect.Target"/> filter exactly like the
/// analogous triggered-side tribal shapes (mirrors
/// <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.OtherSubtypePumpTriggeredRule"/>):
/// <see cref="ObjectFilter.Subtypes"/> for the capitalised creature subtype (CR
/// 205.3m), <see cref="ObjectFilter.Controller"/> = You, and
/// <see cref="ObjectFilter.ExcludeSelf"/> = true for the "other" self-exclusion (CR
/// 109.5) — the source permanent's own entry is not affected by its own replacement.
/// The counter is put via the existing <see cref="PutCountersEffect"/> node (Count =
/// 1, CounterType as printed); "additional" describes that the counter stacks with
/// any other counter-granting replacement rather than requiring a separate AST flag
/// (mirrors <see cref="UnleashStaticRule"/>'s "an additional +1/+1 counter" reading).
/// </para>
///
/// <para>
/// New, collision-free file. ANCHORED (^…$) so it cannot steal any sibling oracle
/// line; requires the literal "Each other" self-exclusion prefix and a capitalised
/// subtype token, so it is disjoint from the self-only "[This/Name] enters with . . ."
/// family (<see cref="EntersWithCountersRule"/>) and from the unfiltered "[Objects]
/// enter [tapped/untapped]" mass shapes (<see cref="LandsYouControlEnterUntappedRule"/>,
/// the opponents-creatures arm of <see cref="EntersTappedRule"/>).
/// </para>
/// </summary>
[StaticRule(Priority = 973)]
public sealed class OtherSubtypeEntersWithCounterRule : IStaticRule
{
  // "Each other <Subtype> creature you control enters with an additional
  // <counterType> counter on it." The subtype token must be capitalised (creature
  // subtypes are proper nouns in oracle text, CR 205.3m); the counter type is
  // either a P/T pair ("+1/+1", "-1/-1") or a named counter word ("loyalty").
  private static readonly Regex _pattern = new(
    @"^\s*Each\s+other\s+(?<subtype>[A-Z][a-zA-Z]+)\s+creature\s+you\s+control\s+enters\s+with\s+an\s+additional\s+"
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

    var subtype = match.Groups["subtype"].Value;
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
                Subtypes = [subtype],
                Controller = ControllerFilter.You,
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
