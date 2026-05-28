namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Web-slinging (Rule 702.188). A static ability that functions while the spell
/// is on the stack: "Web-slinging [cost]" means "You may cast this spell by paying
/// [cost] and returning a tapped creature you control to its owner's hand rather
/// than paying its mana cost." MAST records the keyword's presence and the
/// web-slinging cost; the return-a-tapped-creature component of the alt cost is
/// expressed in reminder text and is engine territory per the
/// descriptive-not-engine doctrine.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with other
/// alt-cost keyword effects (Cycling, Evoke, Flashback, Dash). All known
/// printings use a <see cref="ManaCost"/>.
/// </para>
/// </summary>
[OracleEffect("webSlinging")]
public sealed record WebSlingingEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The web-slinging cost paid (along with returning a tapped creature) as the
  /// alternative casting cost. Most commonly a <see cref="ManaCost"/>, but the
  /// polymorphic <see cref="Cost"/> base accommodates future non-mana variants.
  /// </summary>
  public required Cost Cost { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to perform this one. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
