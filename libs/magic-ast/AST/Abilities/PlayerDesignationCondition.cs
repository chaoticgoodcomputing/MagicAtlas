namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "you have the city's blessing" — a gate on whether you currently hold a player
/// DESIGNATION (CR 700.x — a designation is a status a player can have, distinct from
/// an object's characteristics). The "as long as …" grants the Ascend family prints
/// key on this: Dusk Charger ("gets +2/+2 as long as you have the city's blessing"),
/// Skymarcher Aspirant ("has flying as long as …"), Storm Fleet Swashbuckler ("has
/// double strike as long as …"). Ascend (CR 702.131) is the keyword that CONFERS the
/// city's-blessing designation once you control ten or more permanents; this condition
/// is the CONSUMER half — it reads whether the designation is currently held.
///
/// <para>
/// <see cref="Designation"/> names which designation is checked. The city's blessing
/// (CR 702.131b) is the only value the current family needs; the enum is the extension
/// point for the sibling player designations (the monarch, CR 725; the initiative)
/// should a "you are the monarch"-style gate land later. The subject is the controller
/// ("you have …"); it is implicit rather than a field because every observed surface is
/// self-scoped — a future "an opponent has the city's blessing" surface would add a
/// controller axis then, not speculatively now.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed designation gate; the
/// engine reads whether you actually hold the designation, MAST does not pre-evaluate
/// it. Structured to this dedicated <see cref="Condition"/> arm rather than left as a
/// free-text <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 702.131b (excerpt): "Ascend … on a permanent … represents a static ability … 'If
/// you control ten or more permanents and you don't have the city's blessing, you get
/// the city's blessing for the rest of the game.'"
/// </summary>
[ConditionKind("designation")]
public sealed record PlayerDesignationCondition : Condition
{
  /// <summary>The player designation being checked — <see cref="PlayerDesignation.CityBlessing"/>.</summary>
  public required PlayerDesignation Designation { get; init; }
}

/// <summary>
/// A player designation a <see cref="PlayerDesignationCondition"/> can check for — a status
/// a player can have (CR 700.x), recorded as written (reference-not-resolution, ADR 0004).
/// Seeded with the city's blessing (the Ascend family, CR 702.131); the monarch (CR 725)
/// and other designations are added as the families that reference them land.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlayerDesignation
{
  /// <summary>The city's blessing (CR 702.131b) — conferred by Ascend, read by "you have the city's blessing".</summary>
  CityBlessing,
}
