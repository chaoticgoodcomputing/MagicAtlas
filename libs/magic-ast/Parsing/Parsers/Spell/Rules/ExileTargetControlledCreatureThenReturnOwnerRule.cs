namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target creature you control, then return it to the battlefield under
/// its owner's control." — the Ephemerate / blink-to-owner spell pattern.
///
/// <para>
/// This is the spell-wrapper of the classic blink (flicker) action where the
/// returned permanent comes back under its <em>owner's</em> control rather than
/// the controller's (contrast with <see cref="FlickerTargetPermanentsSpellRule"/>,
/// which returns under "your control"). The oracle text uses a curly apostrophe
/// (U+2019) in "owner’s", matched verbatim.
/// </para>
///
/// <para>
/// The composite shape mirrors the <see cref="FlickerTargetPermanentsSpellRule"/>:
/// an <see cref="ExileEffect"/> on the chosen creature, followed by a
/// <see cref="ReturnToBattlefieldEffect"/> whose target is the linked exiled card
/// (CR 607.2 / ADR 0004 reference-not-resolution), addressed via
/// <see cref="ObjectFilter.ExiledWith"/> = <c>{Kind: Self}</c>. "under its
/// owner's control" maps to <see cref="ObjectReferenceKind.Owner"/> on
/// <see cref="ReturnToBattlefieldEffect.UnderControl"/> (CR 400.6).
/// </para>
///
/// <para>
/// The pattern is anchored (^…$) to prevent substring matches inside
/// more-specific sibling rules (e.g. the "non-Angel" or "another target" families).
/// CR 701.13 (exile). CR 400.6 (owner/controller distinction).
/// </para>
/// </summary>
[SpellRule(Priority = 80)]
public sealed class ExileTargetControlledCreatureThenReturnOwnerRule : ISpellRule
{
  // "Exile target creature you control, then return it to the battlefield under
  //  its owner's control."
  // The apostrophe in "owner's" is the Unicode curly apostrophe U+2019 (’)
  // as printed in the oracle text.
  private static readonly Regex Pattern = new(
    @"^Exile\s+target\s+creature\s+you\s+control,\s*then\s+return\s+it\s+to\s+the\s+battlefield\s+under\s+its\s+owner[’']s\s+control$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.').Trim();
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
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
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
