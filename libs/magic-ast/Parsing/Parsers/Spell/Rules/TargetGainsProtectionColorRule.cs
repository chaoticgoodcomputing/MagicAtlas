namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Recognises the "gains protection from the color of your choice UEOT" shape:
///   "Target creature you control gains protection from the color of your choice until end of turn."
///
/// This covers protection-grant instants such as Gods Willing (THS) and Shelter
/// (INV) where the protected color is chosen by the controller at resolution,
/// not fixed in the oracle text.
///
/// The chosen color is modeled as
/// <see cref="ProtectionQualityKind.ChosenColor"/> — a declarative marker that
/// the protection quality is controller-chosen at resolution time. MAST records
/// the instruction to choose, not the chosen value (per the descriptive-not-engine
/// doctrine; see ChooseColorEffect for the analogous standalone color-choice shape,
/// which here has no entry timing — the choice is a spell effect, so no
/// StaticTimingKind.AsThisEnters applies).
///
/// Rule citations: 702.16 (Protection), 702.16e (protection from a player's choice),
/// 613.1c (Layer 6 — ability-granting continuous effects), 611 (continuous effects
/// with duration).
/// </summary>
[SpellRule]
public sealed class TargetGainsProtectionColorRule : ISpellRule
{
  // Matches: "Target creature you control gains protection from the color of your choice until end of turn"
  // The regex is anchored; trailing period has already been stripped by the spell parser.
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+you\s+control\s+gains\s+protection\s+from\s+the\s+color\s+of\s+your\s+choice\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new GainAbilityEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
        },
      },
      GainedAbility = new StaticAbility
      {
        KeywordSource = "Protection",
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
      Duration = new UntilEndOfTurnDuration(),
    };
    return true;
  }
}
