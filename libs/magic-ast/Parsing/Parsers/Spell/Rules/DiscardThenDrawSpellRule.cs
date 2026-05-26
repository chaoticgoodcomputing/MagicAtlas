namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You may discard [N] card(s). If you do, draw [M] card(s)." — Abandon Attachments.
/// Head <see cref="DiscardCardsEffect"/> with IsOptional=true and IfYouDo continuation.
/// </summary>
[SpellRule]
public sealed class DiscardThenDrawSpellRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^You\s+may\s+discard\s+(?<dn>a|one|two|three|four|five|\d+)\s+cards?\.\s*If\s+you\s+do,\s*draw\s+(?<rn>a|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }

    var discardCount = SpellRuleHelpers.ParseSmallWord(m.Groups["dn"].Value);
    var drawCount = SpellRuleHelpers.ParseSmallWord(m.Groups["rn"].Value);

    var draw = new DrawCardsEffect
    {
      Count = LiteralQuantity.Of(drawCount),
      Player = ObjectReference.You(),
    };
    effect = new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(discardCount),
      Player = ObjectReference.You(),
      Random = false,
      IsOptional = true,
      IfYouDo = draw,
    };
    return true;
  }
}
