namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Surge (Rule 702.114). "You may cast this spell for its surge cost if you or
/// a teammate has cast another spell this turn." MAST records the keyword's
/// presence and the surge cost; the condition on who has cast another spell is
/// conventionally inferred from the rules and belongs in the reminder text.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with other
/// cost-bearing keyword effects (Kicker, Cycling, Bestow). Surge always uses
/// a <see cref="ManaCost"/> in printed Oracle text, but the base accommodates
/// potential future variants.
/// </para>
/// </summary>
[OracleEffect("surge")]
public sealed record SurgeEffect : Effect
{
  /// <summary>
  /// The surge cost paid as an alternative cost when casting this spell.
  /// </summary>
  public required Cost Cost { get; init; }
}
