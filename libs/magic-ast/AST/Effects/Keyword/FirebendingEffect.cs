namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Firebending N (Avatar: The Last Airbender). A triggered keyword ability
/// printed as "Firebending N (Whenever this creature attacks, add N {R}.
/// This mana lasts until end of combat.)". MAST records the keyword and its
/// integer value; the attack trigger, mana-addition, and end-of-combat duration
/// are engine territory per the descriptive-not-engine doctrine.
///
/// <para>
/// Integer-parameterized keyword; mirrors <see cref="BushidoEffect"/> and
/// <see cref="MobilizeEffect"/> in shape. The <see cref="Value"/> is the
/// printed integer N (the number of {R} mana added when attacking).
/// Variable-value printings ("Firebending X, where X is ...") are out of scope
/// for this batch.
/// </para>
/// </summary>
[OracleEffect("firebending")]
public sealed record FirebendingEffect : Effect
{
  /// <summary>
  /// The amount of {R} mana added whenever this creature attacks
  /// (N in "Firebending N").
  /// </summary>
  public required int Value { get; init; }
}
