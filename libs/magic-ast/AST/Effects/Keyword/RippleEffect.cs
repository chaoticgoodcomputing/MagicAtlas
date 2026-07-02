namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Ripple (Rule 702.60). A triggered ability printed as "Ripple N" on
/// instant and sorcery cards, functioning only while the card with ripple
/// is on the stack. When you cast this spell, you may reveal the top N
/// cards of your library (or all of them, if fewer than N remain); you may
/// cast any revealed cards with the same name as this spell without paying
/// their mana costs, then put the rest on the bottom of your library in any
/// order. MAST records the keyword and its integer ripple value; the
/// reveal, free-cast, and bottoming semantics are engine territory.
///
/// <para>
/// Integer-parameterized keyword effect; follows the <see cref="BushidoEffect"/>
/// shape.
/// </para>
/// </summary>
[OracleEffect("ripple")]
public sealed record RippleEffect : Effect
{
  /// <summary>The ripple value N printed on the card (e.g., "Ripple 4" -> 4).</summary>
  public required int Value { get; init; }
}
