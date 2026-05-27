namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// "destroy target [filter]." / "you may destroy target [filter]." —
/// single-target destroy on the triggered side.
///
/// Delegates single-noun filter parsing to <see cref="SpellRuleHelpers.ParseTargetFilter"/>
/// (same lexical surface as <see cref="Spell.Rules.DestroyTargetSimpleRule"/>).
/// Handles type-disjunction filters ("artifact or enchantment",
/// "creature, artifact, or enchantment") via
/// <see cref="SpellRuleHelpers.SplitTypeDisjunction"/>, matching the
/// spell-side <see cref="Spell.Rules.DestroyTargetTypeDisjunctionRule"/>.
///
/// Covers: land, artifact, enchantment, creature, permanent, and richer filter
/// phrases (color + type, non- prefix, etc.), as well as any two-or-more-type
/// disjunction.  The optional "you may" prefix sets
/// <see cref="DestroyEffect.IsOptional"/> = <see langword="true"/>.
/// </summary>
[TriggeredRule]
public sealed class DestroyTargetTriggeredRule : ITriggeredRule
{
  // Optional "you may " prefix, followed by "destroy target <filter>".
  private static readonly Regex Pattern = new(
    @"^(?:you\s+may\s+)?destroy\s+target\s+(?<filter>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Disjunction forms: "[type1] or [type2]" / "[type1], [type2], or [type3]".
  // Uses a word-boundary check so plain single-word filters (e.g. "creature")
  // don't match this branch.
  private static readonly Regex DisjunctionPattern = new(
    @"^[a-z]+(?:\s*,\s*[a-z]+)*\s+or\s+[a-z]+$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var isOptional = trimmed.StartsWith("you may", StringComparison.OrdinalIgnoreCase);
    var filterPhrase = m.Groups["filter"].Value.Trim();

    // Try disjunction first ("artifact or enchantment", "creature or planeswalker", etc.)
    if (DisjunctionPattern.IsMatch(filterPhrase))
    {
      var cardTypes = SpellRuleHelpers.SplitTypeDisjunction(filterPhrase);
      if (cardTypes.Count >= 2)
      {
        effect = new DestroyEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter { CardTypes = cardTypes },
          },
          IsOptional = isOptional,
        };
        return true;
      }
    }

    // Fall back to single-noun filter ("creature", "white creature", "nonbasic land", etc.)
    var filter = SpellRuleHelpers.ParseTargetFilter(filterPhrase);
    if (filter is null)
    {
      return false;
    }

    effect = new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = filter,
      },
      IsOptional = isOptional,
    };
    return true;
  }
}
