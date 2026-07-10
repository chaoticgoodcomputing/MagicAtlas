namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "you may exile a nonland card from your hand" — the Imprint triggered-ability effect
/// (Semblance Anvil, Mirrodin Besieged / MBS:224).
///
/// <para>
/// Imprint is an ability word (flavor, not a CR keyword in this ruleset): an imprint
/// ability exiles a card from the controller's hand onto this permanent so a second ability
/// may reference the exiled card (a linked ability, CR 406.6). The exile is a one-shot effect
/// (Rule 701.13) from Zone.Hand: the controller chooses an eligible card from their hand
/// (not a targeted card — the word "target" does not appear) and exiles it. The choice is
/// modelled as <see cref="ObjectReferenceKind.Any"/> (controller-choice reference, CR 109.5)
/// with Zone.Hand so the interaction layer can distinguish "from hand" from "on the
/// battlefield". The "you may" gate is modelled as an <see cref="OptionalEffect"/> per ADR
/// 0005. "nonland" is the <see cref="ObjectFilter.ExcludedCardTypes"/> negation axis (CR
/// 110.4), mirroring <see cref="ChooseNonlandCardNameOnEntryRule"/>'s "nonland card"
/// encoding.
/// </para>
///
/// <para>
/// Distinct from <see cref="ExileInstantFromHandImprintTriggeredRule"/> (Isochron Scepter),
/// which restricts to instant cards with a mana-value cap instead of the plain "nonland
/// card" restriction here. No mana-value or specific-type filter is printed on this card, so
/// the pattern is anchored to the bare "a nonland card" noun phrase — distinct enough from
/// the instant-card sibling that the two patterns cannot collide on the same input.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ExileNonlandCardFromHandImprintTriggeredRule : ITriggeredRule
{
  // "you may exile a nonland card from your hand"
  private static readonly Regex Pattern = new(
    @"^you\s+may\s+exile\s+a\s+nonland\s+card\s+from\s+your\s+hand$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Inner = new ExileEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Any,
          Filter = new ObjectFilter
          {
            CardTypes = ["card"],
            ExcludedCardTypes = ["land"],
            Controller = ControllerFilter.You,
            Zone = Zone.Hand,
          },
        },
      },
    };
    return true;
  }
}
