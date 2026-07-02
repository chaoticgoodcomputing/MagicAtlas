namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// "exile target [filter]." — single-target permanent exile on the triggered side.
/// Delegates filter parsing to <see cref="SpellRuleHelpers.ParseTargetFilter"/>
/// (same lexical surface as <see cref="Spell.Rules.ExileTargetSimpleRule"/>).
/// Covers bare card-type targets (creature, permanent, artifact, enchantment,
/// planeswalker) and richer filter phrases via <see cref="SpellRuleHelpers.ParseTargetFilter"/>:
/// <list type="bullet">
///   <item>Subtype-only: "Spirit", "Human"</item>
///   <item>Color + card type: "black creature", "white artifact"</item>
///   <item>non- prefix + card type: "nonblack creature", "nontoken creature"</item>
/// </list>
/// This rule handles PERMANENT exile (no "until [this] leaves the battlefield" suffix).
/// The <see cref="ExileUntilLeavesTriggeredRule"/> handles the temporary Oblivion Ring shape.
///
/// <para>
/// An optional "another" before "target" is the CR 109.5 self-exclusion marker
/// ("exile another target permanent" — Flickerwisp): it rides on
/// <see cref="ObjectFilter.ExcludeSelf"/> = true rather than on the card-type axis.
/// </para>
/// Rule 701.13 (exile action) + Rule 205.3 (card types) + Rule 109.5 ("another").
/// </summary>
[TriggeredRule]
public sealed class ExileTargetTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^exile\s+(?<another>another\s+)?target\s+(?<filter>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var filterPhrase = m.Groups["filter"].Value.Trim();
    var filter = SpellRuleHelpers.ParseTargetFilter(filterPhrase);
    if (filter is null)
    {
      return false;
    }

    // "another target ..." excludes the source permanent (CR 109.5). The
    // self-exclusion is an axis on the filter, not a card-type qualifier.
    if (m.Groups["another"].Success)
    {
      filter = filter with { ExcludeSelf = true };
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
