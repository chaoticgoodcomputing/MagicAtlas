namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Its controller mills N cards." — the mill keyword action (CR 701.17a) whose
/// subject back-references the controller of a permanent or spell named in a
/// prior sentence, mirroring the anaphoric-controller pattern already used by
/// <see cref="ItsControllerLosesLifeRule"/>.
///
/// This rule handles the trailing sentence in patterns like:
/// "Counter target spell. Its controller mills three cards." (Didn't Say Please)
///
/// "Its controller" is modelled as <see cref="ObjectReferenceKind.Controller"/>,
/// an anaphoric reference to the controller of the previously mentioned object
/// (the countered spell). No runtime tracking is introduced — MAST describes the
/// card, it does not execute it.
///
/// CR 701.17a (verbatim): "For a player to mill a number of cards, that player
/// puts that many cards from the top of their library into their graveyard."
/// </summary>
[SpellRule]
public sealed class ItsControllerMillsRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Its\s+controller\s+mills\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+cards?$",
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

    effect = new MillEffect
    {
      Count = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["amount"].Value)),
      Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
    };
    return true;
  }
}
