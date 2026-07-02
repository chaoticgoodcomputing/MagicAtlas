namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile [N] target [type list] you control, then return those cards to the
/// battlefield under your control." — the spell-side flicker (blink) of
/// Ghostly Flicker.
///
/// <para>
/// The line is a single composite (CR 603.2 governs the triggered sibling, Displacer
/// Kitten; this is the spell wrapper of the same flicker effect): an
/// <see cref="ExileEffect"/> on the chosen permanents, then a
/// <see cref="ReturnToBattlefieldEffect"/> of the just-exiled cards. "those cards" is
/// NOT free text — it is the linked exiled reference (CR 607.2 / ADR 0004
/// "reference, not resolution"): a <see cref="ObjectReferenceKind.Designated"/> card in
/// <see cref="Zone.Exile"/> exiled with this object
/// (<c>ExiledWith = {Kind: Self}</c>), exactly the Petravark return shape. The two
/// effects sit in one <see cref="CompositeEffect"/> because the oracle states them as a
/// single "exile …, then return …" action.
/// </para>
///
/// <para>
/// "two" is a structured cardinality on the target reference's
/// <see cref="ObjectReference.Quantity"/> (a <see cref="LiteralQuantity"/>), not a
/// literal repeated node. "artifacts, creatures, and/or lands you control" is a
/// structured multi-type disjunction filter (<c>CardTypes = ["artifact", "creature",
/// "land"]</c> + <c>Controller = You</c>) — any chosen permanent may be any of the
/// three types. "under your control" rides on
/// <see cref="ReturnToBattlefieldEffect.UnderControl"/> as a
/// <see cref="ObjectReferenceKind.You"/> reference (CR 400.6).
/// </para>
/// </summary>
[SpellRule]
public sealed class FlickerTargetPermanentsSpellRule : ISpellRule
{
  // "Exile two target artifacts, creatures, and/or lands you control, then return
  //  those cards to the battlefield under your control"
  private static readonly Regex Pattern = new(
    @"^Exile\s+two\s+target\s+artifacts,\s+creatures,\s+and/or\s+lands\s+you\s+control,\s+then\s+return\s+those\s+cards\s+to\s+the\s+battlefield\s+under\s+your\s+control$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text))
    {
      return false;
    }

    effect = new CompositeEffect
    {
      Effects =
      [
        new ExileEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Quantity = LiteralQuantity.Of(2),
            Filter = new ObjectFilter
            {
              CardTypes = ["artifact", "creature", "land"],
              Controller = ControllerFilter.You,
            },
          },
        },
        new ReturnToBattlefieldEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Designated,
            Filter = new ObjectFilter
            {
              Zone = Zone.Exile,
              ExiledWith = new ObjectReference { Kind = ObjectReferenceKind.Self },
            },
          },
          UnderControl = ObjectReference.You(),
        },
      ],
    };
    return true;
  }
}
