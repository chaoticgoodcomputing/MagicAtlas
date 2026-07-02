namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "incubate N" keyword action (CR 701.53).
///
/// <para>
/// CR 701.53a: "To incubate N, create an Incubator token that enters the
/// battlefield with N +1/+1 counters on it. See rule 111.10i."
/// </para>
///
/// <para>
/// CR 701.53b: "An Incubator token is a double-faced token. Its front face is a
/// colorless Incubator artifact with \"{2}: Transform this token.\" Its back face
/// is a 0/0 colorless Phyrexian artifact creature named \"Phyrexian Token.\""
/// </para>
///
/// <para>
/// MAST records the keyword-action with its integer value N. The Incubator token's
/// double-faced nature, +1/+1 counters, the "{2}: Transform this token." front-face
/// activated ability, and the 0/0 Phyrexian artifact-creature back face are all
/// engine territory (reminder text, CR 701.53b) — the node names the action, not
/// the execution. Same discipline as <see cref="AmassEffect"/> and
/// InvestigateEffect.
/// </para>
/// </summary>
[OracleEffect("incubate")]
public sealed record IncubateEffect : Effect
{
  /// <summary>
  /// The N value: number of +1/+1 counters the Incubator token enters with (CR 701.53a).
  /// </summary>
  public required Quantity Count { get; init; }
}
