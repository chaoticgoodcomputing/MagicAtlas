namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// The counting window over which a spell-cast cap is enforced — the "each turn"
/// half of "can't cast more than one spell each turn". Kept as a small descriptive
/// axis so the period is explicit and extensible rather than a bare boolean flag.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SpellCastLimitPeriod>))]
public enum SpellCastLimitPeriod
{
  /// <summary>"each turn" — the cap resets every turn (Eidolon of Rhetoric, Arcane Laboratory).</summary>
  [JsonStringEnumMemberName("eachTurn")]
  EachTurn,
}

/// <summary>
/// A continuous static effect that caps how many spells a player may cast within a
/// counting window — e.g. "Each player can't cast more than one spell each turn."
/// (Eidolon of Rhetoric). This is a rules-of-the-game-modifying continuous effect
/// (CR 611.1: a continuous effect "affects players or the rules of the game, for a
/// fixed or indefinite period").
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine enforces.
/// This effect records the cap (<see cref="MaxPerPeriod"/>), whose casting is
/// capped (<see cref="Player"/>), and the window over which the cap is counted
/// (<see cref="Period"/>). It does NOT model the per-player spell counter, the
/// turn-state reset, or the legality check that removes an over-the-cap spell from
/// the stack — those are engine concerns.
///
/// <para>
/// "Cast" is the keyword action of CR 601.2 ("To cast a spell is to take it from
/// where it is … put it on the stack, and pay its costs …"). The cap counts cast
/// events within the period; it is not a property of any one spell.
/// </para>
///
/// <para>
/// Distinct from <see cref="CantBeCastEffect"/>, which prevents a *class of spells*
/// (described by the containing ability's affected-objects filter) from being cast
/// at all. This effect imposes no per-spell restriction — any spell is castable, but
/// only up to <see cref="MaxPerPeriod"/> of them per <see cref="Period"/> per player.
/// </para>
/// </remarks>
[OracleEffect("cantCastMoreThanNSpells")]
public sealed record CantCastMoreThanNSpellsEffect : Effect
{
  /// <summary>
  /// Whose casting is capped — the scope of the restriction. "Each player" →
  /// <see cref="ObjectReferenceKind.EachPlayer"/>; the symmetric Eidolon shape.
  /// (Asymmetric variants such as "your opponents" would use the matching
  /// player-scope reference.)
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// The cap: the maximum number of spells a capped player may cast per
  /// <see cref="Period"/>. "can't cast more than one spell" → 1.
  /// </summary>
  public required int MaxPerPeriod { get; init; }

  /// <summary>
  /// The counting window over which <see cref="MaxPerPeriod"/> is enforced.
  /// "each turn" → <see cref="SpellCastLimitPeriod.EachTurn"/>.
  /// </summary>
  public required SpellCastLimitPeriod Period { get; init; }
}
