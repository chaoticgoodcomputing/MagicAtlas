namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// "destroy target [filter]." — single-target destroy on the triggered side.
/// Delegates filter parsing to <see cref="SpellRuleHelpers.ParseTargetFilter"/>
/// (same lexical surface as <see cref="Spell.Rules.DestroyTargetSimpleRule"/>).
/// Covers: land, artifact, enchantment, creature, permanent, and richer filter
/// phrases (color + type, non- prefix, etc.).
/// </summary>
[TriggeredRule]
public sealed class DestroyTargetTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^destroy\s+target\s+(?<filter>.+)$",
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

    var filterPhrase = m.Groups["filter"].Value.Trim();
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
    };
    return true;
  }
}
