namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Triggered-context effect body "you draw N card(s) and lose M life." — the
/// controller draw-and-drain payoff on a triggered ability (Elegy Acolyte:
/// "…you draw a card and lose 1 life."). ANCHORED (<c>^…$</c>) so it beats the
/// unanchored <see cref="DrawCardsTriggeredRule"/> (which matches the bare
/// "draw a card" substring and silently drops the "and lose 1 life" conjunct);
/// this rule's higher priority makes it win the dispatch and emit BOTH effects.
///
/// <para>
/// Emits a <see cref="CompositeEffect"/> of the two sibling effects
/// (<see cref="DrawCardsEffect"/> + <see cref="LoseLifeEffect"/>), both scoped to
/// the controller ("you"), mirroring the spell-context
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.YouDrawCardsAndYouLoseLifeRule"/>.
/// </para>
///
/// <para>
/// CR 121.1 (draw a card); CR 119.3: "If an effect causes a player to gain life
/// or lose life, that player's life total is adjusted accordingly."
/// </para>
/// </summary>
[TriggeredRule(Priority = 120)]
public sealed class YouDrawCardAndLoseLifeTriggeredRule : ITriggeredRule
{
  private const string CountTokens = @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+";

  private static readonly Regex _pattern = new(
    $@"^you\s+draw\s+(?<draw>{CountTokens})\s+cards?\s+and\s+lose\s+(?<lose>{CountTokens})\s+life\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var match = _pattern.Match(text.Trim());
    if (!match.Success)
    {
      return false;
    }

    var you = ObjectReference.You();
    effect = new CompositeEffect
    {
      Effects =
      [
        new DrawCardsEffect
        {
          Count = LiteralQuantity.Of(TriggeredRuleHelpers.ParseWordOrDigitCount(match.Groups["draw"].Value) ?? 1),
          Player = you,
        },
        new LoseLifeEffect
        {
          Amount = LiteralQuantity.Of(TriggeredRuleHelpers.ParseWordOrDigitCount(match.Groups["lose"].Value) ?? 1),
          Player = ObjectReference.You(),
        },
      ],
    };
    return true;
  }
}
