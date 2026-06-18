namespace MagicAST.AST.Effects.Resource;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Creatures you control can boast [N] times during each of your turns rather than once."
///
/// <para>
/// A static continuous effect (CR 611.3) that modifies the boast-ability activation
/// limit (CR 702.142a: "Activate only if this creature attacked this turn and only
/// once each turn.") for permanents matching <see cref="Target"/> —
/// Birgi grants controlled creatures the ability to boast <see cref="NewLimit"/>
/// times per turn instead of the default once.
/// </para>
///
/// <para>
/// CR 702.142a (verbatim): "A boast ability is a special kind of activated ability.
/// 'Boast - [Cost]: [Effect]' means '[Cost]: [Effect]. Activate only if this
/// creature attacked this turn and only once each turn.'"
/// </para>
///
/// <para>
/// This effect overrides the "once each turn" restriction for the controller's
/// creatures — not granting a boast ability itself, but relaxing the frequency
/// restriction on any boast abilities those creatures already have.
/// </para>
/// </summary>
[OracleEffect("modifyBoastActivationLimit")]
public sealed record ModifyBoastActivationLimitEffect : ContinuousEffect
{
  /// <summary>
  /// The permanents whose boast activation limit is modified.
  /// Typically "creatures you control" (controller=You, cardTypes=["creature"]).
  /// </summary>
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// The new activation limit per turn (replaces the default once-per-turn
  /// constraint from CR 702.142a). Birgi sets this to 2 ("twice").
  /// </summary>
  public required int NewLimit { get; init; }
}
