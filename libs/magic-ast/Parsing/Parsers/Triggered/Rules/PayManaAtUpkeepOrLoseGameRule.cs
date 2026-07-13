namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.References;

/// <summary>
/// "pay {COST}. If you don't, you lose the game." — the Pact deferred-cost
/// consequence, the effect half of the delayed upkeep trigger "At the beginning of
/// your next upkeep, pay {COST}. If you don't, you lose the game." (Intervention
/// Pact, Pact of Negation, Slaughter Pact, Summoner's Pact, Pact of the Titan).
///
/// <para>
/// Semantically "you lose the game unless you pay {COST}": the stated loss is the
/// consequence, gated by whether the controller pays the mana. Modelled — exactly
/// like the "sacrifice this permanent unless you pay {COST}" upkeep-tax
/// (<see cref="SacrificeSelfUnlessPayRule"/>) — as a <see cref="PreventableEffect"/>
/// whose <see cref="PreventableEffect.Inner"/> is a <see cref="LoseTheGameEffect"/>
/// (Player = You) and whose <see cref="UnlessClause"/> carries Player = You and the
/// parsed mana Cost. Paying a cost is never automatic (CR 118.5), so the loss occurs
/// only if the controller chooses not to pay.
/// </para>
///
/// <para>
/// The trigger half ("At the beginning of your next upkeep") is split off by
/// <see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/> and parsed by
/// <see cref="NextPhaseTriggerConditionRule"/> into a GameTime with When = Next; this
/// rule only produces the resolution effect. ANCHORED (<c>^pay … lose the game$</c>)
/// so it matches only this exact two-sentence consequence.
/// </para>
///
/// <para>
/// CR 104.3a: "A player still in the game loses the game as a result of … an effect
/// that states that the player loses the game." CR 118.5: paying a cost is not
/// automatic.
/// </para>
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class PayManaAtUpkeepOrLoseGameRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^pay\s+(?<cost>(?:\{[^}]+\})+)\.\s+If\s+you\s+don't,\s+you\s+lose\s+the\s+game$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var manaCost = TriggeredRuleHelpers.TryBuildManaCost(m.Groups["cost"].Value);
    if (manaCost is null)
    {
      return false;
    }

    effect = EffectWrap.Preventable(
      new LoseTheGameEffect { Player = ObjectReference.You() },
      new UnlessClause
      {
        Player = ObjectReference.You(),
        Cost = manaCost,
      }
    );
    return true;
  }
}
