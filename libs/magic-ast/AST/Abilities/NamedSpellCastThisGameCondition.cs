namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "you've cast another spell named [name] this game" — a whole-GAME cast-history gate keyed
/// on a specific card name. The second conjunct of Approach of the Second Sun's win
/// condition ("… and you've cast another spell named Approach of the Second Sun this game").
///
/// <para>
/// The "this game" window is the widest temporal scope (the entire game, CR 104 / 725), far
/// beyond the "this turn" of every existing history predicate — so this is a dedicated node,
/// not a <see cref="CountCondition"/> over a nonexistent this-game cast predicate.
/// <see cref="Name"/> is the exact card name checked (CR 201.2); <see cref="ExcludeSelf"/> is
/// <c>true</c> for "ANOTHER spell named …" (CR 109.5 — a copy other than this one).
/// The caster is always the controller ("you"), so it is not parameterised.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed named-spell cast-history
/// gate; the engine reads whether a matching spell was cast this game, MAST does not
/// pre-evaluate it. Structured rather than left as part of a free-text
/// <see cref="OtherCondition"/> residual (here as a conjunct of an <see cref="AllCondition"/>).
/// </para>
///
/// CR 601 (casting a spell); CR 201.2 (same name); CR 104.2b (Approach's win condition).
/// </summary>
[ConditionKind("namedSpellCastThisGame")]
public sealed record NamedSpellCastThisGameCondition : Condition
{
  /// <summary>The exact card name the cast-history gate checks (CR 201.2) — "Approach of the Second Sun".</summary>
  public required string Name { get; init; }

  /// <summary><c>true</c> for "ANOTHER spell named …" (excludes this very spell, CR 109.5); <c>false</c> for a bare "a spell named …".</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? ExcludeSelf { get; init; }
}
