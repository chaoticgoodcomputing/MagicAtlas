namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile the top N cards of your library. Until the end of your next turn, you may
/// cast an instant or sorcery spell from among those exiled cards." — Chandra,
/// Hope's Beacon's "+1".
///
/// <para>
/// The same duration-bounded impulse family as Jeska's Will / The Legend of Roku:
/// all N cards are exiled (none kept in hand) and stay playable from exile for a
/// window, so it collapses to a single <see cref="ImpulseEffect"/> with
/// <see cref="ImpulseRestDestination.RemainExiled"/> and an inherited
/// <see cref="ContinuousEffect.Duration"/> — "those exiled cards" back-references the
/// pile. Modeling the exile count via <see cref="ImpulseEffect.Count"/> (structured)
/// rather than an <see cref="MagicAST.AST.Effects.ZoneChange.ExileEffect"/> with a
/// "top" positional predicate avoids a free-text sink.
/// </para>
///
/// <para>
/// Chandra's wrinkle over that family: only an <b>instant or sorcery</b> may be cast
/// from the pile, carried structurally on <see cref="ImpulseEffect.PlayableFilter"/>
/// (<c>CardTypes = ["instant","sorcery"]</c>) — not free-texted.
/// </para>
///
/// <para>
/// CR 606.5 (loyalty ability resolution); CR 406 (exile zone); CR 601.3e (permission
/// to cast from a zone other than the hand). ANCHORED (<c>^…$</c>) so the rule cannot
/// claim a substring of a larger effect. Sibling of the [SpellRule]
/// <c>ExileTopCardsPlayUntilTimeRule</c> (spell context, un-filtered "play those
/// cards"); this one is the activated/loyalty, instant-or-sorcery-restricted variant.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 954)]
public sealed class ExileTopMayCastInstantSorceryUntilTimeActivatedRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Exile\s+the\s+top\s+(?<count>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+cards?\s+of\s+your\s+library\.\s+Until\s+the\s+end\s+of\s+your\s+next\s+turn,\s+you\s+may\s+cast\s+an\s+instant\s+or\s+sorcery\s+spell\s+from\s+among\s+those\s+exiled\s+cards$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var m = Pattern.Match(effectText.Trim().TrimEnd('.').Trim());
    if (!m.Success)
    {
      return null;
    }

    var count = ActivatedRuleHelpers.ParseNumberWord(m.Groups["count"].Value) ?? 0;
    if (count <= 0)
    {
      return null;
    }

    return new ImpulseEffect
    {
      Count = LiteralQuantity.Of(count),
      RestDestination = ImpulseRestDestination.RemainExiled,
      PlayableFilter = new ObjectFilter { CardTypes = ["instant", "sorcery"] },
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
