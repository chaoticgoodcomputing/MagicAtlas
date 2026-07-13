namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals X damage to target creature an opponent controls, where X is the
/// number of instant and sorcery cards in your graveyard." — the entering
/// permanent deals damage to a targeted opponent-controlled creature equal to the
/// count of instant and sorcery cards in your graveyard. Covers Cyclops
/// Electromancer's enters trigger.
///
/// CR 603.2: triggered abilities resolve by executing their effects. CR 120.1–
/// 120.2: a source deals damage to a permanent or player. CR 115.1: "target"
/// creates a target — the recipient is "target creature an opponent controls"
/// (<see cref="ObjectReferenceKind.Target"/> + <see cref="ControllerFilter.Opponent"/>).
///
/// "It" is the anaphoric pronoun referring to the entering creature the trigger
/// matched — the ability's own source; modelled as
/// <see cref="ObjectReferenceKind.It"/>. The amount "X" is a
/// <see cref="CountQuantity"/> — the count of objects matching the filter
/// (instant/sorcery card types + <see cref="ControllerFilter.You"/> +
/// <see cref="Zone.Graveyard"/>). "Where X is the number of…" is a definitional
/// clause naming the quantity; the letter X has no independent identity here —
/// the structured <see cref="CountQuantity"/> replaces it. The filter mirrors the
/// same "instant and sorcery cards in your graveyard" shape Cryptic Serpent's
/// cost-reduction fixture already encodes.
///
/// Sibling of <see cref="ItDealsCountDamageToAnyTargetRule"/> (Dragon Tempest,
/// which deals a subtype-you-control count to "any target"). Fully anchored
/// (<c>^…$</c>) on the specific "target creature an opponent controls" recipient
/// and the "instant and sorcery cards in your graveyard" count clause so it cannot
/// capture a broader or sibling effect fragment.
/// </summary>
[TriggeredRule(Priority = 65)]
public sealed class ItDealsInstantSorceryGraveyardCountDamageToOpponentCreatureRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^it\s+deals?\s+X\s+damage\s+to\s+target\s+creature\s+an\s+opponent\s+controls,?\s+where\s+X\s+is\s+the\s+number\s+of\s+instant\s+and\s+sorcery\s+cards?\s+in\s+your\s+graveyard\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new DealDamageEffect
    {
      Source = ObjectReference.It(),
      Amount = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          CardTypes = ["instant", "sorcery"],
          Controller = ControllerFilter.You,
          Zone = Zone.Graveyard,
        },
      },
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.Opponent,
        },
      },
    };
    return true;
  }
}
