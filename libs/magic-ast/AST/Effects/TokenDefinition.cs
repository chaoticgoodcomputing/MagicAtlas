namespace MagicAST.AST.Effects;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;

/// <summary>
/// Defines a token that can be created.
/// </summary>
public sealed record TokenDefinition
{
  /// <summary>
  /// Power of the token (for creatures).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Power { get; init; }

  /// <summary>
  /// Toughness of the token (for creatures).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Toughness { get; init; }

  /// <summary>
  /// Colors of the token.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Colors { get; init; }

  /// <summary>
  /// Card types of the token.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Types { get; init; }

  /// <summary>
  /// Subtypes of the token.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Subtypes { get; init; }

  /// <summary>
  /// Supertypes of the token (e.g. "Legendary" — CR 205.4a). Mirrors the
  /// <c>Supertypes</c> field on the card's own <c>TypeLine</c>/<c>ObjectFilter</c>
  /// (CR 205.4a: "Legendary" and other supertypes). Some named tokens are printed
  /// with an explicit supertype in their creation text (e.g. "create Cragflame, a
  /// legendary colorless Equipment artifact token …" — Mabel, Heir to Cragflame),
  /// distinct from <see cref="Types"/>/<see cref="Subtypes"/> (CR 205.4b:
  /// supertypes are not card types or subtypes).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Supertypes { get; init; }

  /// <summary>
  /// Name of the token if specified.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Name { get; init; }

  /// <summary>
  /// Abilities the token has.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<Ability>? Abilities { get; init; }

  /// <summary>
  /// Raw ability text if abilities aren't fully parsed.
  /// </summary>
  [FreeTextField]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? AbilityText { get; init; }

  /// <summary>
  /// True if this is a copy of another object.
  /// </summary>
  public bool IsCopy { get; init; }

  /// <summary>
  /// True when the token enters the battlefield tapped (e.g. "create a tapped Treasure token" —
  /// CR 110.6b: "A token enters the battlefield under the control of the player who created it."
  /// CR 305.9 / 302.6: a permanent can be instructed to enter tapped by an effect). Null (omitted
  /// in JSON) when the token enters normally (untapped). Distinct from the permanent's current
  /// tapped state at resolution: this is a declarative entry modifier, not a tap action.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? EntersTapped { get; init; }

  /// <summary>
  /// True when the token enters the battlefield already attacking (e.g. "create a 1/1 white
  /// Warrior creature token that's tapped and attacking" — Najeela, the Blade-Blossom). CR 508.4:
  /// "an effect can put a creature onto the battlefield attacking"; such a creature is a declared
  /// attacker without having been declared during the declare-attackers step (CR 508.1) and was
  /// never "declared as an attacker" for triggers that check that. Null (omitted in JSON) when the
  /// token enters without attacking. Declarative entry modifier paralleling <see cref="EntersTapped"/>;
  /// the choice of which player/planeswalker/battle it attacks is engine territory.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? EntersAttacking { get; init; }

  // Factory methods for common tokens.
  //
  // CR 111.10 predefined tokens (Treasure, Food, Clue, Blood, …) have their activated ability
  // defined by the game rules, not by the creating card. The oracle text of a card that creates
  // one prints that ability only as parenthetical reminder text (CR 207.2 — reminder text has no
  // rules meaning), which the parser strips. Following the Map/Powerstone clean precedent, the
  // predefined ability body is therefore NOT re-asserted here as free text: the token is identified
  // structurally by its artifact type + named subtype, and its intrinsic affordance is resolved
  // downstream from that subtype via PredefinedTokens.Registry (mast-interaction, ADR-0002 §9).
  // Nothing reads <see cref="AbilityText"/> on these tokens; carrying it only created a free-text
  // residual (ADR-0004 recursive-body de-string initiative, issue #40).
  public static TokenDefinition Treasure() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Treasure"],
    };

  public static TokenDefinition TappedTreasure() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Treasure"],
      EntersTapped = true,
    };

  public static TokenDefinition Food() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Food"],
    };

  public static TokenDefinition Clue() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Clue"],
    };

  public static TokenDefinition Blood() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Blood"],
    };

  /// <summary>
  /// A Map token — a colorless artifact predefined token (CR 111.10 — predefined tokens).
  /// Its predefined activated ability ("{1}, {T}, Sacrifice this token: Target creature you
  /// control explores. Activate only as a sorcery.") rides on the parenthetical reminder text
  /// the parser strips (CR 207.2 — reminder text has no rules meaning), so — following the
  /// Powerstone precedent added clean in <c>CreateTappedPredefinedTokenRule</c> — the ability
  /// body is NOT re-asserted here as free text. The token is identified structurally by its
  /// artifact type + named "Map" subtype.
  /// </summary>
  public static TokenDefinition Map() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Map"],
    };
}
