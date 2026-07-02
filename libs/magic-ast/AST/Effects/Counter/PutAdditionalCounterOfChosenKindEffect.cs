namespace MagicAST.AST.Effects.Counter;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Choose a counter on [target permanent]. Put an additional counter of that kind on that permanent."
/// — Ichormoon Gauntlet's triggered effect.
///
/// <para>
/// The player chooses one counter type already present on the target permanent, then places one
/// additional counter of that same kind on it. The counter kind is a runtime choice, not statically
/// fixed in the oracle text, so this node captures the shape (choose + duplicate one of that kind)
/// rather than a fixed <see cref="PutCountersEffect"/> with a known counter type.
/// </para>
///
/// <para>
/// CR 122.1 (counters): "A counter is a marker placed on an object or player that modifies its
/// characteristics and/or interacts with a rule or effect." The kind chosen must already be on
/// the target — the player cannot choose a counter kind absent from the permanent.
/// </para>
/// </summary>
[OracleEffect("putAdditionalCounterOfChosenKind")]
public sealed record PutAdditionalCounterOfChosenKindEffect : Effect
{
  /// <summary>
  /// The permanent on which a counter is chosen and an additional counter is placed.
  /// In Ichormoon Gauntlet's trigger this is a <see cref="ObjectReferenceKind.Target"/> permanent.
  /// </summary>
  public required ObjectReference Target { get; init; }
}
