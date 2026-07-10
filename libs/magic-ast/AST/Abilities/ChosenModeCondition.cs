namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "if you chose [Name]" — the option-gate of the Believe/Doubt named-mode family
/// (Phenomenon Investigators). True when the named mode this ability belongs to is
/// the one the controller chose as the source permanent entered the battlefield
/// (the choice recorded by
/// <see cref="MagicAST.AST.Effects.Keyword.ChooseNamedOptionEffect"/>). The two
/// ability lines a Believe/Doubt card prints — each labelled with an option name —
/// are ordinary abilities that FUNCTION only under the matching mode; this condition
/// is the printed gate that couples an ability to its option.
///
/// <para>
/// Consumer half of the choose-named-option producer/consumer duality (ADR 0003/0004,
/// reference-not-resolution): <see cref="Mode"/> is keyed to the producing effect's
/// <see cref="MagicAST.AST.Effects.Keyword.ChooseNamedOptionEffect.Options"/> entry
/// on the same card, NOT a pre-resolved boolean threaded from the choice. Carried in
/// <see cref="TriggeredAbility.InterveningIf"/> (or <see cref="StaticAbility.Condition"/>)
/// so it composes with the ordinary trigger/static shape rather than forming a
/// mode-specific ability discriminator.
/// </para>
///
/// <para>
/// CR 700.2 (modal): a spell or ability with two or more named options. CR 614.12 —
/// the Believe/Doubt cards make the modal choice as the object enters and then gate
/// ongoing abilities on the chosen mode, rather than resolving one option once.
/// </para>
/// </summary>
[ConditionKind("chosenMode")]
public sealed record ChosenModeCondition : Condition
{
  /// <summary>
  /// The mode name whose selection makes this condition true — e.g. <c>"Believe"</c>
  /// or <c>"Doubt"</c>. Matches one of the
  /// <see cref="MagicAST.AST.Effects.Keyword.ChooseNamedOptionEffect.Options"/> printed
  /// on the same card. A verbatim card-specific label, not free-text structure.
  /// </summary>
  public required string Mode { get; init; }
}
