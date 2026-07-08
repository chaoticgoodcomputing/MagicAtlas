namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return target [type] card from your graveyard to your hand." — the targeted
/// graveyard-to-hand retrieval as an activated-ability effect (Adun Oakenshield:
/// "{B}{R}{G}, {T}: Return target creature card from your graveyard to your hand.").
/// Also handles the untyped "Return target card from your graveyard to your hand."
///
/// <para>
/// The activated-ability analogue of the spell-side
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.ReturnFromGraveyardToHandRule"/>,
/// producing the identical <see cref="ReturnToHandEffect"/> shape so both parsing
/// paths model the same retrieval uniformly. Source zone is the controller's own
/// graveyard (CR 404.1: a player's graveyard holds objects owned by that player),
/// destination is the controller's hand (CR 402.1). No dedicated keyword action;
/// the text is stated directly. "your graveyard" maps to
/// <see cref="ControllerFilter.You"/> plus <see cref="Zone.Graveyard"/>, mirroring
/// the spell rule's convention.
/// </para>
///
/// <para>
/// ANCHOR: the pattern is fully anchored (^…$) on the whole clause, so it cannot
/// match as a substring of any more-specific sibling. It is distinct from the
/// self-retrieval <see cref="ReturnFromGraveyardToHandEffectRule"/> ("Return this
/// card from your graveyard…") and from the battlefield-bounce
/// <see cref="ReturnTargetToHandEffectRule"/> ("…to its owner's hand"), which
/// require different surface text and so never collide.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 986)]
public sealed class ReturnTargetFromGraveyardToHandEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Return\s+target\s+(?:(?<type>creature|artifact|enchantment|land|permanent|planeswalker)\s+)?card\s+from\s+your\s+graveyard\s+to\s+your\s+hand\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim();
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return null;
    }

    // type group is empty for the untyped "target card" form (e.g. Regrowth).
    var typeRaw = m.Groups["type"].Value.ToLowerInvariant();
    var cardTypes = typeRaw.Length == 0
      ? new List<string> { "card" }
      : new List<string> { typeRaw };

    return new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes,
          Controller = ControllerFilter.You,
          Zone = Zone.Graveyard,
        },
      },
    };
  }
}
