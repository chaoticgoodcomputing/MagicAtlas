namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.References;

/// <summary>
/// "You may play that card for as long as it remains exiled." — a one-shot
/// permission to play a specific, previously-referenced card while it sits in
/// exile, UNBOUNDED by any turn clock (Savvy Trader: "... exile target
/// permanent card from your graveyard. You may play that card for as long as
/// it remains exiled.").
///
/// <para>
/// Sibling of <see cref="YouMayPlayThatCardFromExileThisTurnTriggeredRule"/>
/// (same <see cref="MayPlayTargetFromExileEffect"/>/<see cref="OptionalEffect"/>
/// shape, same "that card" → <see cref="ObjectReferenceKind.It"/> back-reference,
/// ADR 0004), but the duration differs: "this turn" is a clock-bounded
/// <see cref="UntilTimeDuration"/>, whereas "for as long as it remains exiled"
/// is a zone-membership state (CR 611.2c) — an
/// <see cref="AsLongAsDuration"/> wrapping an
/// <see cref="ObjectInZoneCondition"/> (Reference = It, Zone = Exile). Anchored
/// (^you may play that card for as long as it remains exiled$) so it cannot
/// collide with the "this turn" sibling's distinct anchor.
/// </para>
///
/// CR 701.13a (exile) places the card in exile; this permission is what lets
/// the controller play it from there, for as long as it stays there (CR 305.1
/// "play a land" / CR 601.2 "cast a spell" normally require the hand zone).
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class YouMayPlayThatCardForAsLongAsExiledTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^you\s+may\s+play\s+that\s+card\s+for\s+as\s+long\s+as\s+it\s+remains\s+exiled$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Inner = new MayPlayTargetFromExileEffect
      {
        Target = ObjectReference.It(),
        Duration = new AsLongAsDuration
        {
          Condition = new ObjectInZoneCondition
          {
            Reference = ObjectReference.It(),
            Zone = Zone.Exile,
          },
        },
      },
    };
    return true;
  }
}
