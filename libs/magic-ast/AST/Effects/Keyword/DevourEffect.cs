namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Devour (Rule 702.82). A static ability printed as "Devour N" on creature
/// cards. As this creature enters, you may sacrifice any number of creatures;
/// this creature enters with N +1/+1 counters on it for each creature
/// sacrificed this way. MAST records the keyword and its integer devour value;
/// the sacrifice-on-entry, counter-placement, and optional semantics are
/// engine territory.
///
/// <para>
/// Integer-parameterized keyword effect; follows the <see cref="BushidoEffect"/>
/// shape.
/// </para>
/// </summary>
[OracleEffect("devour")]
public sealed record DevourEffect : Effect
{
  /// <summary>The devour value N printed on the card (e.g., "Devour 2" -> 2).</summary>
  public required int Value { get; init; }
}
