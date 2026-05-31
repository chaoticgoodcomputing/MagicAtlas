namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "you may pay [cost]. If you do, [effect]" — the conditional-pay triggered
/// pattern. The controller may pay the stated cost; the
/// <see cref="IOptionalEffect.IfYouDo"/> branch carries the consequent effect
/// when payment is made.
///
/// <para>Rule 117.3: A player may pay a cost at the time the game asks them
/// to. Rule 603.1: Triggered abilities trigger when their trigger event occurs.
/// The oracle sentence "you may pay [cost]. If you do, [effect]" describes
/// a triggered-ability resolution step in which paying the cost is optional and
/// gated: the consequent fires only if the cost was paid.</para>
///
/// <para>This effect is distinct from <c>UntapEffect { IsOptional = true }</c>
/// (the Mana Vault shape) in that the payment cost is a first-class field here
/// rather than relegated to the ability's Instructions list. Cards like
/// Deathgreeter ("Whenever a creature dies, you may pay {1}. If you do, you
/// gain 1 life.") have the payment as the descriptive axis — the cost is what
/// the oracle text names, not a gate to an effect that already has its own
/// type. The descriptive-not-engine doctrine (MAST records what oracle text
/// says) requires the cost to appear in the AST.</para>
///
/// <para>The <c>IsOptional</c> flag is always <c>true</c> for this effect:
/// "you may pay" is the canonical form; there is no forced-pay variant at the
/// triggered-ability level modelled by this type.</para>
/// </summary>
[OracleEffect("conditionalPay")]
public sealed record ConditionalPayEffect : Effect
{
  /// <summary>
  /// The cost the controller may choose to pay. Typically a
  /// <see cref="ManaCost"/> but the polymorphic <see cref="Cost"/> base
  /// accommodates life costs, discard costs, etc.
  /// </summary>
  public required Cost Cost { get; init; }
}
