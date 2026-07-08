namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target player's graveyard" / "Exile target opponent's graveyard" /
/// "Exile each opponent's graveyard" / "Exile each player's graveyard" — the
/// graveyard-hate family that exiles the entire contents of a player's graveyard
/// zone, in the effect position of an activated ability (Tormod's Crypt: "{T},
/// Sacrifice this artifact: Exile target player's graveyard.").
///
/// <para>
/// CR 701.13a: "To exile an object, move it to the exile zone from wherever it
/// is." The action exiles every card in the named graveyard, so the effect's
/// target is the whole zone's contents rather than a single chosen card —
/// modeled as an <see cref="ObjectReferenceKind.Each"/> reference (all cards)
/// whose filter pins the graveyard zone and the owning player. The player scope
/// rides the filter's <see cref="ObjectFilter.Owner"/> axis (CR 108.3 — cards in
/// a graveyard are owned by that player): "target player's" → <c>Owner=Target</c>;
/// "target opponent's" / "each opponent's" → <c>Owner=Opponent</c>.
/// </para>
///
/// <para>
/// Mirrors the triggered-ability sibling <c>ExilePlayerGraveyardZoneRule</c>
/// (same effect shape, different ability kind), which cannot be reused directly
/// because the activated and triggered effect-rule pipelines are separate
/// reflection-discovered chains (<see cref="IActivatedEffectRule"/> vs.
/// <c>ITriggeredRule</c>).
/// </para>
///
/// Anchored (^…$) so it only fires on the bare whole-graveyard-exile body and
/// never inside a longer composite clause a more-specific rule should handle.
/// </summary>
[ActivatedEffectRule(Priority = 983)]
public sealed class ExilePlayerGraveyardZoneActivatedEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Exile\s+(?<scope>target\s+player|target\s+opponent|each\s+opponent|each\s+player)'?s?\s+graveyard\s*\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim();
    var match = Pattern.Match(text);
    if (!match.Success)
    {
      return null;
    }

    var scope = match.Groups["scope"].Value.ToLowerInvariant();
    var owner = scope.Contains("opponent") ? ControllerFilter.Opponent : ControllerFilter.Target;

    return new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["card"],
          Owner = owner,
          Zone = Zone.Graveyard,
        },
      },
    };
  }
}
