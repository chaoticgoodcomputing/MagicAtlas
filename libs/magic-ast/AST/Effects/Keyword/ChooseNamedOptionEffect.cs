namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "choose [Name] or [Name]." — the named-mode-choice declaration of the
/// Believe/Doubt family (Phenomenon Investigators: "As this creature enters,
/// choose Believe or Doubt."). The oracle line records that the controller picks
/// exactly one of a fixed set of NAMED options; the two ability lines that follow
/// (each prefixed with an option name) function only if their option was the one
/// chosen — those downstream abilities carry a
/// <see cref="MagicAST.AST.Abilities.ChosenModeCondition"/> gate naming the same
/// option. MAST models only the choice declaration itself, not the producer/consumer
/// link (ADR 0004, reference-not-resolution).
///
/// <para>Timing is a separate axis: this choice happens as the permanent enters
/// ("As this creature enters, choose …" — CR 614.12), so the enclosing
/// <see cref="MagicAST.AST.Abilities.StaticAbility"/> carries
/// <see cref="MagicAST.AST.Abilities.StaticTimingKind.AsThisEnters"/>; the effect
/// itself stays plain. Timing and effect are composable, never baked into the
/// effect discriminator.</para>
///
/// <para>Sibling of <see cref="ChoosePlayerEffect"/> / <see cref="ChooseColorEffect"/>
/// / <see cref="ChooseCreatureTypeEffect"/> / <see cref="ChooseCardTypeEffect"/> /
/// <see cref="ChooseBasicLandTypeEffect"/>: those record a choice from an OPEN
/// characteristic domain (a color, a creature type, a player); this records a choice
/// among a CLOSED set of card-specific NAMED modes printed on the card. The chosen
/// value is the mode name, so the alternatives are carried on <see cref="Options"/>
/// rather than left implicit. CR 700.2 (modal): "A spell or ability is modal if it
/// has two or more options … preceded by 'Choose one —' …"; the Believe/Doubt shape
/// makes the choice as the object enters (CR 614.12) and gates ongoing abilities on
/// the result rather than resolving one option immediately.</para>
/// </summary>
[OracleEffect("chooseNamedOption")]
public sealed record ChooseNamedOptionEffect : Effect
{
  /// <summary>
  /// The named options the controller chooses between, in printed order —
  /// e.g. <c>["Believe", "Doubt"]</c>. Each name links to the
  /// <see cref="MagicAST.AST.Abilities.ChosenModeCondition.Mode"/> of the ability
  /// gated on that option. Verbatim card-specific labels (like mode names on
  /// <see cref="MagicAST.AST.Abilities.ModalOption.Name"/>), not free-text structure.
  /// </summary>
  public required IReadOnlyList<string> Options { get; init; }
}
