namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Scavenge (Rule 702.97). An activated ability functioning only from the graveyard:
/// "{Cost}, Exile this card from your graveyard: Put a number of +1/+1 counters equal
/// to this card's power on target creature. Scavenge only as a sorcery."
/// MAST records the keyword's presence and the scavenge cost; the counter-placement
/// semantics are conventionally inferred from the rules.
/// </summary>
[OracleEffect("scavenge")]
public sealed record ScavengeEffect : Effect
{
  /// <summary>
  /// The cost paid to scavenge this card. Always a <see cref="ManaCost"/> in printed
  /// oracle text (e.g. "{6}{B}", "{5}{G}", "{0}").
  /// </summary>
  public required Cost Cost { get; init; }
}
