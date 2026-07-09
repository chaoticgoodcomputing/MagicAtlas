namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You gain life equal to the sacrificed creature's toughness." — the derived
/// life-gain resolution of an activated ability whose cost sacrifices a creature
/// (Diamond Valley: "{T}, Sacrifice a creature: You gain life equal to the
/// sacrificed creature's toughness."). The gained amount is a
/// <see cref="DerivedQuantity"/> keyed on <see cref="DerivedKind.Toughness"/> whose
/// <see cref="DerivedQuantity.Source"/> is the anaphoric "the sacrificed creature":
/// the object moved to the graveyard by this same ability's
/// <see cref="MagicAST.AST.Costs.SacrificeCost"/> (CR 701.21a). That back-reference
/// is a CR 607.1 linked ability — the effect "directly refers to … objects … that
/// were affected by" the cost — so MAST records the textual link, not the runtime
/// toughness value (ADR 0004: reference-not-resolution).
///
/// <para>
/// The activated-ability sibling of
/// <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.YouGainLifeEqualToThatCreaturesToughnessTriggeredRule"/>
/// (Engulfing Slagwurm's "You gain life equal to that creature's toughness."),
/// which sources the toughness from a trigger's "that creature" instead of a
/// sacrificed-cost creature. CR 119.3: "If an effect causes a player to gain life
/// or lose life, that player's life total is adjusted accordingly."
/// </para>
///
/// <para>
/// Anchored (<c>^…$</c>) to the exact sacrificed-toughness surface so it cannot
/// claim the numeric ("You gain N life", <see cref="GainLifeEffectRule"/>) or
/// life-lost ("You gain life equal to the life lost this way",
/// <see cref="GainLifeEqualToLifeLostEffectRule"/>) siblings, and so a future
/// broader "You gain life equal to …" pattern cannot silently swallow this
/// derived-quantity link.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 990)]
public sealed class GainLifeEqualToSacrificedCreaturesToughnessEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^You\s+gain\s+life\s+equal\s+to\s+the\s+sacrificed\s+creature's\s+toughness$",
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
      Amount = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.Toughness,
        Source = "the sacrificed creature",
      },
      Player = ObjectReference.You(),
    };
  }
}
