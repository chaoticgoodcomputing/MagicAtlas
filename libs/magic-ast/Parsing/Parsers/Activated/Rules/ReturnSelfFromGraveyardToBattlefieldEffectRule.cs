namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return this card from your graveyard to the battlefield tapped." — a
/// graveyard-self reanimation activated ability (Drownyard Temple). The ability
/// functions while the card is in its owner's graveyard; activating it returns the
/// card itself (Self) from the graveyard to the battlefield, and the card enters
/// tapped.
///
/// Distinct from <see cref="ReturnToBattlefieldEffectRule"/> ("Return TARGET X from
/// [zone] to the battlefield"): the subject here is the source card itself
/// (CR 109.2 — "this card" refers to the object with the ability), expressed as a
/// <see cref="ObjectReferenceKind.Self"/> reference. The source zone "your
/// graveyard" is the structured <see cref="ObjectFilter.Zone"/> Graveyard +
/// <see cref="ControllerFilter.You"/>, not free text.
///
/// CR 611.1 (verbatim): "A continuous effect modifies characteristics of objects,
/// modifies control of objects, or affects players or the rules of the game, for a
/// fixed or indefinite period." — the resolving ability changes the card's zone and
/// returns it under its owner's control; the "tapped" entering state is a structured
/// <see cref="ReturnToBattlefieldEffect.Tapped"/> flag, not free text.
/// </summary>
[ActivatedEffectRule(Priority = 990)]
public sealed class ReturnSelfFromGraveyardToBattlefieldEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = Regex.Match(
      trimmed,
      @"^return\s+this\s+card\s+from\s+your\s+graveyard\s+to\s+the\s+battlefield\s+tapped$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    return new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Self,
        Filter = new ObjectFilter
        {
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
      Tapped = true,
    };
  }
}
