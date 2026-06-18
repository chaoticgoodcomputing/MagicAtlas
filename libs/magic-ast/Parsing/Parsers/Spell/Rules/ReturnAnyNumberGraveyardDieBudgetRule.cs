namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Return any number of cards with total mana value X or less from your graveyard to your hand, where
/// X is the total of those results." — Pair o' Dice Lost. A graveyard recursion whose budget is a SET
/// cap on the total mana value of the chosen cards (CR 107.3), and the budget X is the die-roll total
/// (CR 706). Emits a <see cref="ReturnToHandEffect"/> targeting the controller's graveyard cards
/// (Kind = Designated — the player chooses which) with <see cref="ReturnToHandEffect.TotalManaValueBudget"/>
/// = the die-roll result.
///
/// <para>Anchored (^…$) to this exact shape so it cannot match a plain graveyard return. The link from X
/// to the preceding "Roll [N] dice" is the die-roll-result quantity; the engine reads the budget, not the
/// arithmetic.</para>
/// </summary>
[SpellRule(Priority = 70)]
public sealed class ReturnAnyNumberGraveyardDieBudgetRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Return\s+any\s+number\s+of\s+cards\s+with\s+total\s+mana\s+value\s+X\s+or\s+less\s+from\s+your\s+graveyard\s+to\s+your\s+hand,?\s+where\s+X\s+is\s+the\s+total\s+of\s+those\s+results$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Designated,
        Filter = new ObjectFilter
        {
          CardTypes = ["card"],
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
      TotalManaValueBudget = new DieRollResultQuantity(),
    };
    return true;
  }
}
