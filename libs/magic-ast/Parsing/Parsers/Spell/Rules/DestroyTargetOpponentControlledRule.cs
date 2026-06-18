namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target [type] you don't control." — single-target destroy of a
/// permanent controlled by an opponent.
///
/// <para>
/// Rule 701.8 (destroy) + CR 109.5 ("you don't control" = opponent-controlled).
/// The "you don't control" qualifier maps to <see cref="ControllerFilter.Opponent"/>
/// on the <see cref="ObjectFilter"/>. Examples: Vandalblast ("Destroy target artifact
/// you don't control.").
/// </para>
///
/// <para>
/// This rule is anchored so it cannot shadow the broader
/// <see cref="DestroyTargetSimpleRule"/> patterns (which require no controller
/// qualifier). Any card type accepted by <c>DestroyCardTypeMap</c> is supported.
/// </para>
/// </summary>
[SpellRule]
public sealed class DestroyTargetOpponentControlledRule : ISpellRule
{
  /// <summary>
  /// Card types that can appear as "Destroy target [type] you don't control."
  /// Maps oracle-text tokens (singular and plural) to their canonical lowercase singular.
  /// </summary>
  private static readonly Dictionary<string, string> CardTypeMap =
    new(StringComparer.OrdinalIgnoreCase)
    {
      { "creature", "creature" },
      { "creatures", "creature" },
      { "artifact", "artifact" },
      { "artifacts", "artifact" },
      { "enchantment", "enchantment" },
      { "enchantments", "enchantment" },
      { "land", "land" },
      { "lands", "land" },
      { "planeswalker", "planeswalker" },
      { "planeswalkers", "planeswalker" },
      { "permanent", "permanent" },
      { "permanents", "permanent" },
    };

  // Fully anchored: "Destroy target <type> you don't control"
  // Does NOT match "Destroy target <type> you control" (handled by DestroyTargetSimpleRule
  // via ParseTargetFilter or the controller-qualified variant).
  private static readonly Regex Pattern = new(
    @"^Destroy\s+target\s+(?<type>[A-Za-z]+)\s+you\s+don't\s+control$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <inheritdoc/>
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var raw = text.Trim().TrimEnd('.');
    var m = Pattern.Match(raw);
    if (!m.Success)
    {
      return false;
    }

    var typeToken = m.Groups["type"].Value;
    if (!CardTypeMap.TryGetValue(typeToken, out var cardType))
    {
      // Unknown type token — fall through to avoid silently dropping the text.
      return false;
    }

    effect = new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = [cardType],
          Controller = ControllerFilter.Opponent,
        },
      },
    };
    return true;
  }
}
