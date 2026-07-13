namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Untap up to N target creatures." — bounded-count untap-targets spell effect,
/// the untap twin of <see cref="TapUpToNTargetsRule"/>. Examples: "Untap up to two
/// target creatures." (Join Forces, first sentence). The quantity lives on
/// <see cref="ObjectReference.Quantity"/> (an <see cref="UpToQuantity"/> — cardinality
/// of the target set), not on <see cref="UntapEffect.Count"/>, because the oracle
/// phrase is "up to N target", not "untap N targets" (mirrors the tap sibling).
///
/// <para>
/// CR 701.26b: "To untap a permanent, rotate it back to the upright position from a
/// sideways position." CR 115.1 ("target") + CR 107.3 ("up to N") make this a 0–N
/// targeted choice. Anchored (<c>^…$</c>): distinct from the "permanents" activated
/// shape handled by <see cref="MagicAST.Parsing.Parsers.Activated.Rules.UntapUpToNTargetPermanentsRule"/>.
/// </para>
/// </summary>
[SpellRule]
public sealed class UntapUpToNTargetsRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Untap\s+up\s+to\s+(?<n>\w+)\s+target\s+(?<types>\w+(?:\s*,\s*\w+)*(?:\s*,?\s+or\s+\w+)?)$",
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

    var types = Regex
      .Split(m.Groups["types"].Value, @"\s*,\s*|\s+or\s+")
      .Select(t => t.Trim().ToLowerInvariant())
      .Select(t => t.EndsWith("s") && t.Length > 1 ? t[..^1] : t)
      .Where(t => t.Length > 0)
      .ToList();

    if (types.Count == 0)
    {
      return false;
    }

    effect = new UntapEffect
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
