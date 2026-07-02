namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target [type1] or [type2]." / "Destroy target [type1], [type2], or [type3]."
/// Multi-element <see cref="ObjectFilter.CardTypes"/> as the disjunction.
///
/// Also handles a keyword qualifier on the last element:
/// "Destroy target [type1], [type2], or [typeN] with [keyword]."
/// e.g., "Destroy target artifact, enchantment, or creature with flying."
/// The qualifier is stored in <see cref="ObjectFilter.Characteristics"/>.
/// </summary>
[SpellRule]
public sealed class DestroyTargetTypeDisjunctionRule : ISpellRule
{
  // Bare disjunction: "artifact or enchantment" / "artifact, enchantment, or creature"
  // The Oxford comma (", or") is handled by ,?\s+ before "or".
  private static readonly Regex BarePattern = new(
    @"^Destroy\s+target\s+(?<types>[a-z]+(?:\s*,\s*[a-z]+)*,?\s+or\s+[a-z]+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Disjunction where the last element carries a "with <keyword>" qualifier:
  // "artifact, enchantment, or creature with flying"
  // The Oxford comma (", or") is handled by ,?\s+ before "or".
  private static readonly Regex WithKeywordPattern = new(
    @"^Destroy\s+target\s+(?<types>[a-z]+(?:\s*,\s*[a-z]+)*,?\s+or\s+[a-z]+)\s+with\s+(?<keyword>[a-z]+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var raw = text.Trim().TrimEnd('.');

    // Try the keyword-qualified form first (more specific).
    var mk = WithKeywordPattern.Match(raw);
    if (mk.Success)
    {
      var cardTypes = SpellRuleHelpers.SplitTypeDisjunction(mk.Groups["types"].Value);
      if (cardTypes.Count < 2)
      {
        return false;
      }

      var keyword = mk.Groups["keyword"].Value.ToLowerInvariant();
      effect = new DestroyEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = cardTypes,
            Characteristics = [Characteristic.FromLabel(keyword)],
          },
        },
      };
      return true;
    }

    // Bare disjunction (no qualifier).
    var m = BarePattern.Match(raw);
    if (!m.Success)
    {
      return false;
    }

    var types = SpellRuleHelpers.SplitTypeDisjunction(m.Groups["types"].Value);
    if (types.Count < 2)
    {
      return false;
    }

    effect = new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = types },
      },
    };
    return true;
  }
}
