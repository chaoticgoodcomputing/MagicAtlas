namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target land." — Caustic Rain and similar sorceries that exile a single
/// land permanent with no color, subtype, or disjunction qualifier. The bare
/// "target land" shape is not covered by <see cref="ExileTypeDisjunctionRule"/>
/// (which requires two or more types joined by "or") or the color/monocolored
/// variants, so this rule handles the simplest case.
/// Rule 701.13 (exile action) + Rule 205.3j (land as card type).
/// </summary>
[SpellRule]
public sealed class ExileTargetLandRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Exile\s+target\s+land$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["land"] },
      },
    };
    return true;
  }
}
