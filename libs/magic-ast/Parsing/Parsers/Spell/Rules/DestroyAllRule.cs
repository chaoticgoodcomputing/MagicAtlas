namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy all [type|subtype]." — mass-destroy spell.
/// Covers type filters (lands, creatures, artifacts, …) and subtype filters
/// (Plains, Islands, Auras, …). Emits a <see cref="DestroyEffect"/> whose
/// <see cref="DestroyEffect.Target"/> has <see cref="ObjectReferenceKind.Each"/>
/// and the appropriate <see cref="ObjectFilter"/> slot populated.
/// </summary>
[SpellRule(Priority = 60)]
public sealed class DestroyAllRule : ISpellRule
{
  /// <summary>
  /// Card types that appear as plural oracle tokens for "Destroy all &lt;type&gt;".
  /// These map directly to the singular lowercase value used in <see cref="ObjectFilter.CardTypes"/>.
  /// </summary>
  private static readonly HashSet<string> CardTypeTokens = new(StringComparer.OrdinalIgnoreCase)
  {
    "lands",
    "creatures",
    "artifacts",
    "enchantments",
    "planeswalkers",
    "permanents",
    "instants",
    "sorceries",
  };

  /// <summary>
  /// Lookup from plural oracle-text subtype tokens to their canonical singular form.
  /// Only subtypes that appear as "Destroy all &lt;subtype&gt;" targets need entries here.
  /// Plain-singular forms (e.g., "Plains") map to themselves; irregular plurals are explicit.
  /// </summary>
  private static readonly Dictionary<string, string> SubtypeSingular =
    new(StringComparer.OrdinalIgnoreCase)
    {
      // Basic land types — canonical singular
      { "Plains", "Plains" },
      { "Islands", "Island" },
      { "Swamps", "Swamp" },
      { "Mountains", "Mountain" },
      { "Forests", "Forest" },
      // Enchantment subtype
      { "Auras", "Aura" },
    };

  private static readonly Regex Pattern = new(
    @"^Destroy\s+all\s+(?<token>[A-Za-z]+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var token = m.Groups["token"].Value;

    if (CardTypeTokens.Contains(token))
    {
      // Singularize by stripping trailing 's': "lands" → "land", "creatures" → "creature".
      // All entries in CardTypeTokens are regular plurals — just strip the final 's'.
      var singularType = token.TrimEnd('s').ToLowerInvariant();
      effect = new DestroyEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Each,
          Filter = new ObjectFilter { CardTypes = [singularType] },
        },
      };
      return true;
    }

    if (SubtypeSingular.TryGetValue(token, out var singular))
    {
      effect = new DestroyEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Each,
          Filter = new ObjectFilter { Subtypes = [singular] },
        },
      };
      return true;
    }

    return false;
  }
}
