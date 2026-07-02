namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Choose a creature type." — the creature-type-choice declaration. The oracle
/// line records that the controller selects a creature type; subsequent abilities
/// that reference "the chosen type" are downstream consumers of this choice. MAST
/// models only the choice declaration itself, not the producer/consumer link.
///
/// <para>Timing is a separate axis: when this choice happens as the permanent
/// enters ("As this land enters, choose a creature type." — the Unclaimed
/// Territory shape, CR 614.1c), the enclosing <see cref="MagicAST.AST.Abilities.StaticAbility"/>
/// carries <see cref="MagicAST.AST.Abilities.StaticTimingKind.AsThisEnters"/>; the
/// effect itself stays plain. Timing and effect are composable, never baked into
/// the effect discriminator.</para>
///
/// <para>Design rationale: This is a separate effect type rather than a variant
/// of <see cref="ChooseColorEffect"/> because the surface noun chosen differs —
/// a creature type versus a color — and the two carry distinct downstream-reference
/// vocabularies ("the chosen type" versus "the chosen color"). Both are explicit
/// player-decision instructions, but collapsing them into one node would erase the
/// descriptive distinction the oracle text draws (per the MAST
/// descriptive-not-engine doctrine).</para>
///
/// <para>Distinct from <see cref="ChooseColorEffect"/> (color choice) and from
/// <c>PayLifeEffect</c> (cost-payment decision, Shockland pattern): each carries a
/// different descriptive shape in oracle text. This node records a
/// creature-type-selection decision.</para>
/// </summary>
[OracleEffect("chooseCreatureType")]
public sealed record ChooseCreatureTypeEffect : Effect
{
  /// <summary>
  /// Optional restriction on the creature-type choice (e.g. "choose a creature
  /// type other than Wall"). Null for unrestricted "choose a creature type"
  /// printings. Stored verbatim from oracle text as a free-form string; MAST
  /// does not parse the exclusion further.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Restriction { get; init; }
}
