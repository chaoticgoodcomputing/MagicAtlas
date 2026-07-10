namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Self] deals damage equal to the sacrificed creature's power to any target." — the
/// resolution half of the Thud shape, whose additional cost (CR 601.2f: "As an additional
/// cost to cast this spell, sacrifice a creature.") is parsed independently by
/// <see cref="MagicAST.Parsing.AttributeExtractor"/> into a card-level
/// <see cref="MagicAST.AdditionalCostsAttribute"/> (the prose-cost line is stripped from
/// <c>Oracle.Abilities</c> by <see cref="MagicAST.Parsing.ClauseSplitter"/> before this rule
/// ever sees it — this rule only recognises the trailing spell-effect sentence).
///
/// <para>
/// The dealt amount is a <see cref="DerivedQuantity"/> keyed on <see cref="DerivedKind.Power"/>
/// whose <see cref="DerivedQuantity.Source"/> is the anaphoric "the sacrificed creature": the
/// object moved to the graveyard by this same spell's <see cref="MagicAST.AST.Costs.SacrificeCost"/>
/// (CR 701.21a). That back-reference is a CR 607.1 linked ability — the effect "directly
/// refers to … objects … that were affected by" the cost — so MAST records the textual link,
/// not a runtime power value (ADR 0004: reference-not-resolution). Mirrors the activated-ability
/// sibling <see cref="MagicAST.Parsing.Parsers.Activated.Rules.GainLifeEqualToSacrificedCreaturesToughnessEffectRule"/>
/// (Diamond Valley), which sources the SAME anaphoric "the sacrificed creature" phrase for a
/// toughness-keyed <c>GainLifeEffect</c> instead of a power-keyed <c>DealDamageEffect</c>.
/// </para>
///
/// <para>
/// The spell names itself as the damage source (CR 601.2c self-reference by printed name),
/// modeled as <see cref="ObjectReference.Self"/> — the same self-as-source convention as the
/// bare-amount sibling <see cref="SelfDealsDamageToAnyTargetRule"/>. "any target" is a creature,
/// player, planeswalker, or battle (CR 120.1), modeled as <see cref="ObjectReferenceKind.AnyTarget"/>.
/// </para>
///
/// <para>
/// Anchored (<c>^…$</c>) to the exact "deals damage equal to the sacrificed creature's power to
/// any target" surface so it cannot claim a substring of the bare-amount sibling
/// (<see cref="SelfDealsDamageToAnyTargetRule"/>, which requires a numeric/word amount, not a
/// derived one) or a future broader "deals damage equal to …'s power" pattern sourced from a
/// different anaphor (e.g. "its power", "that creature's power").
/// </para>
/// </summary>
[SpellRule]
public sealed class SelfDealsDamageEqualToSacrificedCreaturesPowerToAnyTargetRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^(?<subject>\S.*?)\s+deals\s+damage\s+equal\s+to\s+the\s+sacrificed\s+creature's\s+power\s+to\s+any\s+target$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var subject = m.Groups["subject"].Value;
    if (subject.Length == 0 || !char.IsUpper(subject[0]))
    {
      return false;
    }

    effect = new DealDamageEffect
    {
      Amount = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.Power,
        Source = "the sacrificed creature",
      },
      Source = ObjectReference.Self(),
      Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
    };
    return true;
  }
}
