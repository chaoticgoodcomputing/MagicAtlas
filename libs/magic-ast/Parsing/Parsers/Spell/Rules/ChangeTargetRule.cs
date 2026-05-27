namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Change the target of target spell with a single target."
/// Rule 115.7 — the controller of this spell or ability may choose a new legal target
/// for the targeted spell. Covers Divert, Misdirection, and functional equivalents.
/// </summary>
[SpellRule]
public sealed class ChangeTargetRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Change the target of target spell with a single target\.?$",
    RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text))
    {
      return false;
    }

    effect = new ChangeTargetEffect
    {
      Spell = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new()
        {
          CardTypes = ["spell"],
          Characteristics = ["single target"],
        },
      },
    };
    return true;
  }
}
