namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you gain life equal to that creature's toughness" — the derived-lifegain resolution
/// paired with the "destroy that creature" back-reference family (Engulfing Slagwurm). The
/// gained amount is a <see cref="DerivedQuantity"/> keyed on <see cref="DerivedKind.Toughness"/>
/// whose <see cref="DerivedQuantity.Source"/> is the anaphoric "that creature" — the object
/// named by this ability's own trigger event (CR 603.2). Mirrors the derived-lifegain shape on
/// Niambi, Esteemed Speaker ("gain life equal to that creature's mana value").
///
/// <para>
/// Anchored to the exact toughness surface (<c>^…$</c>) so it cannot claim the mana-value /
/// power / literal siblings handled elsewhere. CR 119.3: "If an effect causes a player to gain
/// life or lose life, that player's life total is adjusted accordingly."
/// </para>
/// </summary>
[TriggeredRule]
public sealed class YouGainLifeEqualToThatCreaturesToughnessTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+gain\s+life\s+equal\s+to\s+that\s+creature's\s+toughness\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    effect = new GainLifeEffect
    {
      Amount = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.Toughness,
        Source = "that creature",
      },
      Player = ObjectReference.You(),
    };
    return true;
  }
}
