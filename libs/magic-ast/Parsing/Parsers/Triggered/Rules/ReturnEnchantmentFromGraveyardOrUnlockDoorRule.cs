namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "return an enchantment card from your graveyard to your hand or unlock a locked
/// door of a Room you control" — a one-of-two modal choice between returning an
/// enchantment from the graveyard and unlocking a door of a Room you control.
///
/// <para>
/// This is the ETB modal effect on Ghostly Dancers. "X or Y" is a controller's
/// choice at resolution — Rule 700.2 (modal). Modelled as a
/// <see cref="ModalEffect"/> with <see cref="ModeSelection.ChooseOne"/> whose two
/// modes each wrap one effect in a <see cref="SpellAbility"/>:
/// Mode 1: <see cref="ReturnToHandEffect"/> — an enchantment card in the
///   controller's graveyard, non-targeted ("an enchantment card", indefinite article,
///   no "target" keyword — CR 115.1 / 601.2c: the "target" keyword creates a target;
///   without it the controller simply picks an object that matches at resolution);
/// Mode 2: <see cref="UnlockDoorEffect"/> — a locked half of a Room the controller
///   controls (CR 709.5e–f: unlock half of a permanent with a shared type line).
/// </para>
///
/// <para>
/// The return is Zone = Graveyard, Controller = You (CR 701.9).
/// The unlock is Subtypes = ["Room"], Controller = You (CR 709.5e, 709.5j).
/// </para>
///
/// Must be tried at higher priority than the generic <see cref="ReturnToHandRule"/>
/// to prevent the return-only half from being matched and discarding the modal shape.
/// </summary>
[TriggeredRule(Priority = 80)]
public sealed class ReturnEnchantmentFromGraveyardOrUnlockDoorRule : ITriggeredRule
{
  // Anchored — prevents substring match inside a more-specific sibling effect.
  // The "or" phrasing is the inline modal marker (CR 700.2).
  private static readonly Regex _pattern = new(
    @"^return\s+an\s+enchantment\s+card\s+from\s+your\s+graveyard\s+to\s+your\s+hand\s+or\s+unlock\s+a\s+locked\s+door\s+of\s+a\s+Room\s+you\s+control$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim().TrimEnd('.')))
    {
      return false;
    }

    // Mode 1: return an enchantment card from your graveyard to your hand.
    // Indefinite ("an enchantment card") → ObjectReferenceKind.Any (no targeting
    // declaration, no shroud/hexproof interaction — CR 115.1 / 601.2c).
    var returnEffect = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Any,
        Filter = new ObjectFilter
        {
          CardTypes = ["enchantment"],
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
    };

    // Mode 2: unlock a locked door of a Room you control.
    // "door" = locked half of a Room permanent (CR 709.5j).
    var unlockEffect = new UnlockDoorEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Designated,
        Filter = new ObjectFilter
        {
          CardTypes = ["enchantment"],
          Subtypes = ["Room"],
          Controller = ControllerFilter.You,
        },
      },
    };

    effect = new ModalEffect
    {
      ModeSelection = ModeSelection.ChooseOne(),
      Modes =
      [
        new ModalOption { Ability = new SpellAbility { Effects = [returnEffect] } },
        new ModalOption { Ability = new SpellAbility { Effects = [unlockEffect] } },
      ],
    };
    return true;
  }
}
