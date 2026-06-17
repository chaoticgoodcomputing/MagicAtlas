namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "It's an [type]." / "It's a [type]." — a layer-4 (CR 613.1d) card-type–setting
/// continuous effect that declares the subject's card types, overriding its
/// existing types. Distinct from <see cref="BecomesCreatureEffect"/> (which
/// <em>adds</em> a creature type alongside existing types and pins P/T and
/// abilities) and from <see cref="ChangeSubtypeEffect"/> (which operates on
/// subtypes, not card types).
///
/// <para>
/// CR 613.1d: "Layer 4: Type-changing effects are applied. These include effects
/// that change an object's card type, subtype, and/or supertype." An object whose
/// card types are set to <c>["enchantment"]</c> ceases to be a creature; it is only
/// an enchantment. This is a descriptive record of the oracle instruction; the
/// engine applies the effect in layer 4 and resolves all downstream consequences
/// (loss of creature status, P/T, creature-type subtypes per CR 205.1a, etc.)
/// without MAST encoding them (descriptive-not-executive doctrine, ADR 0003).
/// </para>
///
/// <para>
/// Examples:
/// <list type="bullet">
///   <item>Enduring Tenacity — dies trigger: "… return it to the battlefield under
///   its owner's control. It's an enchantment." → Subject: It, CardTypes:
///   ["enchantment"]. The permanent re-enters as a pure enchantment; the "it's not
///   a creature" parenthetical is a clarifying note, not a separate effect.</item>
/// </list>
/// </para>
/// </summary>
[OracleEffect("setCardTypes")]
public sealed record SetCardTypesEffect : ContinuousEffect
{
  /// <summary>
  /// The permanent whose card types are being set. Typically
  /// <see cref="ObjectReferenceKind.It"/> ("it") in a triggered-ability context
  /// where "it" refers back to the subject of the trigger.
  /// </summary>
  public required ObjectReference Subject { get; init; }

  /// <summary>
  /// The complete card-type set the permanent is declared to have (CR 205.2),
  /// lowercase to match the <see cref="ObjectFilter.CardTypes"/> convention —
  /// e.g. <c>["enchantment"]</c>. This is a <em>set</em> operation: any card
  /// types the permanent previously had that are not in this list are no longer
  /// present (CR 613.1d).
  /// </summary>
  public required IReadOnlyList<string> CardTypes { get; init; }
}
