namespace MagicAST.AST.Effects.Modification;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[Subject] is/are [color(s)] in addition to its/their other colors." — a layer-5
/// (CR 613.1e) continuous effect that ADDITIVELY grants one or more colors to an
/// object. Unlike <see cref="ChangeColorEffect"/> (which REPLACES the object's
/// existing colors — CR 105.3's default), this node models the "in addition to its
/// other colors" clause explicitly named by CR 105.3: "If an effect gives an object a
/// new color, the new color replaces all previous colors the object had (unless the
/// effect said the object became that color 'in addition' to its other colors)."
///
/// <para>
/// Layer-5 sibling of <see cref="AddTypeEffect"/> (the layer-4 additive-TYPE grant):
/// both model an "in addition to its other …" clause on a different characteristic
/// axis, existing prior values retained and the new value(s) appended rather than
/// replacing. Reused alongside <see cref="AddTypeEffect"/> — NOT wrapped in a
/// <see cref="MagicAST.AST.Effects.Core.CompositeEffect"/> — when one oracle sentence
/// additively grants both a color and a card type in the same breath ("becomes a blue
/// artifact in addition to its other colors and types"), mirroring the established
/// multi-effect-per-clause convention (<c>AttachedModifyPTAndCardTypeRule</c>: one
/// static ability, two sibling effects sharing a subject).
/// </para>
///
/// <para>
/// Example — Unctus, Grand Metatect: "Until end of turn, target creature you control
/// becomes a blue artifact in addition to its other colors and types." →
/// Target: Target(CardTypes:["creature"], Controller:You), Colors: ["U"],
/// Duration: untilEndOfTurn — paired with an <see cref="AddTypeEffect"/>
/// (AddedCardTypes: ["artifact"]) on the same target and duration.
/// </para>
/// </summary>
[OracleEffect("addColor")]
public sealed record AddColorEffect : ContinuousEffect
{
  public required ObjectReference Target { get; init; }

  /// <summary>Colors additively granted (single-letter codes: W/U/B/R/G).</summary>
  public required IReadOnlyList<string> Colors { get; init; }
}
