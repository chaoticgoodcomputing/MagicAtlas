namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Backup (Rule 702.165). A triggered keyword ability printed as "Backup N".
/// When this creature enters, put N +1/+1 counters on target creature. If that
/// is another creature, it also gains the non-backup abilities of this creature
/// printed below the backup ability until end of turn.
/// MAST records the keyword and its integer value; the counter placement,
/// ability-grant, and "printed below this one" scoping are engine territory
/// per the descriptive-not-engine doctrine.
///
/// <para>
/// Integer-parameterized keyword; mirrors the BushidoEffect
/// shape — <see cref="Value"/> is the backup number lifted from the printed
/// oracle text.
/// </para>
/// </summary>
[OracleEffect("backup")]
public sealed record BackupEffect : Effect
{
  /// <summary>The backup value N printed on the card (e.g., "Backup 2" → 2).</summary>
  public required int Value { get; init; }
}
