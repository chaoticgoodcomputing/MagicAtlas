namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[This permanent] becomes a copy of [target] [until end of turn]." — a layer-1
/// copy effect (CR 707) that makes the source object become a copy of the named
/// object for the stated duration.
///
/// <para>
/// Distinct from <see cref="MagicAST.AST.Effects.TokenCopy.CopyEffect"/>, which
/// <em>creates a new token or spell copy</em> (CR 707.1: "create a token that's a
/// copy of"). <c>BecomesCopyEffect</c> modifies the existing permanent in place —
/// no new object is created (CR 707.6: "Some effects cause a permanent that's
/// copying a permanent to copy a different object while remaining on the
/// battlefield."). The subject is the source of the ability (typically Self); the
/// <see cref="CopyTarget"/> is the object it becomes a copy of.
/// </para>
///
/// <para>
/// CR 707.2 governs which characteristics are copied; CR 707.6 governs the
/// remain-in-place semantics. Both are engine territory and are not modelled here
/// (descriptive-not-executive doctrine).
/// </para>
///
/// <para>
/// <b>Examples:</b>
/// Shifting Woodland — "Delirium — {2}{G}{G}: This land becomes a copy of target
/// permanent card in your graveyard until end of turn." → Subject: Self,
/// CopyTarget: Target(CardTypes:["permanent"], Zone:Graveyard, Controller:You),
/// Duration: untilEndOfTurn.
/// </para>
/// </summary>
[OracleEffect("becomesCopy")]
public sealed record BecomesCopyEffect : ContinuousEffect
{
  /// <summary>
  /// The permanent that becomes the copy. For the Shifting Woodland template this
  /// is <see cref="ObjectReferenceKind.Self"/> ("this land").
  /// </summary>
  public required ObjectReference Subject { get; init; }

  /// <summary>
  /// The object that is being copied — the target of the "becomes a copy of"
  /// phrase. Carries the zone, card-type, and controller/owner constraints from
  /// the oracle text (e.g. "target permanent card in your graveyard").
  /// </summary>
  public required ObjectReference CopyTarget { get; init; }
}
