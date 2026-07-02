namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Tap up to N target creatures." — bounded-count tap-targets spell effect.
/// Examples: "Tap up to two target creatures." / "Tap up to three target creatures."
/// The quantity lives on <see cref="ObjectReference.Quantity"/> (cardinality of the
/// target set), not on <see cref="TapEffect.Count"/>, because the oracle phrase is
/// "up to N target", not "tap N targets". Rule 115.1 / Rule 701.26.
/// </summary>
[SpellRule]
public sealed class TapUpToNTargetsRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Tap\s+up\s+to\s+(?<n>\w+)\s+target\s+(?<types>\w+(?:\s*,\s*\w+)*(?:\s*,?\s+or\s+\w+)?)$",
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

    if (!SpellRuleHelpers.TryParseSmallWord(m.Groups["n"].Value, out var maximum))
    {
      return false;
    }

    var typesPhrase = m.Groups["types"].Value;
    var types = Regex
      .Split(typesPhrase, @"\s*,\s*|\s+or\s+")
      .Select(t => t.Trim().ToLowerInvariant())
      .Select(t => t.EndsWith("s") && t.Length > 1 ? t[..^1] : t)
      .Where(t => t.Length > 0)
      .ToList();

    if (types.Count == 0)
    {
      return false;
    }

    effect = new TapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = types },
        Quantity = new UpToQuantity { Maximum = maximum, Minimum = 0 },
      },
    };
    return true;
  }
}
