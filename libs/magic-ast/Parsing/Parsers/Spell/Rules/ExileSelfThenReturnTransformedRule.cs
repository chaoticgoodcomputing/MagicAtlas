namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile this [permanent], then return it to the battlefield transformed under
/// your control." — the final chapter of a transforming Saga (The Legend of Roku,
/// chapter III). Returns the flat two-effect list [exile{Self}, returnToBattlefield].
///
/// <para>
/// The "it" in the second clause back-references the exiled permanent, which
/// re-enters transformed (back face up, CR 712) under the controller's control.
/// CR 701.13 (exile); CR 712 (transform); CR 715 (Saga). The exile-then-return is
/// the standard flicker-to-transform idiom; the permanent's identity across the
/// zone change is engine bookkeeping.
/// </para>
/// </summary>
[SpellRule]
public sealed class ExileSelfThenReturnTransformedRule : ISpellRule, IMultiSpellRule
{
  private static readonly Regex Pattern = new(
    @"^Exile\s+this\s+\w+,\s*then\s+return\s+it\s+to\s+the\s+battlefield\s+transformed\s+under\s+your\s+control$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Single-effect path declines so the multi-effect path fires (per IMultiSpellRule contract).
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    if (!Pattern.IsMatch(text.Trim().TrimEnd('.')))
    {
      return false;
    }

    effects =
    [
      new ExileEffect { Target = ObjectReference.Self() },
      new ReturnToBattlefieldEffect
      {
        Target = ObjectReference.Self(),
        UnderControl = ObjectReference.You(),
        Transformed = true,
      },
    ];
    return true;
  }
}
