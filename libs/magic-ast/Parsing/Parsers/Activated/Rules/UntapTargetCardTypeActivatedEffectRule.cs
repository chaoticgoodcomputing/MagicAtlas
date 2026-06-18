namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Untap target creature." / "Untap target artifact." / "Untap another target permanent." /
/// etc. in an activated ability effect position — uses CardTypes (not Subtypes) for the
/// card-type words that are CR card types rather than subtypes. Runs at higher priority
/// than <see cref="UntapEffectRule"/> (Priority 994 > 993) so card-type words are routed
/// here and do not land in <c>Subtypes</c> on the generic rule.
///
/// <para>
/// Also handles the "another target [type]" form (e.g. "Untap another target permanent."),
/// where "another" excludes the source permanent (<c>ExcludeSelf = true</c>). CR 109.5:
/// "'Another' means a different object." — the source is excluded from the legal targets.
/// </para>
///
/// CR 300.1 / 205.2a: the card types (artifact, creature, enchantment, instant, land,
/// planeswalker, sorcery, battle, …) are distinct from subtypes (CR 205.3 / 205.3i). "Forest"
/// is a land subtype; "creature" is a card type.
/// </summary>
[ActivatedEffectRule(Priority = 994)]
public sealed class UntapTargetCardTypeActivatedEffectRule : IActivatedEffectRule
{
  // Anchored. Matches both:
  //   "Untap target [type]." — plain targeted untap
  //   "Untap another target [type]." — excludes the source (ExcludeSelf)
  // Named group <another> is present when "another" appears before "target".
  // Subtypes (Forest, Island, etc.) are handled by UntapEffectRule.
  private static readonly Regex _pattern = new(
    @"^Untap\s+(?<another>another\s+)?target\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent|spell)\s*\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var m = _pattern.Match(effectText.Trim());
    if (!m.Success)
    {
      return null;
    }

    var cardType = m.Groups["type"].Value.ToLowerInvariant();
    var excludeSelf = m.Groups["another"].Success ? true : (bool?)null;

    return new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = [cardType], ExcludeSelf = excludeSelf },
      },
    };
  }
}
