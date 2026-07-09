namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile the top card of your library. You may play that card this turn." —
/// the single-card, current-turn-bounded impulse in a TRIGGERED context
/// (Count on Luck: "At the beginning of your upkeep, ...").
///
/// <para>
/// The two oracle sentences are one semantic unit — "that card" back-references
/// the single exiled card from the first sentence — so they collapse to a single
/// <see cref="ImpulseEffect"/> with <see cref="ImpulseRestDestination.RemainExiled"/>
/// (nothing kept in hand; the one exiled card stays playable) and an inherited
/// <see cref="ContinuousEffect.Duration"/> of <see cref="UntilTimeDuration.EndOfTurn"/>.
/// This is the singular sibling of the plural spell/activated shapes
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.ExileTopCardsPlayThisTurnRule"/>
/// ("... You may play them this turn.") and
/// <see cref="MagicAST.Parsing.Parsers.Activated.Rules.ExileTopCardsPlayThisTurnActivatedRule"/>
/// ("... You may play those cards this turn."), emitting the identical node so
/// the same effect reads consistently across spell, activated, and triggered
/// contexts. CR 406 (exile zone); CR 701.13 (exile); CR 601.3 (play).
/// </para>
///
/// <para>
/// ANCHORED (^…$) to the exact two-sentence phrase so it can only ever claim the
/// whole unit — never a lone "Exile the top card of your library" or a lone
/// "You may play that card this turn". This is what keeps the dispatcher's
/// sentence-bundle splitter from fragmenting the pair: neither half parses on its
/// own, so the splitter bails and the full text reaches this rule intact.
/// </para>
/// </summary>
[TriggeredRule(Priority = 80)]
public sealed class ExileTopCardPlayThisTurnTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^Exile\s+the\s+top\s+card\s+of\s+your\s+library\.\s+You\s+may\s+play\s+that\s+card\s+this\s+turn\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new ImpulseEffect
    {
      Count = LiteralQuantity.Of(1),
      RestDestination = ImpulseRestDestination.RemainExiled,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
