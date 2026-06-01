namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[This permanent] becomes a [P/T] [colors] [subtypes] [card types] creature
/// with [keywords] [until end of turn]." — the Keyrune/Monument "animate" template:
/// a single continuous effect that, for a duration, makes the source permanent into
/// a creature with a fully-specified characteristic set.
///
/// <para>
/// There is no single keyword <em>action</em> for "becomes a creature" — the line
/// is one continuous effect (CR 611) that SETS several characteristics of one object
/// at once. The relevant characteristic rules:
/// <list type="bullet">
///   <item><b>CR 205</b> — card types (the added "creature" type) and subtypes
///   (the added "Bird"/"Beast" creature type).</item>
///   <item><b>CR 208</b> — power and toughness (the stated 2/2, 3/2 box).</item>
///   <item><b>CR 105</b> — colors (the stated "white and blue" / "red and green").</item>
///   <item><b>CR 113.6/702</b> — the granted keyword ability (flying, trample).</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Why one composite node rather than a <c>CompositeEffect</c> of primitives.</b>
/// MAST is descriptive: it models what the oracle line SAYS, not how a rules engine
/// applies it. The line is grammatically and conceptually ONE thing — "this
/// permanent becomes a [full spec]" — so a single faithful node is truer than a bag
/// of five independent effects (<c>definePT</c> + <c>changeColor</c> +
/// <c>changeSubtype</c> + add-type + <c>gainAbility</c>) that would imply five
/// separately-stated effects. The single <see cref="ContinuousEffect.Duration"/>
/// scopes the whole transformation as a unit (it would otherwise have to be
/// replicated onto each child). Reusing <see cref="ChangeColorEffect"/> /
/// <see cref="ChangeSubtypeEffect"/> would also import their replace / Aura-attached
/// semantics, which do not fit an additive animate, and there is no existing
/// "add a card type" primitive at all.
/// </para>
///
/// <para>
/// Layer/timestamp ordering (CR 613) — how this continuous effect interacts with
/// others, which layer each characteristic change lands in — is ENGINE territory and
/// is deliberately not modeled here (descriptive-not-executive doctrine).
/// </para>
///
/// <para>
/// Examples:
/// <list type="bullet">
///   <item>Azorius Keyrune — "This artifact becomes a 2/2 white and blue Bird
///   artifact creature with flying until end of turn." → Subject: Self, Power/
///   Toughness: 2/2, Colors: ["W","U"], AddedSubtypes: ["Bird"], CardTypes:
///   ["artifact","creature"], GainedAbilities: [flying], Duration: untilEndOfTurn.</item>
///   <item>Gruul Keyrune — "This artifact becomes a 3/2 red and green Beast artifact
///   creature with trample until end of turn." → 3/2, ["R","G"], ["Beast"],
///   ["artifact","creature"], [trample], untilEndOfTurn.</item>
/// </list>
/// </para>
/// </summary>
[OracleEffect("becomesCreature")]
public sealed record BecomesCreatureEffect : ContinuousEffect
{
  /// <summary>
  /// The permanent that becomes a creature. For the Keyrune/Monument template this
  /// is <see cref="ObjectReferenceKind.Self"/> ("this artifact" — the source).
  /// </summary>
  public required ObjectReference Subject { get; init; }

  /// <summary>
  /// The power the permanent's box is set to (CR 208) — e.g. 2 for "a 2/2". A
  /// <see cref="Quantity"/> rather than a bare int so variable animate ("an X/X")
  /// is expressible; the fixed Keyrune case is a <see cref="LiteralQuantity"/>.
  /// </summary>
  public required Quantity Power { get; init; }

  /// <summary>
  /// The toughness the permanent's box is set to (CR 208).
  /// </summary>
  public required Quantity Toughness { get; init; }

  /// <summary>
  /// The full color set the permanent becomes (CR 105), as WUBRG codes — e.g.
  /// ["W","U"] for "white and blue". The animate template states the complete
  /// color spec, so this is the set the permanent has for the duration.
  /// </summary>
  public required IReadOnlyList<string> Colors { get; init; }

  /// <summary>
  /// The card types the permanent has for the duration (CR 205.2), lowercase to
  /// match the <see cref="ObjectFilter.CardTypes"/> convention — e.g.
  /// ["artifact","creature"] for "artifact creature" (the existing artifact type is
  /// retained and the creature type is added).
  /// </summary>
  public required IReadOnlyList<string> CardTypes { get; init; }

  /// <summary>
  /// The creature subtypes added (CR 205.3) — e.g. ["Bird"], ["Beast"]. Title-cased
  /// to match the <see cref="ObjectFilter.Subtypes"/> convention.
  /// </summary>
  public required IReadOnlyList<string> AddedSubtypes { get; init; }

  /// <summary>
  /// The keyword abilities granted as part of the transformation (CR 113.6) — e.g.
  /// a flying / trample <see cref="StaticAbility"/>. Each is a full structured
  /// ability, mirroring <see cref="GainAbilityEffect.GainedAbility"/>. Empty when
  /// the animate spec grants no keywords.
  /// </summary>
  public required IReadOnlyList<Ability> GainedAbilities { get; init; }
}
