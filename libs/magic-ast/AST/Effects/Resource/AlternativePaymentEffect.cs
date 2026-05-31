namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A static ability that lets a player pay part of a spell's cost by an alternative
/// means — Convoke (tap creatures), Improvise (tap artifacts), Delve (exile graveyard
/// cards), Assist (another player pays).
///
/// <para>
/// CR 702.51b (Convoke), 702.66b (Delve), and 702.126b (Improvise) are explicit that
/// these are <em>neither</em> additional <em>nor</em> alternative costs: they are
/// payment <em>substitutions</em> applied while the spell's normal cost is paid. So
/// this is modeled as an <see cref="Effect"/> on the static ability and never routed
/// to a cost slot (ADR 0003 keyword-decomposition round, HITL-resolved shape).
/// </para>
/// </summary>
[OracleEffect("alternativePayment")]
public sealed record AlternativePaymentEffect : Effect
{
  /// <summary>How the substitute payment is made.</summary>
  public required AlternativePaymentMethod Method { get; init; }

  /// <summary>What the substitute payment counts toward in the cost.</summary>
  public required AlternativePaymentKind Pays { get; init; }

  /// <summary>
  /// The objects tapped or exiled to pay (Convoke: creatures you control; Improvise:
  /// artifacts you control; Delve: cards in your graveyard). Null when payment is
  /// delegated to a player rather than drawn from objects (Assist).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Source { get; init; }

  /// <summary>
  /// Convoke (CR 702.51b): a tapped creature may pay for {1} OR for one mana of that
  /// creature's own color. True only when a colored option keyed to the source
  /// object's own color is available; null/false for Improvise/Delve (generic only).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? ColorMustMatchMana { get; init; }
}

/// <summary>How an <see cref="AlternativePaymentEffect"/> substitutes payment.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AlternativePaymentMethod
{
  /// <summary>Tap objects to pay — Convoke (creatures), Improvise (artifacts).</summary>
  TapObject,

  /// <summary>Exile objects to pay — Delve (graveyard cards, {1} each).</summary>
  Exile,

  /// <summary>Delegate payment to another player — Assist.</summary>
  DelegatePayment,
}

/// <summary>What part of the cost an <see cref="AlternativePaymentEffect"/> covers.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AlternativePaymentKind
{
  /// <summary>Pays toward generic mana in the cost ({1} each).</summary>
  Generic,

  /// <summary>Pays toward colored mana in the cost.</summary>
  Colored,
}
