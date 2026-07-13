namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Choose a player." — the player-choice declaration. The oracle line records
/// that the controller selects a player; subsequent abilities that reference
/// "the chosen player" (via <see cref="MagicAST.AST.References.ControllerFilter.ChosenPlayer"/>)
/// are downstream consumers of this choice. MAST models only the choice
/// declaration itself, not the producer/consumer link.
///
/// <para>Timing is a separate axis: when this choice happens as the permanent
/// enters ("As this creature enters, choose a player." — Sawhorn Nemesis, CR
/// 614.1c), the enclosing <see cref="MagicAST.AST.Abilities.StaticAbility"/>
/// carries <see cref="MagicAST.AST.Abilities.StaticTimingKind.AsThisEnters"/>;
/// the effect itself stays plain. Timing and effect are composable, never
/// baked into the effect discriminator.</para>
///
/// <para>Sibling of <see cref="ChooseColorEffect"/> / <see cref="ChooseCreatureTypeEffect"/>
/// / <see cref="ChooseCardTypeEffect"/> / <see cref="ChooseBasicLandTypeEffect"/>: the
/// surface noun chosen ("a player") differs from color/creature-type/card-type/
/// land-type, and downstream references to "the chosen player" name a distinct
/// entity than "the chosen color"/"the chosen type", so this is a separate node
/// rather than a variant of an existing chosen-value effect (per the MAST
/// descriptive-not-engine doctrine).</para>
/// </summary>
[OracleEffect("choosePlayer")]
public sealed record ChoosePlayerEffect : Effect
{
  /// <summary>
  /// Restricts the pool of players eligible to be chosen — "choose an opponent"
  /// (The Rack, CR 614.12 as-enters replacement binding) sets this to
  /// <see cref="ControllerFilter.Opponent"/>. Null for the unrestricted "choose a
  /// player" printing (Sawhorn Nemesis), which may choose any player including
  /// the controller. Reuses <see cref="ControllerFilter"/> — the same closed
  /// player-identity vocabulary <see cref="MagicAST.AST.References.GameTime.Whose"/>
  /// and <see cref="MagicAST.AST.Effects.Replacement.TurnPartEvent.Whose"/> already
  /// use for "whose turn/step" — rather than a free-text restriction string,
  /// because "opponent" is a closed, structured concept the AST already names,
  /// not open-ended prose.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ControllerFilter? Scope { get; init; }
}
