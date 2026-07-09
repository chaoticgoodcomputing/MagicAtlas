namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "exile target card from an opponent's graveyard" — single-card graveyard-hate
/// on the triggered side (Leonin of the Lost Pride, Ruin Rat, Disposal Mummy,
/// Scavenging Harpy, Carrion Locust, General Kudro of Drannith): one chosen card
/// in an opponent's graveyard is exiled.
///
/// <para>
/// Distinct from <see cref="ExilePlayerGraveyardZoneRule"/> (which exiles the WHOLE
/// contents of a player's graveyard via <see cref="ObjectReferenceKind.Each"/>).
/// Here a single <see cref="ObjectReferenceKind.Target"/> card is chosen, so the
/// reference is <c>Kind=Target</c> with a <c>CardTypes=["card"]</c> +
/// <c>Zone=Graveyard</c> filter, and the owning-player scope rides the filter's
/// <see cref="ObjectFilter.Owner"/> axis: "an opponent's" / "target opponent's" →
/// <c>Owner=Opponent</c> (CR 108.3 — a card in a graveyard is owned by that player;
/// CR 404.2 — a graveyard is that player's pile).
/// </para>
///
/// <para>
/// The bare "exile target card from a graveyard" form (no owner scope) is handled
/// by the activated/spell graveyard-exile rules and by
/// <see cref="Spell.Rules.ExileTargetCardFromGraveyardRule"/>; this triggered rule
/// deliberately covers only the opponent-scoped variant so it never overlaps the
/// owner-less form.
/// </para>
///
/// Anchored (^…$) so it fires only when the entire effect clause is this
/// single-card opponent-graveyard exile, never as a substring of a longer
/// composite clause a more-specific sibling should own.
/// Rule 701.13 (Exile) + Rule 108.3 (ownership) + Rule 404.2 (graveyard).
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class ExileTargetCardFromOpponentGraveyardTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^exile\s+target\s+card\s+from\s+(?:an\s+opponent|target\s+opponent)['’]s\s+graveyard$",
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

    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["card"],
          Owner = ControllerFilter.Opponent,
          Zone = Zone.Graveyard,
        },
      },
    };
    return true;
  }
}
