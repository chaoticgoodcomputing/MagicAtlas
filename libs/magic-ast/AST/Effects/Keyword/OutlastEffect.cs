namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Outlast {cost} (Rule 702.107). A keyword ability that allows a creature
/// to grow larger over time. Oracle form: "Outlast {cost} ({cost}, {T}: Put
/// a +1/+1 counter on this creature. Outlast only as a sorcery.)".
///
/// <para>
/// MAST records the keyword's presence and its mana cost parameter; the
/// activated-ability structure ({cost}, tap, sorcery-speed restriction) and
/// the counter-placement are engine territory.
/// </para>
///
/// <para>
/// Mana-cost-parameterized keyword; mirrors
/// <see cref="MadnessEffect"/>, and the Kicker/Echo/Bestow family.
/// <see cref="Cost"/> is typed as the polymorphic <see cref="Cost"/> base to
/// mirror the existing mana-cost keyword pattern.
/// </para>
/// </summary>
[OracleEffect("outlast")]
public sealed record OutlastEffect : Effect
{
  /// <summary>The mana cost paid to activate Outlast (e.g., "Outlast {W}" → {W}).</summary>
  public required Cost Cost { get; init; }
}
