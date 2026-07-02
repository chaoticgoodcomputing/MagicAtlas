namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Up to N target creatures can't block this turn." — a bounded-count-target
/// blocking restriction (Abandon the Post). The count lives on
/// <see cref="ObjectReference.Quantity"/> as an <see cref="UpToQuantity"/>
/// (Rule 115.1 targeting; "up to N target" is a bounded target count, not an
/// effect-level count) and the restriction is scoped to the targeted creatures
/// via <see cref="ObjectReferenceKind.Target"/>, mirroring
/// <see cref="TapUpToNTargetsRule"/>'s up-to-N target-count plumbing.
/// The restriction lasts "this turn" — an <c>untilEndOfTurn</c> duration
/// (Rule 509.1: blocking restrictions resolve at the declare-blockers step of
/// the current turn), mirroring <see cref="CreaturesCantBlockThisTurnRule"/>'s
/// effect shape. Scoped to creatures only, since only creatures block.
/// </summary>
[SpellRule]
public sealed class UpToNTargetCreaturesCantBlockThisTurnRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Up\s+to\s+(?<n>\w+)\s+target\s+creatures?\s+can'?t\s+block\s+this\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    if (!SpellRuleHelpers.TryParseSmallWord(m.Groups["n"].Value, out var maximum))
    {
      return false;
    }

    effect = new CantBlockEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
        Quantity = new UpToQuantity { Maximum = maximum, Minimum = 0 },
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
