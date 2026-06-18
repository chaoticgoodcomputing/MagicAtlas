namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target creature you control, then return that card to the battlefield
/// under your control." — the Cloudshift blink-to-controller spell pattern.
///
/// <para>
/// Cloudshift is the mandatory (non-optional) single-target blink that returns
/// the exiled creature under the <em>caster's</em> control. It differs from the
/// sibling <see cref="ExileTargetControlledCreatureThenReturnOwnerRule"/>, which
/// uses "return it … under its owner's control" (returning under <em>owner's</em>
/// control), and from <see cref="FlickerTargetPermanentsSpellRule"/>, which targets
/// two permanents of mixed types. This rule handles the "that card" anaphoric
/// pronoun (back-reference to the just-exiled creature) returning under "your
/// control" (the spell controller, CR 400.6).
/// </para>
///
/// <para>
/// The composite shape: an <see cref="ExileEffect"/> on the chosen creature,
/// followed by a <see cref="ReturnToBattlefieldEffect"/> whose target is the linked
/// exiled reference (CR 607.2 / ADR 0004 "reference, not resolution"), addressed
/// via <see cref="ObjectFilter.ExiledWith"/> = <c>{Kind: Self}</c>. "under your
/// control" maps to <see cref="ObjectReferenceKind.You"/> on
/// <see cref="ReturnToBattlefieldEffect.UnderControl"/> (CR 400.6).
/// </para>
///
/// <para>
/// The pattern is anchored (^…$) to prevent substring matches inside more-specific
/// sibling rules. Priority 80 ensures this fires before generic exile rules.
/// CR 406.2 (exile — to put into the exile zone from whatever zone it's currently
/// in). CR 400.6 (zone-change control assignment). CR 406.1 (exile may be
/// temporary).
/// </para>
/// </summary>
[SpellRule(Priority = 80)]
public sealed class ExileTargetCreatureYouControlThenReturnYourControlSpellRule : ISpellRule
{
  // "Exile target creature you control, then return that card to the battlefield
  //  under your control."
  private static readonly Regex Pattern = new(
    @"^Exile\s+target\s+creature\s+you\s+control,\s*then\s+return\s+that\s+card\s+to\s+the\s+battlefield\s+under\s+your\s+control$",
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
          UnderControl = new ObjectReference { Kind = ObjectReferenceKind.You },
        },
      ],
    };
    return true;
  }
}
