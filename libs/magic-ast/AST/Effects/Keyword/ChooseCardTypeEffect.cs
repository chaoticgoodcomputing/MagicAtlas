namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Choose artifact, creature, enchantment, instant, or sorcery." — the card-type-choice
/// declaration. The oracle line records that the controller selects a card type; subsequent
/// abilities that reference "the chosen type" (via
/// <see cref="MagicAST.AST.References.ChosenCharacteristicKind.CardType"/>) are downstream
/// consumers of this choice (CR 607 linked abilities). MAST models only the choice declaration
/// itself, not the producer/consumer link.
///
/// <para>Timing is a separate axis: when this choice happens as the permanent enters
/// ("As this artifact enters, choose artifact, creature, enchantment, instant, or sorcery."
/// — Cloud Key, the CR 614.1c shape), the enclosing
/// <see cref="MagicAST.AST.Abilities.StaticAbility"/> carries
/// <see cref="MagicAST.AST.Abilities.StaticTimingKind.AsThisEnters"/>; the effect itself
/// stays plain. Timing and effect are composable, never baked into the effect discriminator.</para>
///
/// <para>Distinct from <see cref="ChooseCreatureTypeEffect"/> (creature-type choice) and
/// <see cref="ChooseColorEffect"/> (color choice): each carries a different descriptive shape
/// in oracle text and a different downstream-reference vocabulary. This node records a
/// card-type-selection decision (the card types being artifact, creature, enchantment, instant,
/// or sorcery — the permanent card types that can also be spell types on the stack).</para>
/// </summary>
[OracleEffect("chooseCardType")]
public sealed record ChooseCardTypeEffect : Effect
{
}
