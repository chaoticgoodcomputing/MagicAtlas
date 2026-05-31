namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Pay N life." — an optional-life-payment declaration. The oracle line
/// describes a single payment the controller MAY make; the
/// <see cref="IOptionalEffect.IfYouDoNot"/> branch carries the fallback effect
/// when payment is declined.
///
/// <para>Timing is a separate axis: in the Shockland pattern ("As this land
/// enters, you may pay N life. If you don't, it enters tapped." — CR 614.1c) the
/// enclosing <see cref="MagicAST.AST.Abilities.StaticAbility"/> carries
/// <see cref="MagicAST.AST.Abilities.StaticTimingKind.AsThisEnters"/> and the
/// declined branch holds a plain <c>TapEffect</c> targeting self; the payment
/// effect itself stays plain. Modelling the verb directly keeps the AST
/// descriptively faithful (MAST records what oracle text says, not what the rules
/// engine derives at run-time).</para>
///
/// <para>Distinct from the enters-tapped + condition shape used for
/// checklands/fastlands: those gate entry on a board-state predicate ("unless you
/// control two or fewer lands") via <see cref="MagicAST.AST.Abilities.StaticAbility.Condition"/>;
/// painlands/shocklands carry a life-payment cost instead. CR 614.1c covers both,
/// but the descriptive shapes differ.</para>
/// </summary>
[OracleEffect("payLife")]
public sealed record PayLifeEffect : Effect
{
  /// <summary>
  /// The amount of life the controller may pay. Typically a
  /// <see cref="LiteralQuantity"/> (every printed painland/shockland uses a
  /// fixed integer); the polymorphic <see cref="Quantity"/> base accommodates
  /// future variants (e.g. an X-cost printing).
  /// </summary>
  public required Quantity Amount { get; init; }
}
