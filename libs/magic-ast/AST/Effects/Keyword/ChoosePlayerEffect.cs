namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.Effects.Traits;
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
}
