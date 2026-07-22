namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[During your turn,] you may [play|cast] cards exiled with [object]
/// [as though they had flash]. [Mana of any type can be spent to cast those
/// spells.]" — a static permission to play cards from exile (Azula, Cunning
/// Usurper).
///
/// <para>
/// The permission is the linked second ability of a CR 406.6 pair: it refers to
/// "cards exiled with [object]" via the <see cref="Cards"/> filter's
/// <see cref="ObjectFilter.ExiledWith"/> reference, NOT by threading a binding
/// from the exile ability (ADR 0004 "reference not resolution"). It is a separate
/// static ability rather than a one-shot bundled inside the exile trigger
/// (ADR 0004 "topology not annotation").
/// </para>
/// </summary>
[OracleEffect("mayPlayFromExile")]
public sealed record MayPlayFromExileEffect : Effect
{
  /// <summary>
  /// Which cards may be played — an exile-zone filter, typically carrying
  /// <see cref="ObjectFilter.ExiledWith"/> to identify the linking object.
  /// </summary>
  public required ObjectFilter Cards { get; init; }

  /// <summary>
  /// What the controller may do with the cards — cast spells, play lands, or both.
  /// </summary>
  public required IReadOnlyList<PlayFromExileAction> Actions { get; init; }

  /// <summary>
  /// Restricts the permission to a particular player's turn ("During your turn").
  /// Null when the permission is unconditioned.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ControllerFilter? WhoseTurn { get; init; }

  /// <summary>
  /// "as though they had flash" — the cards may be cast at instant speed
  /// (CR 702.8). Null/false when no timing relaxation is granted.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? AsThoughFlash { get; init; }

  /// <summary>
  /// "Mana of any type can be spent to cast those spells" — relaxes the mana
  /// restriction. Null when no mana-spend relaxation is stated.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ManaSpendRelaxation? ManaSpend { get; init; }

  /// <summary>
  /// "without paying its mana cost" — the card may be played/cast paying no mana cost at all
  /// (CR 118.5 / 601.2f alternative cost of nothing), as opposed to merely relaxing WHICH mana
  /// pays (<see cref="ManaSpend"/>). Windbrisk Heights: "you may play the exiled card without
  /// paying its mana cost". Null/false when the normal cost still applies. Distinct from
  /// <see cref="ManaSpend"/>: that keeps the cost but frees its colour; this waives the cost.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? WithoutPayingManaCost { get; init; }
}

/// <summary>
/// What a <see cref="MayPlayFromExileEffect"/> permits doing with the cards.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlayFromExileAction
{
  /// <summary>"you may cast [those] cards" — cast spells only.</summary>
  CastSpells,

  /// <summary>"you may play [those] cards" — play (cast spells or play lands).</summary>
  PlayCards,
}

/// <summary>
/// How the mana restriction is relaxed when casting cards under a
/// <see cref="MayPlayFromExileEffect"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ManaSpendRelaxation
{
  /// <summary>"Mana of any type can be spent to cast those spells."</summary>
  [JsonStringEnumMemberName("anyType")]
  AnyType,
}
