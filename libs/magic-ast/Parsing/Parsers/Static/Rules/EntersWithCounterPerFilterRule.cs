namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "[Name] enters with a +1/+1 counter on it for each [filter]." — the
/// per-object-count sibling of <see cref="EntersWithCountersRule"/> (a
/// literal/word/X count) and <see cref="EntersWithCountersEqualToManaSpentRule"/>
/// (a derived mana-spent count). Golgari Grave-Troll: "This creature enters
/// with a +1/+1 counter on it for each creature card in your graveyard."
///
/// <para>
/// CR 614.12 (verbatim, head): "Some replacement effects modify how a permanent
/// enters the battlefield. (See rules 614.1c-d.) Such effects may come from the
/// permanent itself if they affect only that permanent (as opposed to a general
/// subset of permanents that includes it). They may also come from other sources."
/// The replacement affects only the permanent itself (a self-only "This creature
/// enters with..." reading), so <see cref="StaticTimingKind.AsThisEnters"/> is
/// used, matching <see cref="EntersWithCountersRule"/>'s own citation.
/// </para>
///
/// <para>
/// "for each creature card in your graveyard" is modelled as
/// <see cref="CountQuantity"/> — a count of objects matching an
/// <see cref="MagicAST.AST.References.ObjectFilter"/> — rather than a derived
/// characteristic, since the count is over discrete objects (cards in a zone),
/// not a scalar characteristic of a single object. The filter shape
/// (<c>CardTypes: ["creature"], Controller: You, Zone: Graveyard</c>) reuses the
/// same "creature card in your graveyard" convention as
/// <see cref="CostReductionForEachRule"/>'s per-object cost-reduction filter.
/// </para>
///
/// <para>
/// The counter placement itself reuses <see cref="MagicAST.AST.Effects.Counter.PutCountersEffect"/>
/// exactly as <see cref="EntersWithCountersRule"/> does, with
/// <see cref="MagicAST.AST.Effects.Counter.PutCountersEffect.Count"/> set to the
/// <see cref="CountQuantity"/> instead of a <see cref="LiteralQuantity"/>/<see cref="VariableQuantity"/>.
/// </para>
///
/// <para>
/// ANCHORED (^…$) and specific to the "a &lt;counterType&gt; counter on it for
/// each &lt;filter&gt;" surface — the "for each" suffix keeps it disjoint from
/// <see cref="EntersWithCountersRule"/> (anchored to end at "counters? on it")
/// and from <see cref="EntersWithCountersEqualToManaSpentRule"/> (anchored to the
/// "equal to the amount of mana spent to cast it" suffix). Only the single
/// "creature card in your graveyard" filter phrase is recognised today; an
/// unrecognised filter phrase falls through to the unparsed fallback (matching
/// <see cref="CostReductionForEachRule"/>'s own gap-reporting convention) rather
/// than being guessed at. Priority 950 matches the sibling rules' specificity
/// tier.
/// </para>
/// </summary>
[StaticRule(Priority = 950)]
public sealed class EntersWithCounterPerFilterRule : IStaticRule
{
  // "[Name] enters with a <counterType> counter on it for each <filter>." — the
  // subject prefix is captured liberally (any non-empty leading words before
  // "enters with"), matching the self-reference convention established by
  // EntersWithCountersRule. The count phrase here is always singular ("a ...
  // counter"), since the actual quantity is the per-filter count, not a literal.
  private static readonly Regex _pattern = new(
    @"^\s*\S.+?\s+enters\s+with\s+a\s+(?<counterType>[\w/+-]+)\s+counter\s+on\s+it\s+for\s+each\s+(?<filter>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
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
      // "creature card in your graveyard" (Golgari Grave-Troll) — same shape as
      // CostReductionForEachRule's per-object filter for the identical phrase.
      "creature card in your graveyard" => new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = ControllerFilter.You,
        Zone = Zone.Graveyard,
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
        When = StaticTimingKind.AsThisEnters,
        Effects = [new MagicAST.AST.Effects.Counter.PutCountersEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
          Count = new CountQuantity { CountOf = countOf },
          CounterType = counterType,
        }],
      },
    ];
  }
}
