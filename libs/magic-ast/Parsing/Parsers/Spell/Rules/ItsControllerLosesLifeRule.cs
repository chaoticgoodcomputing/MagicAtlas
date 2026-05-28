namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Its controller loses N life." — life-loss whose subject back-references
/// the controller of a permanent named in a prior sentence (Rule 119.3:
/// "If an effect causes a player to gain life or lose life, that player's
/// life total is adjusted accordingly.").
///
/// This rule handles the trailing sentence in patterns like:
/// "Destroy target land. Its controller loses 2 life." (Spreading Rot)
///
/// "Its controller" is modelled as <see cref="ObjectReferenceKind.Controller"/>,
/// which is an anaphoric reference to the controller of the previously mentioned
/// object (the destroyed land). No runtime tracking is introduced — MAST describes
/// the card, it does not execute it.
///
/// CR 119.3 (verbatim): "If an effect causes a player to gain life or lose life,
/// that player's life total is adjusted accordingly."
/// CR 701.8a (verbatim): "To destroy a permanent, move it from the battlefield
/// to its owner's graveyard."
/// </summary>
[SpellRule]
public sealed class ItsControllerLosesLifeRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Its\s+controller\s+loses\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effect = new LoseLifeEffect
    {
      Amount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["amount"].Value)),
      Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
    };
    return true;
  }
}
