namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Choose a card name." — the card-name-choice declaration (CR 201.4-adjacent
/// naming; the as-enters chosen-value binding is CR 614.12, the same replacement
/// mechanism that underlies <see cref="ChooseBasicLandTypeEffect"/>). The oracle
/// line records that the controller names a card; subsequent abilities that
/// reference "the chosen name" (via
/// <see cref="MagicAST.AST.References.ChosenCharacteristicKind.CardName"/>) are
/// downstream consumers of this choice (CR 607 linked abilities). MAST models
/// only the choice declaration itself, not the producer/consumer link.
///
/// <para>Timing is a separate axis: when this choice happens as the permanent
/// enters ("As this enchantment enters, choose a card name." — Declaration of
/// Naught, CR 614.12), the enclosing <see cref="MagicAST.AST.Abilities.StaticAbility"/>
/// carries <see cref="MagicAST.AST.Abilities.StaticTimingKind.AsThisEnters"/>; the
/// effect itself stays plain. Timing and effect are composable, never baked into
/// the effect discriminator.</para>
///
/// <para>Sibling of <see cref="ChoosePlayerEffect"/> / <see cref="ChooseColorEffect"/>
/// / <see cref="ChooseCreatureTypeEffect"/> / <see cref="ChooseCardTypeEffect"/> /
/// <see cref="ChooseBasicLandTypeEffect"/>: the surface noun chosen ("a card name")
/// differs from player/color/creature-type/card-type/land-type, and downstream
/// references to "the chosen name" name a distinct entity than those siblings'
/// chosen values, so this is a separate node rather than a variant of an existing
/// chosen-value effect (per the MAST descriptive-not-engine doctrine).</para>
/// </summary>
[OracleEffect("chooseCardName")]
public sealed record ChooseCardNameEffect : Effect
{
}
