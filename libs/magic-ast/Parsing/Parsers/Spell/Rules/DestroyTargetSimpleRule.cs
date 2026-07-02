namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target [filter]." — single-target destroy spell.
/// Covers bare card-type targets (creature, artifact, land, …) and richer filter
/// phrases via <see cref="SpellRuleHelpers.ParseTargetFilter"/>:
/// <list type="bullet">
///   <item>Subtype-only: "Spirit", "Human"</item>
///   <item>Color + card type: "black creature", "white creature"</item>
///   <item>non- prefix + card type: "nonbasic land"</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class DestroyTargetSimpleRule : ISpellRule
{
  // Simple bare card-type targets (fast path — preserves existing behaviour).
  private static readonly Regex SimplePattern = new(
    @"^Destroy\s+target\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Richer filter: one or more words after "Destroy target"
  // (covers color+type, subtype-only, non-prefix patterns).
  private static readonly Regex FilterPattern = new(
    @"^Destroy\s+target\s+(?<filter>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // Fast path: bare card-type target (Destroy target creature, etc.)
    var simple = SimplePattern.Match(text);
    if (simple.Success)
    {
      effect = new DestroyEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = [simple.Groups["type"].Value.ToLowerInvariant()] },
        },
      };
      return true;
    }

    // Richer filter path (subtype, color+type, non-prefix).
    var rich = FilterPattern.Match(text);
    if (!rich.Success)
    {
      return false;
    }

    var filterPhrase = rich.Groups["filter"].Value.Trim();
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
