namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing;

/// <summary>
/// Decomposes a "Replicate [cost]" oracle line into the two abilities defined by
/// CR 702.56a (mirroring the <see cref="ReconfigureStaticRule"/> / <see cref="DashStaticRule"/>
/// multi-ability precedent).
///
/// <para>
/// CR 702.56a (verbatim): "Replicate is a keyword that represents two abilities... 'Replicate
/// [cost]' means 'As an additional cost to cast this spell, you may pay [cost] any number of
/// times' and 'When you cast this spell, if a replicate cost was paid for it, copy it for each
/// time its replicate cost was paid. If the spell has any targets, you may choose new targets
/// for any of the copies.'"
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000) so the
/// two-ability decomposition takes precedence over the single-ability keyword combinator
/// path. The combinator in <see cref="MagicAST.Keywords.Definitions.ReplicateKeyword"/>
/// remains live as a fallback, emitting only the primary additional-cost static ability (the
/// keyword expander returns a single <c>Ability</c>; Replicate decomposes into two).
/// </para>
///
/// <para>
/// "you may choose new targets for any of the copies" is engine territory (target reselection)
/// and is omitted per the descriptive-not-engine doctrine.
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class ReplicateStaticRule : IStaticRule
{
  // Matches: "Replicate {cost}" with optional trailing reminder text.
  // The cost group captures one or more mana symbols, e.g. "{1}{U}".
  private static readonly Regex _pattern = new(
    @"^\s*Replicate\s+(?<cost>(?:\{[^}]+\})+)\s*(?<reminder>\(.*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var costStr = match.Groups["cost"].Value;
    ManaCost cost;
    try
    {
      var parsed = new ManaCostParser().Parse(costStr);
      if (parsed.Symbols.Count == 0)
      {
        return null;
      }
      cost = new ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }

    Parenthetical? reminder = null;
    var reminderGroup = match.Groups["reminder"];
    if (reminderGroup.Success && reminderGroup.Value.Length > 0)
    {
      reminder = new Parenthetical { Text = reminderGroup.Value };
    }

    // Ability 1 (CR 702.56a, first clause): the repeatable additional-cost static ability.
    // "As an additional cost to cast this spell, you may pay [cost] any number of times." The
    // reminder text rides on the primary ability (matching the combinator path). The
    // synthesized cost carries no SourceSpan — identity rides on KeywordSource.
    var costAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Replicate,
      Reminder = reminder,
      Effects =
      [
        new AdditionalCastCostEffect
        {
          AdditionalCost = new AdditionalCost
          {
            Cost = cost,
            IsOptional = true,
            Repeatable = true,
          },
        },
      ],
    };

    // Ability 2 (CR 702.56a, second clause): the cast-copy triggered ability, gated on the
    // replicate cost having been paid. "When you cast this spell, if a replicate cost was paid
    // for it, copy it for each time its replicate cost was paid." Timing lives on the
    // SpellCast trigger (CR 603); the intervening-if references the keyword's typed identity
    // (CR 702.56a's "if a replicate cost was paid"); the copy count references the times-paid
    // count of the same keyword ("for each time its replicate cost was paid").
    var copyAbility = new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Replicate,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.SpellCast,
      },
      InterveningIf = new KeywordCostPaidCondition { Keyword = KeywordAbility.Replicate },
      Effects =
      [
        new CopyEffect
        {
          Target = ObjectReference.Self(),
          Count = new KeywordCostPaidCountQuantity { Keyword = KeywordAbility.Replicate },
        },
      ],
    };

    return [costAbility, copyAbility];
  }
}
