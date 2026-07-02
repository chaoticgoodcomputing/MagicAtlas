namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target [filter]." — single-target exile spell.
/// Covers bare card-type targets (creature, artifact, enchantment, permanent, …)
/// and richer filter phrases via <see cref="SpellRuleHelpers.ParseTargetFilter"/>:
/// <list type="bullet">
///   <item>Subtype-only: "Spirit", "Human"</item>
///   <item>Color + card type: "black creature", "white artifact"</item>
///   <item>non- prefix + card type: "nonbasic land"</item>
/// </list>
/// Coexistence with <see cref="ExileTargetLandRule"/>: both rules dispatch at
/// priority 50; alphabetical ordering puts ExileTargetLandRule ahead of
/// ExileTargetSimpleRule, so the bare "land" shape is still claimed by the
/// specialised rule. This rule handles all other card-type filters.
/// Rule 701.13 (exile action) + Rule 205.3 (card types).
/// </summary>
[SpellRule]
public sealed class ExileTargetSimpleRule : ISpellRule
{
  // Simple bare card-type targets (fast path).
  private static readonly Regex SimplePattern = new(
    @"^Exile\s+target\s+(?<type>creature|artifact|enchantment|planeswalker|permanent|instant|sorcery)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Richer filter: one or more words after "Exile target"
  // (covers color+type, subtype-only, non-prefix patterns).
  private static readonly Regex FilterPattern = new(
    @"^Exile\s+target\s+(?<filter>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // Fast path: bare card-type target (Exile target creature, etc.)
    var simple = SimplePattern.Match(text);
    if (simple.Success)
    {
      effect = new ExileEffect
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

    effect = new ExileEffect
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
