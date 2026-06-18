namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return this card from your graveyard to the battlefield" — graveyard self-reanimation
/// as a triggered effect (Forsaken Miner, CR 700.13 / CR 603).
///
/// <para>
/// The source zone "your graveyard" is encoded on the Target's ObjectFilter.Zone axis
/// (<see cref="Zone.Graveyard"/>) and Controller axis (<see cref="ControllerFilter.You"/>).
/// The card re-enters the battlefield NOT tapped (no "tapped" qualifier); Tapped = false.
/// </para>
///
/// <para>
/// This is the un-tapped counterpart to the activated-ability rule
/// <c>ReturnSelfFromGraveyardToBattlefieldEffectRule</c> (which handles the tapped form).
/// The regex is fully anchored (^…$) so it cannot match inside a longer effect clause.
/// </para>
///
/// <para>
/// CR 113.6 example (Reassembling Skeleton): an ability on a card in the graveyard that
/// returns the card to the battlefield functions from that zone. This rule models the
/// equivalent triggered-effect form.
/// </para>
/// </summary>
[TriggeredRule(Priority = 72)]
public sealed class ReturnSelfFromGraveyardToBattlefieldRule : ITriggeredRule
{
  /// <summary>
  /// Matches: "return this card from your graveyard to the battlefield"
  /// Full-string anchor prevents collision with longer variants (e.g. "…tapped" form).
  /// </summary>
  private static readonly Regex _pattern = new(
    @"^return\s+this\s+card\s+from\s+your\s+graveyard\s+to\s+the\s+battlefield$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim().TrimEnd('.')))
    {
      return false;
    }

    effect = new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Self,
        Filter = new ObjectFilter
        {
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
      Tapped = false,
    };
    return true;
  }
}
