namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "create a token that's a copy of this creature" — token-copy effect that fires
/// as the resolution clause of a <c>DealsCombatDamageToPlayer</c> trigger.
///
/// <para>
/// Rule 707.2: A token that is a copy of an object copies the copiable values of
/// that object. Rule 111.8: Such tokens are created on the battlefield under their
/// creator's control. The copy source is the card itself ("this creature"), modelled
/// as <see cref="ObjectReferenceKind.Self"/>.
/// </para>
///
/// <para>
/// Priority 70 — more specific than the generic <see cref="CreateTokenRule"/> (50)
/// so this rule is tried first and the generic rule never sees the "copy of this
/// creature" phrasing.
/// </para>
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class CreateCopyOnCombatDamageTriggeredRule : ITriggeredRule
{
  // "create a token that's a copy of this creature[.]"
  // Terminal period is stripped by the dispatcher before TryMatch is called.
  private static readonly Regex _pattern = new(
    @"^create\s+a\s+token\s+that(?:'s|'s)\s+a\s+copy\s+of\s+this\s+creature\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    // Rule 707.2: the token copies the copiable values of the source permanent.
    // The copy source is "this creature" — ObjectReferenceKind.Self.
    effect = new CopyEffect
    {
      Target = ObjectReference.Self(),
      IsOptional = false,
    };
    return true;
  }
}
