namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return the exiled card to its owner's hand." — the LTB resolution clause of a
/// CR 607 (Linked Abilities) exile/return pair where the return zone is the HAND
/// rather than the battlefield. This is the hand-return sibling of
/// <see cref="ReturnExiledCardOnLeaveTriggeredRule"/> (the Petravark battlefield
/// shape). Exemplars: Ashiok's Erasure (ETB "exile target spell" + this LTB return),
/// and the broader two-trigger family (Mesmeric Fiend, Tidehollow Sculler, the
/// Champion/Champions cards) where an earlier linked ability exiled the card.
///
/// <para>
/// "the exiled card" is NOT free text — it is a linked reference (CR 607, Linked
/// Abilities): the same axis the battlefield-return rule and Petravark's gold use.
/// The returned object is identified by <see cref="ObjectFilter.ExiledWith"/> as a
/// <see cref="ObjectReferenceKind.Designated"/> card in the <see cref="Zone.Exile"/>
/// zone exiled with this object (<c>ExiledWith = {Kind: Self}</c>). This is a
/// reference, not a threaded runtime binding (ADR 0004 reference-not-resolution):
/// the ETB exile ability and this LTB return ability are two separate triggered
/// abilities, linked by CR 607.
/// </para>
///
/// <para>
/// The card returns to ITS OWNER'S hand by default (CR 406, Exile — a card leaving
/// exile goes to its owner's hand), so unlike the battlefield-return shape there is
/// no explicit "under its owner's control" rider to carry: hand has no controller,
/// only an owner. The plural / multi-owner surfaces ("the exiled cards to their
/// owner's hand" / "to their owners' hands") are intentionally NOT matched here —
/// they pair with mass-exile ETB abilities (Wormfang Behemoth, Angel of Serenity)
/// that are separate parser slices.
/// </para>
///
/// Rule citations: CR 607 (Linked Abilities); CR 406 (Exile).
/// </summary>
[TriggeredRule]
public sealed class ReturnExiledCardToHandOnLeaveTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^return\s+the\s+exiled\s+card\s+to\s+its\s+owner'?s\s+hand$",
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

    effect = new ReturnToHandEffect
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
    };
    return true;
  }
}
