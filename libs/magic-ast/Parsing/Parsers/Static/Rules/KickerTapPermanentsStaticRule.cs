namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Decomposes a "Kicker—Tap an untapped [Subtype] you control." oracle line (em-dash
/// kicker with a tap-permanents non-mana cost, CR 702.33a) into a single
/// <see cref="StaticAbility"/> carrying an <see cref="AdditionalCastCostEffect"/> with
/// a <see cref="TapPermanentsCost"/>.
///
/// <para>
/// CR 702.33a (verbatim): "Kicker is a static ability that functions while the spell
/// with kicker is on the stack. 'Kicker [cost]' means 'You may pay an additional [cost]
/// as you cast this spell.' Paying a spell's kicker cost(s) follows the rules for paying
/// additional costs in rules 601.2b and 601.2f-h."
/// </para>
///
/// <para>
/// The em-dash separator ("Kicker—") is the Wizards printing convention for non-mana
/// kicker costs (e.g. Blood Tribute: "Kicker—Tap an untapped Vampire you control.").
/// The <see cref="KickerKeyword"/> combinator handles the mana-symbol form
/// ("Kicker {cost}") via the tokenised Superpower pipeline; this rule intercepts the
/// em-dash + tap-permanents shape before <see cref="KeywordListRule"/> fires so the
/// full cost is preserved as a typed <see cref="TapPermanentsCost"/>.
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000). The
/// pattern is anchored (^…$) to avoid substring-matching inside a more-specific sibling.
/// Requires the cost phrase to match "Tap an untapped [Subtype] you control" exactly
/// (case-insensitive); the subtype is captured and used as the filter criterion.
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class KickerTapPermanentsStaticRule : IStaticRule
{
  /// <summary>
  /// Matches: "Kicker—Tap an untapped [Subtype] you control. (optional reminder)"
  /// The subtype is a single word (e.g., "Vampire", "Warrior", "Cleric").
  /// </summary>
  private static readonly Regex _pattern = new(
    @"^\s*Kicker—Tap\s+an\s+untapped\s+(?<subtype>[A-Z][A-Za-z]+)\s+you\s+control\s*\.?\s*(?<reminder>\(.*\))?\s*$",
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

    var subtype = match.Groups["subtype"].Value;

    Parenthetical? reminder = null;
    var reminderGroup = match.Groups["reminder"];
    if (reminderGroup.Success && reminderGroup.Value.Length > 0)
    {
      reminder = new Parenthetical { Text = reminderGroup.Value };
    }

    // CR 702.33a: "Kicker [cost]" means "You may pay an additional [cost] as you cast
    // this spell." The cost is a TapPermanentsCost — tap one untapped [Subtype] you
    // control. IsOptional = true ("you may"), Repeatable = false (at most once — Kicker,
    // not Multikicker). The synthesized AdditionalCost omits SourceSpan; identity rides
    // on this ability's KeywordSource (Kicker).
    var costAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Kicker,
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
                Subtypes = [subtype],
                Controller = ControllerFilter.You,
              },
              Quantity = LiteralQuantity.Of(1),
            },
            IsOptional = true,
          },
        },
      ],
    };

    return [costAbility];
  }
}
