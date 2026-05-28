namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Draw N cards" — "Draw two cards", "Draw a card", "Each other player draws a card".
/// "Each other player" maps to EachOtherPlayer (broader than EachOpponent in
/// multiplayer, Rule 109.1 / 102.1).
/// </summary>
[ActivatedEffectRule(Priority = 998)]
public sealed class DrawCardsEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');
    var lower = effectText.ToLowerInvariant();

    if (!lower.Contains("draw"))
    {
      return null;
    }

    ObjectReference player;
    if (lower.Contains("each other player"))
    {
      player = new ObjectReference { Kind = ObjectReferenceKind.EachOtherPlayer };
    }
    else if (lower.Contains("each opponent"))
    {
      player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent };
    }
    else if (lower.Contains("you"))
    {
      player = ObjectReference.You();
    }
    else
    {
      // Default to "you"
      player = ObjectReference.You();
    }

    var count = ActivatedRuleHelpers.ParseNumberWord(effectText) ?? 1;

    return new DrawCardsEffect { Count = LiteralQuantity.Of(count), Player = player };
  }
}
