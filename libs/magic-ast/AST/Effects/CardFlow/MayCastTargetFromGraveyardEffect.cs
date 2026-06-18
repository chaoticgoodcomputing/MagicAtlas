namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may cast [target] from your graveyard [paying its mana cost]." — a
/// one-shot permission granted by an activated ability that allows the controller
/// to cast a targeted card from their graveyard, paying its normal mana cost.
///
/// <para>
/// Distinct from <see cref="MayPlayFromGraveyardEffect"/> (a continuous static
/// permission for lands) and <see cref="MagicAST.AST.Effects.Timing.CastWithoutPayingEffect"/>
/// (which waives the mana cost). This effect is a one-shot permission — it grants
/// the option to cast the targeted card at its printed mana cost from the graveyard.
/// </para>
///
/// <para>
/// CR 601.3e: "Some rules and effects state that an alternative set of characteristics
/// or a subset of characteristics are considered to determine if a card or copy of a
/// card is legal to cast…" Here the permission grants a zone exception — graveyard
/// rather than hand — without altering the cost. MAST describes the permission;
/// the engine handles the legality check, zone-change, and stack placement (ADR
/// 0003/0004 describe-not-execute).
/// </para>
/// </summary>
[OracleEffect("mayCastTargetFromGraveyard")]
public sealed record MayCastTargetFromGraveyardEffect : Effect
{
  /// <summary>
  /// The card the controller may cast from their graveyard — typically a
  /// <see cref="ObjectReferenceKind.Target"/> or <see cref="ObjectReferenceKind.It"/>
  /// reference back-linking to the chosen target.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }

  /// <summary>
  /// Filter narrowing which cards this permission applies to. When the effect
  /// contains an inline targeting declaration ("Choose target [filter]"), the
  /// filter captures that targeting restriction so consumers can determine the
  /// legal targets for the ability.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Filter { get; init; }
}
