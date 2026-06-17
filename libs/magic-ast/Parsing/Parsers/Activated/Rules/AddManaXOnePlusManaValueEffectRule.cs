namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;

/// <summary>
/// "Add X mana of any one color, where X is 1 plus the exiled creature's mana value.
///  Spend this mana only to cast creature spells." — Food Chain's exile-creature mana
///  production with a spend restriction.
///
/// <para>The X quantity is a derived calculation: 1 plus the exiled creature's mana
/// value. MAST models this as a <see cref="CalculatedQuantity"/> whose
/// <see cref="CalculatedQuantity.BaseQuantity"/> is a <see cref="DerivedQuantity"/>
/// keyed on <see cref="DerivedKind.ManaValue"/> with <c>Source = "exiled creature"</c>
/// (reference-not-resolution per ADR 0004 — MAST names the source object, it does
/// not thread the runtime value). <c>Operation = "add"</c>, <c>Operand = 1</c>.</para>
///
/// <para>The spend restriction ("Spend this mana only to cast creature spells") is
/// captured verbatim in <see cref="AddManaEffect.SpendRestriction"/> per the existing
/// MAST doctrine: MAST describes, does not execute; the restriction text is the
/// engine's domain, not the AST's.</para>
///
/// <para>Per CR 605.1a — "An activated ability is a mana ability if it meets all of
/// the following criteria: it doesn't require a target, it could add mana to a player's
/// mana pool when it resolves, and it's not a loyalty ability." — this ability is a
/// mana ability despite its spend restriction (a spend restriction does not make an
/// ability non-mana; cf. Unclaimed Territory, Secluded Courtyard).</para>
///
/// <para>This rule fires at Priority 1001 (above the existing
/// <c>AddManaEffectRule</c>'s 1000) so it claims the Food Chain combined string
/// before the base rule's <c>UnmodeledManaClause</c> bail would swallow it.</para>
/// </summary>
[ActivatedEffectRule(Priority = 1001)]
public sealed class AddManaXOnePlusManaValueEffectRule : IActivatedEffectRule
{
  // Matches the full combined text that arrives when TryParseMultiEffectSentences
  // fails (because "Spend this mana only ..." has no standalone rule):
  //   "Add X mana of any one color, where X is 1 plus the exiled creature's mana value.
  //    Spend this mana only to cast creature spells."
  // The spend-restriction clause is captured verbatim into <restriction>.
  private static readonly Regex _fullPattern = new(
    @"^Add\s+X\s+mana\s+of\s+any\s+one\s+color,\s+where\s+X\s+is\s+1\s+plus\s+the\s+exiled\s+creature's\s+mana\s+value"
    + @"(?:[.\s]+Spend\s+this\s+mana\s+only\s+to\s+(?<restriction>.+?))?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    var match = _fullPattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var restrictionGroup = match.Groups["restriction"];
    var spendRestriction = restrictionGroup.Success
      ? restrictionGroup.Value.Trim().TrimEnd('.')
      : null;

    return new AddManaEffect
    {
      Mana = string.Empty,
      AnyColor = true,
      Amount = new CalculatedQuantity
      {
        BaseQuantity = new DerivedQuantity
        {
          DerivedFrom = DerivedKind.ManaValue,
          Source = "exiled creature",
        },
        Operation = "add",
        Operand = 1,
      },
      SpendRestriction = spendRestriction,
    };
  }
}
