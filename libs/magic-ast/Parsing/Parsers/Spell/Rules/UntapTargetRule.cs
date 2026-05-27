namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// Untap spell rules — two shapes:
/// <list type="bullet">
///   <item>"Untap target [filter]." — single-target untap spell.</item>
///   <item>"Untap it." — pronoun reference to an already-named target, used as the trailing
///     clause of multi-sentence spells such as "Target creature gets +N/+M and gains
///     &lt;keyword&gt; until end of turn. Untap it."</item>
/// </list>
/// Covers the bare card-type target shapes most common in the corpus:
/// creature and permanent.
/// </summary>
[SpellRule]
public sealed class UntapTargetRule : ISpellRule
{
  private static readonly Regex TargetPattern = new(
    @"^Untap\s+target\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  private static readonly Regex ItPattern = new(
    @"^Untap\s+it$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    // "Untap it." — pronoun reference, emits It() target.
    if (ItPattern.IsMatch(trimmed))
    {
      effect = new UntapEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.It },
      };
      return true;
    }

    // "Untap target <type>." — explicit target filter.
    var m = TargetPattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    effect = new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = [m.Groups["type"].Value.ToLowerInvariant()] },
      },
    };
    return true;
  }
}
