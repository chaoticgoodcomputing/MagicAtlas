namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You gain life equal to the life lost this way." — controller life-gain whose
/// amount is derived from the life lost by a preceding <see cref="LoseLifeEffect"/>
/// in the same ability (the drain shape). Representative card: Scholar of Athreos
/// ("{2}{B}: Each opponent loses 1 life. You gain life equal to the life lost this
/// way."), whose activated ability's second sentence is dispatched here after the
/// multi-sentence pre-pass splits the effect body on the sentence boundary.
///
/// <para>
/// The amount is a <see cref="DerivedQuantity"/> keyed on
/// <see cref="DerivedKind.LifeLost"/> — reference-not-resolution (ADR 0004): MAST
/// records the textual link to "the life lost this way", not the runtime value
/// (CR 119.3: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly."). This is the activated-ability
/// counterpart of <see cref="MagicAST.Parsing.Parsers.Spell.Rules.GainLifeSpellRule"/>'s
/// "You gain life equal to the life lost this way." branch (Blood Tithe, the
/// sorcery twin of this drain sentence).
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the exact drain sentence. Distinct from the sibling
/// <see cref="GainLifeEffectRule"/> ("You gain N life", which requires a numeric
/// token after "gain" and so cannot match this life-equal-to phrase) — the anchor
/// prevents a future broader "You gain …" pattern from consuming this sentence and
/// silently losing the derived-quantity link.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 991)]
public sealed class GainLifeEqualToLifeLostEffectRule : IActivatedEffectRule
{
  // Anchored: "You gain life equal to the life lost this way"
  private static readonly Regex Pattern = new(
    @"^You\s+gain\s+life\s+equal\s+to\s+the\s+life\s+lost\s+this\s+way$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new GainLifeEffect
    {
      Amount = new DerivedQuantity { DerivedFrom = DerivedKind.LifeLost },
      Player = ObjectReference.You(),
    };
  }
}
