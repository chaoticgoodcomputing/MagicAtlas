namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Untap target creature." / "Untap target artifact." / etc. in an activated ability
/// effect position — uses CardTypes (not Subtypes) for the card-type words that
/// are CR card types rather than subtypes. Runs at higher priority than
/// <see cref="UntapEffectRule"/> (Priority 994 > 993) so card-type words are routed
/// here and do not land in <c>Subtypes</c> on the generic rule.
///
/// CR 300.1 / 205.2a: the card types (artifact, creature, enchantment, instant, land,
/// planeswalker, sorcery, battle, …) are distinct from subtypes (CR 205.3 / 205.3i). "Forest"
/// is a land subtype; "creature" is a card type.
/// </summary>
[ActivatedEffectRule(Priority = 994)]
public sealed class UntapTargetCardTypeActivatedEffectRule : IActivatedEffectRule
{
  // Anchored list of CR card types that appear after "Untap target" in activated
  // ability oracle text. Subtypes (Forest, Island, etc.) are handled by UntapEffectRule.
  private static readonly Regex _pattern = new(
    @"^Untap\s+target\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent|spell)\s*\.?\s*$",
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

    return new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = [cardType] },
      },
    };
  }
}
