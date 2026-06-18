namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "you may create a token that's a copy of it" — the optional constellation-style
/// token-copy effect found on cards such as Ondu Spiritdancer.
///
/// <para>
/// The pronoun "it" is a back-reference (CR 113.8b) to the object named by the
/// trigger condition — the entering enchantment. MAST models this as
/// <see cref="ObjectReferenceKind.It"/>, consistent with other "copy it" rules
/// such as <see cref="CopyTriggeringSpellTriggeredRule"/>.
/// </para>
///
/// <para>
/// The "you may" prefix means the controller may choose not to copy. MAST
/// wraps the <see cref="CopyEffect"/> in an <see cref="OptionalEffect"/> per
/// ADR 0005 (clause-modifier composition). No <c>IfYouDo</c> is present
/// (the copy is the sole consequence).
/// </para>
///
/// <para>
/// CR 707.1: "Some effects create a token that's a copy of another object."
/// CR 111.2: "The player who creates a token is its owner."
/// CR 603.2h: "A triggered ability may have an instruction followed by 'Do
/// this only once each turn.'" — the restriction is peeled from the effect
/// text by <see cref="TriggeredAbilityParser"/> before this rule is invoked;
/// this rule handles only the single-sentence body.
/// </para>
///
/// <para>
/// ANCHORED (^…$): prevents matching inside a more-specific sibling whose text
/// also contains "create a token that's a copy of it" as a substring.
/// Priority 72 — above the generic <see cref="CreateCopyOnCombatDamageTriggeredRule"/>
/// (70) and well above the vanilla token rule (50); specific enough to be tried
/// before any broader "you may create" path.
/// </para>
/// </summary>
[TriggeredRule(Priority = 72)]
public sealed class YouMayCreateTokenCopyOfItTriggeredRule : ITriggeredRule
{
  // "you may create a token that's a copy of it"
  // Terminal period is stripped by the dispatcher before TryMatch is called.
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+create\s+a\s+token\s+that(?:'s|'s)\s+a\s+copy\s+of\s+it$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Inner = new CopyEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.It },
      },
    };
    return true;
  }
}
