namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return the exiled card to the battlefield under its owner's control." — the
/// LTB resolution clause of a CR 607.2 linked pair (Petravark): the card a prior
/// linked exile ability put in exile is returned to the battlefield.
///
/// "the exiled card" is NOT free text — it is a linked reference (CR 607.2). The
/// returned object is identified by the <see cref="ObjectFilter.ExiledWith"/>
/// reference (the same axis Azula's play-from-exile permission uses): a
/// <see cref="ObjectReferenceKind.Designated"/> card in the
/// <see cref="Zone.Exile"/> zone exiled with this object
/// (<c>ExiledWith = {Kind: Self}</c>). This is a reference, not a threaded runtime
/// binding (ADR 0004 reference-not-resolution); the ETB exile ability and this LTB
/// return ability are two separate triggered abilities, linked by CR 607.2.
///
/// "under its owner's control" rides on <see cref="ReturnToBattlefieldEffect.UnderControl"/>
/// as an <see cref="ObjectReferenceKind.Owner"/> reference (CR 400.6 — an object
/// returning under a specific player's control enters under that control).
/// </summary>
[TriggeredRule]
public sealed class ReturnExiledCardOnLeaveTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^return\s+the\s+exiled\s+card\s+to\s+the\s+battlefield\s+under\s+its\s+owner'?s\s+control$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new ReturnToBattlefieldEffect
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
    };
    return true;
  }
}
