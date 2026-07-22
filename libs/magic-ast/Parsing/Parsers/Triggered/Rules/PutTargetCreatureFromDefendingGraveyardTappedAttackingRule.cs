namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "put target creature card from defending player's graveyard onto the battlefield
/// under your control tapped and attacking." — Chorale of the Void's attack-trigger
/// reanimation. Reanimates a creature card from the DEFENDING player's graveyard
/// (CR 508.1b) under the controller's control, entering both tapped and already
/// attacking (CR 508.4 — an effect can put a creature onto the battlefield attacking).
///
/// <para>Modelled as a <see cref="ReturnToBattlefieldEffect"/> ("put … onto the
/// battlefield" is the reanimation verb, same node the graveyard-reanimation family
/// uses):
/// <list type="bullet">
///   <item><see cref="ReturnToBattlefieldEffect.Target"/> — a targeted creature card in
///   the defending player's graveyard (<see cref="ControllerFilter.DefendingPlayer"/>,
///   the graveyard-owner axis convention the reanimation golds use, e.g. Knight's
///   Charge's <c>Controller = You</c> for "from your graveyard").</item>
///   <item><see cref="ReturnToBattlefieldEffect.Tapped"/> = true — "tapped".</item>
///   <item><see cref="ReturnToBattlefieldEffect.Attacking"/> = true — "and attacking"
///   (the reanimation analogue of <see cref="TokenDefinition.EntersAttacking"/>).</item>
///   <item><see cref="ReturnToBattlefieldEffect.UnderControl"/> = You — "under your
///   control".</item>
/// </list></para>
///
/// <para>ANCHORED (^…$): the full clause is anchored so it cannot substring-match a
/// sibling.</para>
/// </summary>
[TriggeredRule(Priority = 62)]
public sealed class PutTargetCreatureFromDefendingGraveyardTappedAttackingRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^put\s+target\s+creature\s+card\s+from\s+defending\s+player's\s+graveyard\s+onto\s+the\s+battlefield\s+under\s+your\s+control\s+tapped\s+and\s+attacking\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.DefendingPlayer,
        },
      },
      Tapped = true,
      Attacking = true,
      UnderControl = ObjectReference.You(),
    };
    return true;
  }
}
