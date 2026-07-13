namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You may draw a card for each other creature you control that shares a creature
/// type with it. If you do, discard a card." — Titan of Littjara's enters-or-attacks
/// card-advantage trigger. The count is the number of OTHER creatures the controller
/// controls that overlap the source's (chosen-plus-printed) creature types
/// (<see cref="ObjectFilter.SharesCreatureTypeWith"/>), and the draw is coupled to a
/// mandatory "if you do, discard a card" follow-up (CR 118.12) via
/// <see cref="OptionalEffect.IfYouDo"/>.
///
/// <para>
/// Priority 90: must outrank the generic <see cref="DrawCardsTriggeredRule"/> (default
/// priority 50), whose unanchored "draw a card" regex would otherwise match the "draw a
/// card" substring anywhere in this text and silently drop both the "for each …" count
/// qualifier and the entire "If you do, discard a card" follow-up — confirmed as the
/// PRE-EXISTING lossy baseline for this exact oracle line before this rule was added.
/// </para>
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class DrawPerSharedCreatureTypeThenDiscardRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+draw\s+a\s+card\s+for\s+each\s+other\s+creature\s+you\s+control\s+that\s+shares\s+a\s+creature\s+type\s+with\s+it\.\s*If\s+you\s+do,\s*discard\s+a\s+card$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    var draw = new DrawCardsEffect
    {
      Count = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
          ExcludeSelf = true,
          SharesCreatureTypeWith = ObjectReference.Self(),
        },
      },
      Player = ObjectReference.You(),
    };

    var discard = new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(1),
      Player = ObjectReference.You(),
    };

    effect = EffectWrap.Optional(draw, true, ifYouDo: discard);
    return true;
  }
}
