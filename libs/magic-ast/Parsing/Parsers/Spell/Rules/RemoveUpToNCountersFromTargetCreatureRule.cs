namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Remove up to three counters from target creature." — an unqualified
/// counter-removal spell effect with a bounded "up to N" count (Heartless Act,
/// mode 2).
///
/// <para>
/// The counters removed are unqualified: the player chooses which counters (of any
/// kind) to remove, up to the stated maximum (CR 122.1 — counters; CR 122.3 governs
/// removal). MAST records this with <see cref="RemoveCountersEffect"/> using the
/// sentinel <c>CounterType = "any"</c> ("counters of any kind, chosen by the removing
/// player" — distinct from the printed-type values like "+1/+1" and from the "all"
/// sentinel meaning "every counter type currently present on a source"). The bounded
/// "up to N" cardinality rides on <see cref="UpToQuantity"/> (minimum 0), the
/// documented home for "up to N" quantities, rather than a fixed
/// <see cref="LiteralQuantity"/>.
/// </para>
///
/// <para>
/// Anchored to the full "counters from target creature" surface (bare "counters", no
/// type qualifier; "target creature", not "target permanent"/"noncreature artifact"/a
/// type disjunction) so it matches only this shape and does not shadow the siblings
/// that remove typed counters ("charge counters", Gremlin Mine) or target other
/// object sets (Glissa Sunslayer / Render Inert "target permanent", Price of Betrayal's
/// "target artifact, creature, planeswalker, or opponent").
/// </para>
/// </summary>
[SpellRule(Priority = 60)]
public sealed class RemoveUpToNCountersFromTargetCreatureRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Remove\s+up\s+to\s+(?<n>\w+)\s+counters\s+from\s+target\s+creature$",
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

    if (!SpellRuleHelpers.TryParseSmallWord(m.Groups["n"].Value, out var max))
    {
      return false;
    }

    effect = new RemoveCountersEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      CounterType = "any",
      Count = new UpToQuantity { Maximum = max },
    };
    return true;
  }
}
