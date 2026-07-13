namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target [type] with mana value N or less/greater/more/fewer." — a
/// mana-value-gated single-target exile spell (Despark: "Exile target permanent
/// with mana value 4 or greater."). The threshold lands on
/// <see cref="ObjectFilter.ManaValueComparison"/> (Rule 202.3 defines mana value);
/// the exile action itself is Rule 701.13a. Fully anchored (^…$) so the mandatory
/// mana-value qualifier cannot let this pattern substring-match a more specific
/// sibling; the type group is restricted to the small closed set of bare card
/// types also handled by <see cref="ExileTargetSimpleRule"/>, so a card printing a
/// richer filter phrase (subtype, color+type, …) alongside a mana-value qualifier
/// still falls through to a different/new rule rather than being silently
/// mis-captured here.
/// Priority 70 — same tier as <see cref="ExileTargetQualifiedRule"/> (its sibling
/// "with power N or less/greater" shape); both run ahead of
/// <see cref="ExileTargetSimpleRule"/> (50), which would otherwise try to claim
/// the raw text via its permissive filter-phrase fallback.
/// </summary>
[SpellRule(Priority = 70)]
public sealed class ExileTargetPermanentByManaValueRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Exile\s+target\s+(?<type>creature|artifact|enchantment|planeswalker|permanent)"
    + @"\s+with\s+mana\s+value\s+(?<n>\d+)\s+or\s+(?<dir>less|fewer|greater|more)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var n = int.Parse(m.Groups["n"].Value);
    var dir = m.Groups["dir"].Value.ToLowerInvariant();
    var op = dir is "less" or "fewer" ? ComparisonOperator.LessThanOrEqual : ComparisonOperator.GreaterThanOrEqual;

    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = [m.Groups["type"].Value.ToLowerInvariant()],
          ManaValueComparison = new Comparison { Operator = op, Value = n },
        },
      },
    };
    return true;
  }
}
