namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "You may play the exiled card without paying its mana cost if you attacked with three or
/// more creatures this turn." — Windbrisk Heights's hideaway-payoff activated effect. The
/// exiled card is the one the Hideaway ability set aside (CR 702.75); the permission to play
/// it for free applies only when the count-thresholded attack-history gate holds.
///
/// <para>
/// Produces a <see cref="ConditionalEffect"/> gating a <see cref="MayPlayFromExileEffect"/>
/// (Cards = <c>{Zone:Exile, ExiledWith:Self}</c> — "the exiled card", the CR 406.6 linked
/// reference to the Hideaway exile; <see cref="MayPlayFromExileEffect.WithoutPayingManaCost"/>
/// = true for "without paying its mana cost", CR 118.5) on the attacker-count condition parsed
/// through <see cref="ConditionParser"/> (an
/// <see cref="MagicAST.AST.Abilities.AttackedWithCreaturesThisTurnCondition"/>). Anchored
/// (^…$). Reference-not-resolution (ADR 0004).
/// </para>
///
/// CR 702.75 (Hideaway); CR 406.6 (linked exile); CR 508.1 (declaring attackers); CR 118.5
/// (playing without paying a cost).
/// </summary>
[ActivatedEffectRule(Priority = 1002)]
public sealed class MayPlayExiledCardWithoutPayingIfAttackedEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^You\s+may\s+play\s+the\s+exiled\s+card\s+without\s+paying\s+its\s+mana\s+cost\s+if\s+(?<cond>you\s+attacked\s+with\s+.+?)\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var match = _pattern.Match(effectText.Trim());
    if (!match.Success)
    {
      return null;
    }

    var condition = ConditionParser.Parse(match.Groups["cond"].Value.Trim());
    if (condition is MagicAST.AST.Abilities.OtherCondition)
    {
      return null;
    }

    return new ConditionalEffect
    {
      Condition = condition,
      Then = new MayPlayFromExileEffect
      {
        Cards = new ObjectFilter
        {
          Zone = Zone.Exile,
          ExiledWith = new ObjectReference { Kind = ObjectReferenceKind.Self },
        },
        Actions = [PlayFromExileAction.PlayCards],
        WithoutPayingManaCost = true,
      },
    };
  }
}
