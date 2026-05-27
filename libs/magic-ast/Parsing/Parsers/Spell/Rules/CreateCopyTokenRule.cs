namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "Create a token that's a copy of target creature [you control|.]"
///
/// <para>
/// Maps the "create a token that's a copy of" pattern to <see cref="CopyEffect"/> with a
/// <see cref="ObjectReferenceKind.Target"/> reference. The token-vs-non-token distinction
/// is engine territory (Rule 706.10); MAST records the copy-of-target relationship
/// descriptively via <see cref="CopyEffect"/>.
/// </para>
///
/// <para>
/// Handles two variants:
/// <list type="bullet">
///   <item>"Create a token that's a copy of target creature you control." — Cackling Counterpart pattern</item>
///   <item>"Create a token that's a copy of target creature." — bare target with no controller clause</item>
/// </list>
/// </para>
/// </summary>
[SpellRule(Priority = 65)]
public sealed class CreateCopyTokenRule : ISpellRule
{
  // Matches:
  //   "Create a token that's a copy of target creature you control"
  //   "Create a token that's a copy of target creature"
  private static readonly Regex Pattern = new(
    @"^Create\s+a\s+token\s+that's\s+a\s+copy\s+of\s+target\s+creature(?:\s+(?<controller>you\s+control))?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var hasController = m.Groups["controller"].Success;

    effect = new CopyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = hasController ? ControllerFilter.You : null,
        },
      },
      IsOptional = false,
    };
    return true;
  }
}
