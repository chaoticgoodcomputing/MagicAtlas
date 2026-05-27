namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Draw [N] card(s), then discard [M] card(s)." — draw-then-discard loot pattern.
/// Examples:
/// <list type="bullet">
///   <item>"Draw four cards, then discard two cards." — Prying Eyes</item>
///   <item>"Draw two cards, then discard a card." — Ghastly Discovery (first line)</item>
/// </list>
/// Emits a flat <c>[DrawCardsEffect, DiscardCardsEffect]</c> list via
/// <see cref="IMultiSpellRule.TryMatchMulti"/>. Both effects target the controller
/// (Player = You). The single-effect <see cref="ISpellRule.TryMatch"/> always returns
/// false; the multi-effect path is the only active route.
/// </summary>
[SpellRule]
public sealed class DrawThenDiscardRule : ISpellRule, IMultiSpellRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+";

  private static readonly Regex _pattern = new(
    $@"^Draw\s+(?<draw>{CountTokens})\s+cards?,\s*then\s+discard\s+(?<discard>{CountTokens})\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // -------------------------------------------------------------------------
  // ISpellRule — single-effect path intentionally disabled.
  // -------------------------------------------------------------------------
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  // -------------------------------------------------------------------------
  // IMultiSpellRule — flat [DrawCardsEffect, DiscardCardsEffect] list.
  // -------------------------------------------------------------------------
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var drawCount = SpellRuleHelpers.ParseSmallWord(m.Groups["draw"].Value);
    var discardCount = SpellRuleHelpers.ParseSmallWord(m.Groups["discard"].Value);

    effects = new List<Effect>
    {
      new DrawCardsEffect
      {
        Count = LiteralQuantity.Of(drawCount),
        Player = ObjectReference.You(),
      },
      new DiscardCardsEffect
      {
        Count = LiteralQuantity.Of(discardCount),
        Player = ObjectReference.You(),
        Random = false,
      },
    };
    return true;
  }
}
