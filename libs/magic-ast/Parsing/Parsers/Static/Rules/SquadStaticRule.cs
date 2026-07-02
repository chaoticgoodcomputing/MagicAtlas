namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing;

/// <summary>
/// Decomposes a "Squad [cost]" oracle line into the two linked abilities defined by
/// CR 702.157a (mirroring the <see cref="DashStaticRule"/> / <see cref="ReconfigureStaticRule"/>
/// multi-ability precedent).
///
/// <para>
/// CR 702.157a (verbatim): "Squad is a keyword that represents two linked abilities...
/// 'Squad [cost]' means 'As an additional cost to cast this spell, you may pay [cost]
/// any number of times' and 'When this creature enters, if its squad cost was paid,
/// create a token that's a copy of it for each time its squad cost was paid.'"
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000) so the
/// two-ability decomposition takes precedence over the single-ability keyword
/// combinator path. The combinator in
/// <see cref="MagicAST.Keywords.Definitions.SquadKeyword"/> remains live as a fallback,
/// emitting only the primary additional-cost static ability (the keyword expander
/// returns a single <c>Ability</c>; Squad decomposes into two).
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class SquadStaticRule : IStaticRule
{
  // Matches: "Squad {cost}" with optional trailing reminder text.
  // The cost group captures one or more mana symbols, e.g. "{1}{G}".
  private static readonly Regex _pattern = new(
    @"^\s*Squad\s+(?<cost>(?:\{[^}]+\})+)\s*(?<reminder>\(.*\))?\s*$",
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

    // Ability 1 (CR 702.157a, first clause): the repeatable additional cost.
    // "As an additional cost to cast this spell, you may pay [cost] any number of
    // times." The reminder text rides on the primary ability (matching the
    // combinator path). The synthesized cost omits SourceSpan (identity rides on
    // KeywordSource, not a text frontier).
    var costAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Squad,
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

    // Ability 2 (CR 702.157a, second clause): the ETB token-copy trigger, gated on
    // the squad cost having been paid. "When this creature enters, if its squad cost
    // was paid, create a token that's a copy of it for each time its squad cost was
    // paid." The intervening-if and the count both reference the squad-cost-paid
    // linked datum (ADR 0003/0004 reference-not-resolution). "a copy of it" is the
    // entering creature itself — TokenDefinition expresses the copy via IsCopy = true.
    var tokenTriggerAbility = new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Squad,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.Enters,
      },
      InterveningIf = new KeywordCostPaidCondition { Keyword = KeywordAbility.Squad },
      Effects =
      [
        new CreateTokenEffect
        {
          Player = ObjectReference.You(),
          Count = new KeywordCostPaidCountQuantity { Keyword = KeywordAbility.Squad },
          Token = new TokenDefinition { IsCopy = true },
        },
      ],
    };

    return [costAbility, tokenTriggerAbility];
  }
}
