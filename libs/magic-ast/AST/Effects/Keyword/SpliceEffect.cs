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
public sealed record SpliceEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The spell subtype a spell must share for this card to be spliced onto it
  /// (e.g. "Arcane"). Printed verbatim after "Splice onto".
  /// </summary>
  public required string Subtype { get; init; }

  /// <summary>
  /// The cost paid to splice this card onto a spell. Every printed splice cost is
  /// a <see cref="ManaCost"/>, but the polymorphic <see cref="Cost"/> base
  /// accommodates future variants.
  /// </summary>
  public required Cost Cost { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to perform this one. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
