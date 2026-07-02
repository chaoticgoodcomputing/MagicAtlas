namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Linq;
using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "You may tap or untap target [type]." and "tap or untap target [type]."
/// The controller chooses whether to tap or untap the named target.
/// Covers single-type and disjunction-type targets (e.g. "artifact, creature, or land").
/// Rule 701.26 (Tap and Untap).
/// </summary>
[SpellRule]
public sealed class TapOrUntapSpellRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^(?<optional>You\s+may\s+)?[Tt]ap\s+or\s+[Uu]ntap\s+target\s+(?<types>\w+(?:\s*,\s*\w+)*(?:\s*,?\s+or\s+\w+)?)$",
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

    var isOptional = m.Groups["optional"].Success;
    var typesPhrase = m.Groups["types"].Value;

    var types = Regex
      .Split(typesPhrase, @",\s*or\s+|,\s*|\s+or\s+")
      .Select(t => t.Trim().ToLowerInvariant())
      .Select(t => t.EndsWith("s") && t.Length > 1 ? t[..^1] : t)
      .Where(t => t.Length > 0)
      .ToList();

    if (types.Count == 0)
    {
      return false;
    }

    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new TapOrUntapEffect {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = types },
      }}, isOptional);
    return true;
  }
}
