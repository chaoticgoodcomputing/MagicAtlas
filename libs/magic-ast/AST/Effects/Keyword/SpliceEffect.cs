namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Splice (Rule 702.47). Printed as "Splice onto [subtype] [cost]" (e.g.
/// "Splice onto Arcane {G}"). While a card with splice is in hand, its controller
/// may reveal it as they cast a spell of the named subtype and pay the splice
/// cost to graft this card's instructions onto that spell.
///
/// <para>
/// Per the descriptive-not-engine doctrine, MAST records only the two printed
/// parameters: the spell <see cref="Subtype"/> a spell must share to be a legal
/// splice target, and the <see cref="Cost"/> paid to splice. The text-grafting
/// machinery (revealing, copying instructions onto the target spell) is reminder
/// text and is conventionally inferred from the rules — it is not modeled here.
/// </para>
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// cost-bearing keyword effects (Bestow, Cycling, Flashback) — every printed
/// splice cost is a <see cref="ManaCost"/>, but the base accommodates future
/// variants without a schema change.
/// </para>
/// </summary>
[OracleEffect("splice")]
public sealed record SpliceEffect : Effect
{
  /// <summary>
  /// The spell this card may be spliced onto, as a filter — "Splice onto Arcane"
  /// → <c>Onto = { Subtypes: ["Arcane"] }</c>. Reshaped from the former bare
  /// <c>Subtype</c> string to the shared <see cref="MagicAST.AST.References.ObjectFilter"/>
  /// primitive (ADR 0003), so "spells of subtype X" clusters with every other subtype
  /// filter rather than hiding in a splice-only string.
  /// </summary>
  public required MagicAST.AST.References.ObjectFilter Onto { get; init; }

  /// <summary>
  /// The cost paid to splice this card onto a spell. Every printed splice cost is
  /// a <see cref="ManaCost"/>, but the polymorphic <see cref="Cost"/> base
  /// accommodates future variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
