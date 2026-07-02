namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Prototype (Rule 702.160, Rule 718). A keyword ability printed as
/// "Prototype {cost} — P/T" (e.g. "Prototype {1}{B} — 1/1"). It lets a card be
/// cast for an alternative cost, as a smaller creature of a different color and
/// size that keeps its abilities and types.
///
/// <para>
/// Per the descriptive-not-engine doctrine, MAST records only what the printed
/// line states: the alternative prototype <see cref="Cost"/> and the prototype
/// <see cref="Power"/>/<see cref="Toughness"/> the card has when cast that way.
/// The alternative-characteristics casting rules ("you may cast … it keeps its
/// abilities and types") are engine territory captured by the reminder text, not
/// modelled as structure here.
/// </para>
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// cost-bearing keyword effects (Cycling, Bestow, Equip) — every printing uses a
/// <see cref="ManaCost"/>, but the base accommodates future variants.
/// <see cref="Power"/> and <see cref="Toughness"/> are kept as raw strings
/// because printed P/T values may be non-numeric (e.g. "*"), mirroring the
/// level-up stanza P/T treatment.
/// </para>
/// </summary>
[OracleEffect("prototype")]
public sealed record PrototypeEffect : Effect
{
  /// <summary>
  /// The alternative prototype cost paid to cast the card in its smaller form.
  /// Most commonly a <see cref="ManaCost"/>, but the polymorphic <see cref="Cost"/>
  /// base accommodates future non-mana variants.
  /// </summary>
  public required Cost Cost { get; init; }

  /// <summary>The prototype power printed after the em dash (e.g. "Prototype {1}{B} — 1/1" → "1").</summary>
  public required string Power { get; init; }

  /// <summary>The prototype toughness printed after the slash (e.g. "Prototype {1}{B} — 1/1" → "1").</summary>
  public required string Toughness { get; init; }
}
