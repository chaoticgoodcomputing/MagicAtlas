namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile the top N cards of your library. You may play them this turn." —
/// a current-turn-bounded impulse where all N exiled cards stay playable
/// from exile until end of turn (Jeska's Will).
///
/// <para>
/// Modeled as a single <see cref="ImpulseEffect"/> with
/// <see cref="ImpulseRestDestination.RemainExiled"/> (none kept in hand; all N
/// remain exiled) and an inherited <see cref="ContinuousEffect.Duration"/> of
/// <see cref="UntilTimeDuration.EndOfTurn"/>. The two oracle sentences are one
/// game concept — "them" back-references the exiled pile — so they collapse into
/// one node rather than a separate exile + permission. CR 406 (exile zone);
/// CR 701.13 (exile). Sibling of <see cref="ExileTopCardsPlayUntilTimeRule"/>
/// which uses "until the end of your next turn".
/// </para>
/// </summary>
[SpellRule]
public sealed class ExileTopCardsPlayThisTurnRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Exile\s+the\s+top\s+(?<count>\w+)\s+cards?\s+of\s+your\s+library\.\s+You\s+may\s+play\s+them\s+this\s+turn\.?$",
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

    if (!SpellRuleHelpers.TryParseSmallWord(m.Groups["count"].Value, out var count))
    {
      return false;
    }

    effect = new ImpulseEffect
    {
      Count = LiteralQuantity.Of(count),
      RestDestination = ImpulseRestDestination.RemainExiled,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
