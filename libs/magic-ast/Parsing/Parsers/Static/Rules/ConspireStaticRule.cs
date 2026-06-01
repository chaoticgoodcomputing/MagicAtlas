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
/// Decomposes a "Conspire" oracle line into the two abilities defined by CR 702.78a
/// (mirroring the <see cref="DashStaticRule"/> / <see cref="ReconfigureStaticRule"/>
/// multi-ability precedent).
///
/// <para>
/// CR 702.78a (verbatim): "Conspire is a keyword that represents two abilities... 'Conspire'
/// means 'As an additional cost to cast this spell, you may tap two untapped creatures you
/// control that each share a color with it' and 'When you cast this spell, if its conspire
/// cost was paid, copy it. If the spell has any targets, you may choose new targets for the
/// copy.'"
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000) so the
/// two-ability decomposition takes precedence over the single-ability keyword combinator
/// path. The combinator in <see cref="MagicAST.Keywords.Definitions.ConspireKeyword"/>
/// remains live as a fallback, emitting only the primary additional-cost static ability
/// (the keyword expander returns a single <c>Ability</c>; Conspire decomposes into two).
/// </para>
///
/// <para>
/// The "each share a color with it" predicate on the tapped creatures is a relational
/// color filter (the creature shares a color with the spell being cast). No such
/// relational-color axis exists on <see cref="ObjectFilter"/> — its <c>Colors</c> axis
/// records absolute colors, not a "shares a color with" relation — so the refinement is
/// omitted per the descriptive-not-engine / no-free-text / no-new-axis contract; the
/// filter models only "creatures you control" with <c>Quantity 2</c>. The trigger's "you
/// may choose new targets for the copy" clause is engine territory and is likewise omitted
/// (copy once, no <c>Count</c>).
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class ConspireStaticRule : IStaticRule
{
  // Matches a bare "Conspire" line with optional trailing reminder text.
  // Conspire is parameterless (CR 702.78a) — no mana/cost symbols follow it.
  private static readonly Regex _pattern = new(
    @"^\s*Conspire\s*(?<reminder>\(.*\))?\s*$",
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

    Parenthetical? reminder = null;
    var reminderGroup = match.Groups["reminder"];
    if (reminderGroup.Success && reminderGroup.Value.Length > 0)
    {
      reminder = new Parenthetical { Text = reminderGroup.Value };
    }

    // Ability 1 (CR 702.78a, first clause): the optional additional-cost static ability.
    // "As an additional cost to cast this spell, you may tap two untapped creatures you
    // control that each share a color with it." The share-a-color relational predicate is
    // omitted (no ObjectFilter axis; see type-level remarks). The reminder text rides on
    // the primary ability (matching the combinator path).
    var costAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Conspire,
      Reminder = reminder,
      Effects =
      [
        new AdditionalCastCostEffect
        {
          AdditionalCost = new AdditionalCost
          {
            Cost = new TapPermanentsCost
            {
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Controller = ControllerFilter.You,
              },
              Quantity = LiteralQuantity.Of(2),
            },
            IsOptional = true,
          },
        },
      ],
    };

    // Ability 2 (CR 702.78a, second clause): the cast-copy triggered ability, gated on the
    // conspire cost having been paid. "When you cast this spell, if its conspire cost was
    // paid, copy it." Timing (When you cast this spell — the SpellCast event, subject Self)
    // lives on the trigger; the intervening-if references the conspire-cost-paid linked
    // ability (CR 702.78a). Copy once — no Count; "you may choose new targets" is engine.
    var copyTriggerAbility = new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Conspire,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.SpellCast,
        Filter = new ObjectFilter { Controller = ControllerFilter.You },
      },
      InterveningIf = new KeywordCostPaidCondition { Keyword = KeywordAbility.Conspire },
      Effects =
      [
        new CopyEffect { Target = ObjectReference.Self() },
      ],
    };

    return [costAbility, copyTriggerAbility];
  }
}
