namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Self-referential analogue of
/// <see cref="TargetGainsProtectionColorActivatedEffectRule"/>: recognises the
/// "this creature gains protection from the color of your choice UEOT" interior of an
/// activated ability —
///   "This creature gains protection from the color of your choice until end of turn."
///
/// This is the effect half of Cartel Aristocrat ("Sacrifice another creature: This
/// creature gains protection from the color of your choice until end of turn."). Unlike
/// Mother of Runes ("Target creature you control gains protection ..."), the grant is on
/// the source permanent itself, so the recipient is <see cref="ObjectReferenceKind.Self"/>
/// with no filter — not a chosen/targeted creature.
///
/// The chosen color is modeled as <see cref="ProtectionQualityKind.ChosenColor"/> — a
/// declarative marker that the protection quality is controller-chosen at resolution
/// time (MAST records the instruction to choose, not the chosen value; the descriptive-
/// not-engine doctrine). The grant is a continuous ability-granting effect: a
/// <see cref="GainAbilityEffect"/> whose granted ability is the Protection static ability
/// carrying a <see cref="ProtectionEffect"/>.
///
/// Rule citations: 702.16 (Protection), 702.16e (protection from a player's choice of
/// color), 613.1c (Layer 6 — ability-granting continuous effects), 611 (continuous
/// effects with a duration).
/// </summary>
[ActivatedEffectRule(Priority = 996)]
public sealed class SelfGainsProtectionColorActivatedEffectRule : IActivatedEffectRule
{
  // Anchored: matches the whole effect fragment (trailing period already stripped by
  // the parser). Anchoring prevents this from swallowing a substring of a longer,
  // more-specific sibling shape.
  private static readonly Regex _pattern = new(
    @"^This\s+creature\s+gains\s+protection\s+from\s+the\s+color\s+of\s+your\s+choice\s+until\s+end\s+of\s+turn$",
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
      Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
      GainedAbility = new StaticAbility
      {
        KeywordSource = KeywordAbility.Protection,
        Effects =
        [
          new ProtectionEffect
          {
            From =
            [
              new ProtectionQuality { Kind = ProtectionQualityKind.ChosenColor },
            ],
          },
        ],
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
