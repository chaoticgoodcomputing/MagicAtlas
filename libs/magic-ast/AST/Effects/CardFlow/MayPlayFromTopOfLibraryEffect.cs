namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may play lands and cast spells from the top of your library." — a static permission
/// allowing the controller to play cards from the top of their library rather than from hand,
/// optionally with an alternative cost for spells cast this way.
///
/// <para>
/// Bolas's Citadel (WAR) grants this permission with an attached alternative cost:
/// "If you cast a spell this way, pay life equal to its mana value rather than pay its mana cost."
/// (CR 118.9b: paying life is an alternative cost; CR 202.3: a spell's mana value is the
/// total cost of its mana symbols.) The <see cref="SpellAltCost"/> field records the
/// alternative cost kind for spells cast this way — null when no alternative cost applies.
/// </para>
///
/// <para>
/// MAST describes the permission as written: the controller may play lands and/or cast spells
/// from the top of their library. What "the top" means and when the controller may exercise
/// this permission are engine territory (CR 305.1 for playing lands, CR 601.2 for casting
/// spells). The effect's <see cref="Actions"/> list records which permissions are granted
/// (play lands, cast spells, or both). Describing, not executing (ADR 0003).
/// </para>
/// </summary>
[OracleEffect("mayPlayFromTopOfLibrary")]
public sealed record MayPlayFromTopOfLibraryEffect : Effect
{
  /// <summary>
  /// What the controller is permitted to do from the top of their library.
  /// One or more of <see cref="PlayFromTopAction"/> — Bolas's Citadel grants both
  /// <see cref="PlayFromTopAction.PlayLands"/> and <see cref="PlayFromTopAction.CastSpells"/>.
  /// </summary>
  public required IReadOnlyList<PlayFromTopAction> Actions { get; init; }

  /// <summary>
  /// Alternative cost kind for spells cast from the top of the library this way.
  /// <see cref="TopOfLibrarySpellAltCost.PayLifeEqualToManaValue"/> for Bolas's Citadel
  /// ("pay life equal to its mana value rather than pay its mana cost" — CR 118.9b, CR 202.3).
  /// Null when spells are cast for their normal cost (no alternative cost rider).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public TopOfLibrarySpellAltCost? SpellAltCost { get; init; }
}

/// <summary>
/// Actions permitted by a <see cref="MayPlayFromTopOfLibraryEffect"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlayFromTopAction
{
  /// <summary>"you may play lands" from the top of your library.</summary>
  PlayLands,

  /// <summary>"you may cast spells" from the top of your library.</summary>
  CastSpells,
}

/// <summary>
/// Alternative cost kinds for spells cast via <see cref="MayPlayFromTopOfLibraryEffect"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TopOfLibrarySpellAltCost
{
  /// <summary>
  /// "pay life equal to its mana value rather than pay its mana cost" (Bolas's Citadel,
  /// CR 118.9b — paying life is an alternative cost; CR 202.3 — mana value is the total
  /// numeric value of the mana cost). The spell's mana value is derived from its own mana
  /// cost at cast time; MAST records the rule reference, not a runtime value.
  /// </summary>
  [JsonStringEnumMemberName("payLifeEqualToManaValue")]
  PayLifeEqualToManaValue,
}
