namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Smothering Tithe pattern: "that player may pay {COST}. If the player doesn't,
/// you create a Treasure token." (CR 603 — triggered on opponent draw;
/// CR 117.7 — the paying player may pay; the IfYouDoNot branch creates for you).
///
/// <para>
/// Decomposes as:
/// <list type="bullet">
///   <item><see cref="OptionalEffect"/> — the "may pay" wrapper.</item>
///   <item><see cref="ConditionalPayEffect"/> as <c>Inner</c> — the cost
///     "that player" may pay. <see cref="ConditionalPayEffect.Player"/> is
///     <see cref="ObjectReferenceKind.ThatPlayer"/> (the opponent who drew the
///     card, per the trigger condition).</item>
///   <item><see cref="CreateTokenEffect"/> as <c>IfYouDoNot</c> — "you create a
///     Treasure token" when the opponent declines to pay. Player is You (the
///     ability controller), Count = 1, Token = <see cref="TokenDefinition.Treasure()"/>.</item>
/// </list>
/// </para>
///
/// <para>ANCHORED (<c>^…$</c>): the full effect clause is matched so that no
/// sibling "that player may pay" text in a different pattern is mis-labelled.
/// Priority 91: must run BEFORE the generic
/// <see cref="ConditionalPayTriggeredRule"/> (priority 80) and after the
/// Rings-of-Brighthearth rule (priority 90), since all three open with a
/// conditional-pay surface but differ in consequent shape.</para>
/// </summary>
[TriggeredRule(Priority = 91)]
public sealed class ThatPlayerMayPayYouCreateTreasureRule : ITriggeredRule
{
  // Full multi-sentence effect (trailing period already stripped by the dispatcher).
  // "that player may pay {COST}. If the player doesn't, you create a Treasure token"
  private static readonly Regex _pattern = new(
    @"^that\s+player\s+may\s+pay\s+(?<cost>(?:\{[^}]+\})+)\s*\."
    + @"\s*If\s+the\s+player\s+doesn't,\s+you\s+create\s+a\s+Treasure\s+token\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var manaCost = TriggeredRuleHelpers.TryBuildManaCost(m.Groups["cost"].Value);
    if (manaCost is null)
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Inner = new ConditionalPayEffect
      {
        Cost = manaCost,
        Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
      },
      IfYouDoNot = new CreateTokenEffect
      {
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Treasure(),
      },
    };
    return true;
  }
}
