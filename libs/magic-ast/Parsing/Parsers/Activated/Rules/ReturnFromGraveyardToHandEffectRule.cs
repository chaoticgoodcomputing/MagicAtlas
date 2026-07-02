namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return this card from your graveyard to your hand." — the self-retrieval
/// pattern on activated abilities (e.g. Abzan Devotee). Models a graveyard-to-hand
/// zone change where the source zone is the controller's graveyard (CR 404) and the
/// destination is the controller's hand (CR 402). The permanent is in the graveyard
/// at the time the cost is paid and ability resolves; Target = Self with a Graveyard
/// zone qualifier captures both the identity of the object and its source zone.
///
/// Anchored on "from your graveyard" to avoid matching battlefield-self bounce
/// (F6 sibling: "to its owner's hand"), which has no graveyard source qualifier.
/// </summary>
[ActivatedEffectRule(Priority = 987)]
public sealed class ReturnFromGraveyardToHandEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    if (!Regex.IsMatch(
          trimmed,
          @"^Return\s+this\s+card\s+from\s+your\s+graveyard\s+to\s+your\s+hand$",
          RegexOptions.IgnoreCase))
    {
      return null;
    }

    return new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Self,
        Filter = new ObjectFilter
        {
          Zone = Zone.Graveyard,
        },
      },
    };
  }
}
