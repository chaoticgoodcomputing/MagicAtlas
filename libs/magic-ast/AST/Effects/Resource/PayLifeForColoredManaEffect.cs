namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A static ability that permits the controller to substitute life payment for
/// colored mana symbols in any cost — the K'rrik, Son of Yawgmoth mechanic:
/// "For each {B} in a cost, you may pay 2 life rather than pay that mana."
///
/// <para>
/// CR 107.4f defines the Phyrexian mana symbol ({B/P} etc.) as a cost that can be
/// paid with the named color OR 2 life. K'rrik extends this substitution to ALL
/// instances of the named color in any cost the controller pays — not just Phyrexian
/// mana symbols on the card itself.
/// </para>
///
/// <para>
/// This is a static continuous permission (CR 604): it is always active while K'rrik
/// is on the battlefield. The "you may" in oracle text means the substitution is
/// optional on a per-symbol basis; paying in mana is always legal. The mechanic is
/// descriptive here — which symbols qualify, what the substitution rate is — and the
/// rules engine applies the permission at cost-payment time (CR 601.2f).
/// </para>
///
/// <para>
/// Distinct from <see cref="AlternativePaymentEffect"/> (Convoke/Improvise/Delve —
/// tap or exile objects to pay), <see cref="GrantAlternativeCostEffect"/> (replace the
/// entire mana cost of a specific spell), and <see cref="PayLifeEffect"/> (Shockland
/// pattern — one-time optional life payment on ETB). This node captures a per-symbol,
/// all-costs, color-targeted life-substitution permission.
/// </para>
/// </summary>
[OracleEffect(
  "payLifeForColoredMana",
  NearDuplicateOf = new[] { "payLife" },
  Reason = "Distinct: 'payLife' is a life-payment effect/cost; 'payLifeForColoredMana' (K'rrik) is a mana-substitution permission — pay N life rather than a colored mana symbol in a cost (CR 107.4f analogue). Different concepts; prefix overlap only."
)]
public sealed record PayLifeForColoredManaEffect : Effect
{
  /// <summary>
  /// The mana colors whose symbols may be substituted with life payment.
  /// For K'rrik: <c>["B"]</c> (one black mana symbol may be paid with 2 life).
  /// Uses the same single-letter color codes as <see cref="MagicAST.AST.Costs.ManaColor"/>
  /// serialization: <c>"W"</c>, <c>"U"</c>, <c>"B"</c>, <c>"R"</c>, <c>"G"</c>.
  /// </summary>
  public required IReadOnlyList<string> Colors { get; init; }

  /// <summary>
  /// How much life may be paid per matching mana symbol. For K'rrik: <c>2</c>.
  /// CR 107.4f analogy: each Phyrexian symbol may be paid with 2 life; K'rrik
  /// extends this to all symbols of the stated color in any cost.
  /// </summary>
  public required int LifePerMana { get; init; }
}
