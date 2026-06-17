namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Add {R} for each card in target opponent's hand." — adds one mana of a
/// specific color for each card in the targeted opponent's hand (Jeska's Will).
///
/// <para>
/// The amount is a <see cref="CountQuantity"/> over cards in the target's hand:
/// <c>CountOf: { CardTypes: ["card"], Zone: Hand, Controller: Target }</c>.
/// The "target opponent" is the player being targeted; <see cref="ControllerFilter.Target"/>
/// identifies that the hand count belongs to the targeted player (CR 108.3 /
/// 109.4 — in a hand, the controller/owner is the player whose hand it is).
/// </para>
/// </summary>
[SpellRule]
public sealed class AddManaForEachCardInHandRule : ISpellRule
{
  // "Add {X} for each card in target opponent's hand."
  // Mana symbol group captures exactly one mana symbol (e.g. {R}).
  private static readonly Regex Pattern = new(
    @"^Add\s+(?<mana>\{[^}]+\})\s+for\s+each\s+card\s+in\s+target\s+opponent'?s\s+hand\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effect = new AddManaEffect
    {
      Mana = m.Groups["mana"].Value,
      Amount = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          CardTypes = ["card"],
          Zone = Zone.Hand,
          Controller = ControllerFilter.Target,
        },
      },
    };
    return true;
  }
}
