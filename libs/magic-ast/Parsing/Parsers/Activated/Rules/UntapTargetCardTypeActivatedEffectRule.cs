namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Untap target creature." / "Untap target artifact." / "Untap target artifact creature." /
/// "Untap another target permanent." / etc. in an activated ability effect position — uses
/// CardTypes (not Subtypes) for the card-type words that are CR card types rather than subtypes.
/// Runs at higher priority than <see cref="UntapEffectRule"/> (Priority 994 > 993) so card-type
/// words are routed here and do not land in <c>Subtypes</c> on the generic rule.
///
/// <para>
/// The target filter accepts a COMPOUND card-type noun (adjacent card-type words with no
/// "or"/"," between them, e.g. "artifact creature") as a conjunction — an object that has ALL
/// the listed card types, emitted as an ordered <c>CardTypes</c> list (CR 205.1a: a permanent
/// can have multiple card types; "artifact creature" is a single object that is both). This is
/// distinct from a disjunctive "artifact or creature" phrasing (which carries an explicit "or"
/// and is not matched here).
/// </para>
///
/// <para>
/// Also handles the "another target [type]" form (e.g. "Untap another target permanent."),
/// where "another" excludes the source permanent (<c>ExcludeSelf = true</c>). CR 109.5:
/// "'Another' means a different object." — the source is excluded from the legal targets.
/// </para>
///
/// <para>
/// CR 602.1: "Activated abilities have a cost and an effect. They are written as '[Cost]:
/// [Effect.] [Activation instructions (if any).]'…" — this rule handles the untap effect that
/// follows the colon (Voltaic Construct: "{2}: Untap target artifact creature.").
/// </para>
///
/// CR 300.1 / 205.2a: the card types (artifact, creature, enchantment, instant, land,
/// planeswalker, sorcery, battle, …) are distinct from subtypes (CR 205.3 / 205.3i). "Forest"
/// is a land subtype; "creature" is a card type.
/// </summary>
[ActivatedEffectRule(Priority = 994)]
public sealed class UntapTargetCardTypeActivatedEffectRule : IActivatedEffectRule
{
  // Anchored (^…$). Matches:
  //   "Untap target [type]." — plain targeted untap
  //   "Untap target [type] [type]…." — compound card-type conjunction ("artifact creature")
  //   "Untap another target [type]." — excludes the source (ExcludeSelf)
  // Named group <another> is present when "another" appears before "target"; <types> captures
  // one or more space-separated card-type words. A disjunctive "artifact or creature" carries an
  // explicit "or" and is NOT matched here. Subtypes (Forest, Island, …) are handled by UntapEffectRule.
  private const string _typeAlternation = "creature|artifact|enchantment|land|planeswalker|permanent|spell";

  private static readonly Regex _pattern = new(
    $@"^Untap\s+(?<another>another\s+)?target\s+(?<types>(?:{_typeAlternation})(?:\s+(?:{_typeAlternation}))*)\s*\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var m = _pattern.Match(effectText.Trim());
    if (!m.Success)
    {
      return null;
    }

    var cardTypes = m.Groups["types"].Value
      .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .Select(t => t.ToLowerInvariant())
      .ToArray();
    var excludeSelf = m.Groups["another"].Success ? true : (bool?)null;

    return new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = cardTypes, ExcludeSelf = excludeSelf },
      },
    };
  }
}
