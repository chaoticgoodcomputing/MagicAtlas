namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Replacement event for when a player would copy a spell one or more times —
/// "If you would copy a spell one or more times, instead …"
///
/// <para>
/// CR 707.10: "To copy a spell, activated ability, or triggered ability means to put a
/// copy of it onto the stack; a copy of a spell isn't cast…" Twinning Staff's static
/// ability is a replacement effect that intercepts the act of copying a spell and adds
/// one additional copy on top of however many copies would otherwise be made.
/// The <see cref="MinimumQuantity"/> encodes the "one or more" threshold.
/// </para>
/// </summary>
[OracleReplacementEvent("spellCopy")]
public sealed record SpellCopyEvent : ReplacementEvent
{
  /// <summary>
  /// Minimum number of copies that would be made for the replacement to apply
  /// (e.g. "one or more" = 1). Null means any number.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MinimumQuantity { get; init; }
}
