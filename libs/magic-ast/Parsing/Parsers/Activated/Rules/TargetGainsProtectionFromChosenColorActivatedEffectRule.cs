namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Target creature gains protection from the chosen color until end of turn."
/// (Floating Shield's "Sacrifice this Aura:" activated ability.)
///
/// <para>
/// Distinct from <see cref="TargetGainsProtectionColorActivatedEffectRule"/>'s "the
/// color of your choice" (<see cref="ProtectionQualityKind.ChosenColor"/> — a fresh
/// choice made anew at THIS ability's own resolution, with no persistent binding):
/// "the chosen color" is a DEFINITE back-reference to the single color already bound
/// by the paired "As this Aura enters, choose a color." replacement ability (CR 607
/// linked ability; CR 614.12; see <see cref="MagicAST.Parsing.Parsers.Static.ChooseColorOnEntryRule"/>) —
/// every mention of "the chosen color" printed on this card names the identical
/// value. Modeled as <see cref="ProtectionQualityKind.ChosenCharacteristic"/> +
/// <see cref="ProtectionQuality.ChosenCharacteristic"/> = <see cref="ChosenCharacteristicKind.Color"/>,
/// mirroring the object-reference analogue used by
/// <see cref="MagicAST.Parsing.Parsers.Static.PreventAllDamageToEnchantedCreatureByChosenColorSourceStaticRule"/>
/// ("sources of the chosen color").
/// </para>
///
/// <para>
/// Also unlike Mother of Runes' "Target creature you control gains …", this text
/// carries no "you control" restriction — plain "Target creature" — so the grant's
/// filter has no <see cref="ControllerFilter"/>.
/// </para>
///
/// Rule citations: 702.16 (Protection), 702.16b (protection from a quality), 607.1
/// (linked abilities), 613.1c (Layer 6 — ability-granting continuous effects), 611
/// (continuous effects with a duration).
/// </summary>
[ActivatedEffectRule(Priority = 996)]
public sealed class TargetGainsProtectionFromChosenColorActivatedEffectRule : IActivatedEffectRule
{
  // Anchored: matches the whole effect fragment (trailing period already stripped by
  // the parser). Anchoring prevents this from swallowing a substring of a longer,
  // more-specific sibling shape.
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+gains\s+protection\s+from\s+the\s+chosen\s+color\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    if (!_pattern.IsMatch(effectText.Trim().TrimEnd('.')))
    {
      return null;
    }

    return new GainAbilityEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      GainedAbility = new StaticAbility
      {
        KeywordSource = KeywordAbility.Protection,
        Effects =
        [
          new ProtectionEffect
          {
            From =
            [
              new ProtectionQuality
              {
                Kind = ProtectionQualityKind.ChosenCharacteristic,
                ChosenCharacteristic = ChosenCharacteristicKind.Color,
              },
            ],
          },
        ],
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
