namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "target opponent loses that much life" — Vito's drain shape. Encodes the
/// derived quantity (LifeGained antecedent) and the OPPONENT recipient.
///
/// <para>
/// The recipient is <see cref="ObjectReferenceKind.Opponent"/> — a singular opponent (CR glossary
/// "Opponent"), matching the corpus convention for opponent-player references (Eroded Canyon, Jagged
/// Barrens, Lonely Arroyo). It is NOT a generic <c>Target {{ CardTypes = ["player"] }}</c>: dropping
/// the opponent constraint flattens "an opponent" to "any player" (which could be you), which is
/// rules-wrong and downstream costs interaction recall — the operator cannot certify the loser is an
/// opponent, so a life-drain loop (Vito × Exquisite Blood) floors to a needless AMBER. The opponent
/// reference is the parse-layer precision that lets that hop tier GREEN (see
/// libs/mast-interaction/docs/adding-a-flow-arm.md).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class LoseLifeDerivedRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var lower = text.ToLowerInvariant();
    var match = Regex.Match(
      lower,
      @"target\s+opponent\s+loses?\s+that\s+much\s+life",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return false;
    }
    effect = new LoseLifeEffect
    {
      Amount = new DerivedQuantity { DerivedFrom = DerivedKind.LifeGained },
      Player = new ObjectReference { Kind = ObjectReferenceKind.Opponent },
    };
    return true;
  }
}
