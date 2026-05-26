namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "target opponent loses that much life" — Vito's drain shape. Encodes the
/// derived quantity (LifeGained antecedent).
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
      Player = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["player"] },
      },
    };
    return true;
  }
}
