namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "[Name] enters with X &lt;counterType&gt; counters on it, where X is the
/// number of &lt;filter&gt;." — the X-DEFINED-BY-A-TRAILING-CLAUSE sibling of
/// <see cref="EntersWithCounterPerFilterRule"/> (whose count phrase is always
/// the singular "a &lt;counterType&gt; counter ... for each &lt;filter&gt;",
/// with no "X"/"where" wording at all) and of <see cref="EntersWithCountersRule"/>
/// (whose bare "X" has no defining tail, so its pattern is anchored to end at
/// "counters? on it" with nothing after). Stag Beetle: "This creature enters
/// with X +1/+1 counters on it, where X is the number of other creatures on
/// the battlefield."
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
/// "the number of other creatures on the battlefield" is modelled as
/// <see cref="CountQuantity"/> over an <see cref="ObjectFilter"/> — a count of
/// discrete objects, not a scalar characteristic of a single object — exactly
/// as <see cref="EntersWithCounterPerFilterRule"/> and
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.ModifyPTXCountSubtypeSpellRule"/>
/// both do for their own "where X is the number of ..." tails. "other" is the
/// filter-level self-exclusion axis (<see cref="ObjectFilter.ExcludeSelf"/>,
/// CR 109.5), and "on the battlefield" carries no controller restriction — the
/// same "every matching object regardless of controller" reading
/// <see cref="ModifyPTXCountSubtypeSpellRule"/> documents for its own "on the
/// battlefield" tail (<see cref="Zone.Battlefield"/> with no
/// <see cref="ObjectFilter.Controller"/>).
/// </para>
///
/// <para>
/// The counter placement itself reuses <see cref="MagicAST.AST.Effects.Counter.PutCountersEffect"/>
/// exactly as <see cref="EntersWithCountersRule"/> does, with
/// <see cref="MagicAST.AST.Effects.Counter.PutCountersEffect.Count"/> set to the
/// <see cref="CountQuantity"/> instead of a <see cref="VariableQuantity"/>.
/// </para>
///
/// <para>
/// ANCHORED (^…$) and specific to the "X &lt;counterType&gt; counters on it,
/// where X is the number of &lt;filter&gt;" surface — the ", where X is the
/// number of" tail keeps it disjoint from <see cref="EntersWithCountersRule"/>
/// (anchored to end at "counters? on it", no trailing clause) and from
/// <see cref="EntersWithCounterPerFilterRule"/> (singular "a ... counter ...
/// for each", no "X"/"where" wording). Only the single "other creatures on the
/// battlefield" filter phrase is recognised today; an unrecognised filter
/// phrase falls through to the unparsed fallback (matching
/// <see cref="EntersWithCounterPerFilterRule"/>'s own gap-reporting convention)
/// rather than being guessed at. Priority 950 matches the sibling rules'
/// specificity tier.
/// </para>
/// </summary>
[StaticRule(Priority = 950)]
public sealed class EntersWithVariableCountersWhereXCountRule : IStaticRule
{
  // "[Name] enters with X <counterType> counters on it, where X is the number
  // of <filter>." — the subject prefix is captured liberally (any non-empty
  // leading words before "enters with"), matching the self-reference
  // convention established by EntersWithCountersRule.
  private static readonly Regex _pattern = new(
    @"^\s*\S.+?\s+enters\s+with\s+X\s+(?<counterType>[\w/+-]+)\s+counters\s+on\s+it,\s+where\s+X\s+is\s+the\s+number\s+of\s+(?<filter>.+?)\.?\s*$",
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
      // "other creatures on the battlefield" (Stag Beetle) — "other" excludes
      // the entering permanent itself (CR 109.5); no controller restriction,
      // so every creature on the battlefield counts, regardless of who
      // controls it.
      "other creatures on the battlefield" => new ObjectFilter
      {
        CardTypes = ["creature"],
        ExcludeSelf = true,
        Zone = Zone.Battlefield,
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
