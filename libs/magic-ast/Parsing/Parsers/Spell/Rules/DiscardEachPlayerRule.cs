namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Each player [may] discard a card." — Death of Gwen Stacy first effect.
/// Optional follow-up "Each player who doesn't loses N life." lands on
/// <see cref="DiscardCardsEffect.IfYouDoNot"/>.
/// </summary>
[SpellRule]
public sealed class DiscardEachPlayerRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var lower = text.ToLowerInvariant();
    if (!lower.StartsWith("each player"))
    {
      return false;
    }
    if (!Regex.IsMatch(lower, @"discard\s+a\s+card"))
    {
      return false;
    }
    var isOptional = lower.Contains("may discard");

    Effect? ifYouDoNot = null;
    var follow = Regex.Match(
      text,
      @"Each\s+player\s+who\s+doesn'?t\s+loses?\s+(?<life>\d+|one|two|three|four|five)\s+life",
      RegexOptions.IgnoreCase
    );
    if (follow.Success)
    {
      var raw = follow.Groups["life"].Value.ToLowerInvariant();
      int life = raw switch
      {
        "one" => 1,
        "two" => 2,
        "three" => 3,
        "four" => 4,
        "five" => 5,
        _ => int.Parse(raw),
      };
      ifYouDoNot = new LoseLifeEffect
      {
        Amount = LiteralQuantity.Of(life),
        Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
      };
    }

    effect = new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(1),
      Player = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
      Random = false,
      IsOptional = isOptional,
      IfYouDoNot = ifYouDoNot,
    };
    return true;
  }
}
