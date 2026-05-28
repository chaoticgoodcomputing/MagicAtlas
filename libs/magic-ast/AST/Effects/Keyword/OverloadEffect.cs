namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Overload (Rule 702.96). A spell modifier printed as "Overload [cost]" on
/// instants and sorceries. When cast for the overload cost the spell's target
/// references change to "each" references (per reminder text); MAST records
/// only the keyword's presence and the alternative cost — the target-to-each
/// rewrite is engine territory, not a descriptive axis of the card.
/// </summary>
[OracleEffect("overload")]
public sealed record OverloadEffect : Effect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The alternative cost that may be paid to cast this spell with overload.
  /// </summary>
  public required Cost Cost { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
