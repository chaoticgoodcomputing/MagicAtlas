namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "exile up to one target nonland permanent you control, then return that card to
/// the battlefield under its owner's control" — the cast-trigger flicker (blink) of
/// Displacer Kitten ("Avoidance — Whenever you cast a noncreature spell, …").
///
/// <para>
/// This is the triggered-side wrapper of the same flicker effect the spell-side
/// <see cref="Spell.Rules.FlickerTargetPermanentsSpellRule"/> (Ghostly Flicker) builds:
/// an <see cref="ExileEffect"/> on the chosen permanent, then a
/// <see cref="ReturnToBattlefieldEffect"/> of the just-exiled card. The cast trigger
/// itself ("Whenever you cast a noncreature spell") is recognised by
/// <see cref="SpellCastConditionRule"/> per CR 603.2 (a game event matching the trigger
/// event triggers the ability); this rule only builds the effect.
/// </para>
///
/// <para>
/// "that card" is NOT free text — it is the linked exiled reference (CR 607.2 /
/// ADR 0004 "reference, not resolution"): a <see cref="ObjectReferenceKind.Designated"/>
/// card in <see cref="Zone.Exile"/> exiled with this object
/// (<c>ExiledWith = {Kind: Self}</c>), the Petravark return shape. "up to one" is a
/// structured <see cref="UpToQuantity"/> on the target reference's
/// <see cref="ObjectReference.Quantity"/>, not a literal. "nonland permanent you control"
/// is a structured filter (<c>CardTypes = ["permanent"]</c> + <c>ExcludedCardTypes =
/// ["land"]</c> + <c>Controller = You</c>). "under its owner's control" rides on
/// <see cref="ReturnToBattlefieldEffect.UnderControl"/> as a
/// <see cref="ObjectReferenceKind.Owner"/> reference (CR 400.6). The two effects sit in
/// one <see cref="CompositeEffect"/> because the oracle states them as a single
/// "exile …, then return …" action.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class FlickerUpToOnePermanentTriggeredRule : ITriggeredRule
{
  // "exile up to one target nonland permanent you control, then return that card to
  //  the battlefield under its owner's control"
  private static readonly Regex Pattern = new(
    @"^exile\s+up\s+to\s+one\s+target\s+nonland\s+permanent\s+you\s+control,\s+then\s+return\s+that\s+card\s+to\s+the\s+battlefield\s+under\s+its\s+owner'?s\s+control$",
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

    effect = new CompositeEffect
    {
      Effects =
      [
        new ExileEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Quantity = new UpToQuantity { Maximum = 1 },
            Filter = new ObjectFilter
            {
              CardTypes = ["permanent"],
              ExcludedCardTypes = ["land"],
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
          UnderControl = new ObjectReference { Kind = ObjectReferenceKind.Owner },
        },
      ],
    };
    return true;
  }
}
