namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return target instant or sorcery card from your graveyard to your hand." — the
/// graveyard-recursion effect on an activated ability (e.g. Scribe of the Mindful:
/// "{1}, {T}, Sacrifice this creature: Return target instant or sorcery card from your
/// graveyard to your hand.").
///
/// <para>
/// This is a zone change moving a targeted card from the controller's graveyard
/// (source: CR 404.1) to the controller's hand (destination: CR 402.1). The activated
/// path previously left this interior as an <c>UnstructuredEffect</c>; the identical
/// shape is already structured on the triggered path
/// (<c>ReturnInstantOrSorceryFromGraveyardRule</c>, Izzet Chronarch / Archaeomancer).
/// Both emit the same <see cref="ReturnToHandEffect"/> so the effect is represented
/// consistently regardless of the ability kind that carries it.
/// </para>
///
/// <para>
/// The filter encodes the stated restrictions directly: <c>CardTypes = ["instant",
/// "sorcery"]</c> is the type disjunction, <c>Zone = Graveyard</c> the source zone,
/// and <c>Controller = You</c> the "your graveyard" qualifier.
/// </para>
///
/// <para>
/// ANCHOR: the regex is fully anchored (<c>^…$</c>) on the complete effect clause, so
/// it neither collides with the battlefield-bounce shape
/// (<see cref="ReturnTargetToHandEffectRule"/>, "to its owner's hand") nor with the
/// self-retrieval shapes. It runs at a high priority because it is a precise, fully
/// specified clause.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 986)]
public sealed class ReturnTargetInstantOrSorceryFromGraveyardToHandEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Return\s+target\s+instant\s+or\s+sorcery\s+card\s+from\s+your\s+graveyard\s+to\s+your\s+hand$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    if (!_pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["instant", "sorcery"],
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
    };
  }
}
