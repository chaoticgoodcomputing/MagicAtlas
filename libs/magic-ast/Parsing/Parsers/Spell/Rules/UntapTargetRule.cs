namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Untap target [filter]." — single-target untap spell.
/// Covers the bare card-type target shapes most common in the corpus:
/// creature and permanent.
/// </summary>
[SpellRule]
public sealed class UntapTargetRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Untap\s+target\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
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
