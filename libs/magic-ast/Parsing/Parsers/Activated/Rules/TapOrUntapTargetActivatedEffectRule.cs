namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Linq;
using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// "(You may) tap or untap target [type]." in an activated ability effect position —
/// the controller chooses whether to tap or untap the named target. Runs at higher
/// priority than <see cref="TapEffectRule"/> and
/// <see cref="UntapTargetCardTypeActivatedEffectRule"/> (Priority 995 &gt; 994) so the
/// "tap or untap" phrase is claimed here before the plain tap/untap rules see it.
///
/// <para>
/// CR 701.26a: "To tap a permanent, turn it sideways from an upright position. Only
/// untapped permanents can be tapped."
/// </para>
/// <para>
/// CR 701.26b: "To untap a permanent, rotate it back to the upright position from a
/// sideways position. Only tapped permanents can be untapped."
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 995)]
public sealed class TapOrUntapTargetActivatedEffectRule : IActivatedEffectRule
{
  // Named groups:
  //   optional — "you may" prefix (present ⇒ IsOptional = true)
  //   types    — everything after "target" (the card-type disjunction phrase)
  private static readonly Regex Pattern = new(
    @"^(?<optional>you\s+may\s+)?tap\s+or\s+untap\s+target\s+(?<types>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var isOptional = m.Groups["optional"].Success;
    var typesPhrase = m.Groups["types"].Value.Trim();

    // Delegate to SpellRuleHelpers.SplitTypeDisjunction so we share the same
    // "creature", "creature or land", "artifact, creature, or land" lexer as
    // the spell and triggered tap/untap rules.
    var types = SpellRuleHelpers
      .SplitTypeDisjunction(typesPhrase)
      .Where(t => t.Length > 0)
      .ToList();

    if (types.Count == 0)
    {
      return null;
    }

    var target = new ObjectReference
    {
      Kind = ObjectReferenceKind.Target,
      Filter = new ObjectFilter { CardTypes = types },
    };

    return MagicAST.AST.Effects.Core.EffectWrap.Optional(
      new TapOrUntapEffect { Target = target },
      isOptional
    );
  }
}
