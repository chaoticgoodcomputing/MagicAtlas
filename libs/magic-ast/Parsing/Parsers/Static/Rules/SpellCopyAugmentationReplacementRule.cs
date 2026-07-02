namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.References;

/// <summary>
/// "If you would copy a spell one or more times, instead copy it that many times plus
/// an additional time. You may choose new targets for the additional copy."
/// — Twinning Staff's spell-copy augmentation static ability.
///
/// <para>
/// CR 707.10: "To copy a spell, activated ability, or triggered ability means to put a
/// copy of it onto the stack; a copy of a spell isn't cast…" This replacement effect
/// intercepts any event where the controller would copy a spell one or more times and
/// adds one additional copy to that total. The original copies still occur
/// (<see cref="ReplacementEffect.OriginalEventOccurs"/> = true); the modifier
/// (Type = "plusOne") specifies one extra copy beyond the original count.
/// The retarget permission (<c>MayChooseNewTargets = true</c>) applies specifically
/// to the additional copy, not the original copies.
/// </para>
/// </summary>
[StaticRule(Priority = 975)]
public sealed class SpellCopyAugmentationReplacementRule : IStaticRule
{
  // "If you would copy a spell one or more times, instead copy it that many times plus
  //  an additional time. You may choose new targets for the additional copy."
  // Both sentences must be present as one ability clause (newline-split by the oracle
  // clause splitter; the space-or-dot join between them is preserved here).
  private static readonly Regex _pattern = new(
    @"^\s*If\s+you\s+would\s+copy\s+a\s+spell\s+one\s+or\s+more\s+times,\s+instead\s+copy\s+it\s+that\s+many\s+times\s+plus\s+an\s+additional\s+time\.\s+You\s+may\s+choose\s+new\s+targets\s+for\s+the\s+additional\s+copy\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new ReplacementEffect
          {
            // The event being replaced: you would copy a spell one or more times.
            // Controller = You scopes the replacement to the controlling player's copies.
            Event = new SpellCopyEvent
            {
              MinimumQuantity = 1,
              Controller = ObjectReference.You(),
            },
            // The original copies still happen; "plus an additional time" adds one more.
            OriginalEventOccurs = true,
            Modifier = new ReplacementModifier
            {
              // "that many times plus an additional time" = original count + 1
              Type = "plusOne",
              // "You may choose new targets for the additional copy."
              MayChooseNewTargets = true,
            },
          },
        ],
      },
    ];
  }
}
