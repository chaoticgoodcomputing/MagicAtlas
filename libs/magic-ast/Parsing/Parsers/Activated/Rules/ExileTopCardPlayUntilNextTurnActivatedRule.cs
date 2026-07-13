namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile the top card of your library. Until the end of your next turn, you may
/// play that card." — the single-card, next-turn-bounded impulse in an
/// activated-ability context (Chase Stein, Runaway; Cori Mountain Monastery).
///
/// <para>
/// The two oracle sentences are one semantic unit — "that card" back-references
/// the single card exiled by the first sentence — so they collapse to a single
/// <see cref="ImpulseEffect"/> with <see cref="ImpulseRestDestination.RemainExiled"/>
/// (the one exiled card stays in exile, none kept in hand) and an inherited
/// <see cref="ContinuousEffect.Duration"/> bounding the play window. This is the
/// count-1 / next-turn analogue of the plural, this-turn sibling
/// <see cref="ExileTopCardsPlayThisTurnActivatedRule"/> and the spell-context
/// duration sibling
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.ExileTopCardsPlayUntilTimeRule"/>.
/// ADR 0004 (reference, not resolution): MAST records the exile and the play
/// window; the engine tracks which specific card was exiled and enforces the
/// deadline.
/// </para>
///
/// <para>
/// CR 406 (exile zone); CR 701.13 (exile); CR 601 / CR 305 ("play" a card).
/// "Until the end of your next turn" → a <see cref="GameTime"/> at the End edge of
/// the controller's Next Turn.
/// </para>
///
/// <para>
/// ANCHORED (^…$): the exact singular "the top card … you may play that card"
/// shape. The trailing "$" keeps it from claiming more-specific siblings that
/// append a rider (e.g. "… you may play that card and you may spend mana as though
/// it were mana of any color"), whose extra permission must not be silently
/// dropped. Priority 975 mirrors the plural sibling — above mid-range, so the
/// two-sentence split path (TryParseMultiEffectSentences) cannot claim either
/// sentence independently (the "Until … you may play that card" half parses no
/// standalone rule).
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 975)]
public sealed class ExileTopCardPlayUntilNextTurnActivatedRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Exile\s+the\s+top\s+card\s+of\s+your\s+library\.\s+Until\s+the\s+end\s+of\s+your\s+next\s+turn,\s+you\s+may\s+play\s+that\s+card\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    if (!_pattern.IsMatch(effectText.Trim()))
    {
      return null;
    }

    return new ImpulseEffect
    {
      Count = LiteralQuantity.Of(1),
      RestDestination = ImpulseRestDestination.RemainExiled,
      Duration = new UntilTimeDuration
      {
        Until = new GameTime
        {
          Part = TurnPart.Turn,
          Edge = TimeBoundary.End,
          When = TimeRelation.Next,
          Whose = ControllerFilter.You,
        },
      },
    };
  }
}
