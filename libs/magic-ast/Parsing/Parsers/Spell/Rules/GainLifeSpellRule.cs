namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You gain N life." — spell-resolution life-gain. Recuperate's first modal option.
/// Also handles "You gain life equal to the life lost this way." — derived-quantity
/// gain linked to a preceding LoseLifeEffect (Rule 119.3). Blood Tithe's second clause.
/// Also handles "Target player gains N life." — life-gain whose subject is a targeted
/// player rather than the controller (CR 119.3: "If an effect causes a player to gain
/// life or lose life, that player's life total is adjusted accordingly.").
/// Also handles "If this spell was kicked, you gain life equal to the life lost this way."
/// — a conditioned life-gain gated on the kicker cost having been paid (CR 702.33d–e,
/// Blood Tribute). Uses the <see cref="GainLifeEffect.Condition"/> field (mirrors
/// <see cref="DrawCardsEffect.Condition"/>).
/// Representative card: Soothing Balm ({1}{W}, Instant).
/// </summary>
[SpellRule]
public sealed class GainLifeSpellRule : ISpellRule
{
  private const string CountTokens =
    @"X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten";

  private static readonly Regex LiteralPattern = new(
    $@"^You\s+gain\s+(?<amount>{CountTokens})\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex LifeLostPattern = new(
    @"^You\s+gain\s+life\s+equal\s+to\s+the\s+life\s+lost\s+this\s+way$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "If this spell was kicked, you gain life equal to the life lost this way."
  /// The "if this spell was kicked" prefix maps to <see cref="KeywordCostPaidCondition"/>
  /// keyed on <see cref="KeywordAbility.Kicker"/> (CR 702.33d–e: "if it was kicked").
  /// </summary>
  private static readonly Regex KickedLifeLostPattern = new(
    @"^If\s+this\s+spell\s+was\s+kicked,\s+you\s+gain\s+life\s+equal\s+to\s+the\s+life\s+lost\s+this\s+way$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex TargetPlayerPattern = new(
    $@"^Target\s+player\s+gains?\s+(?<amount>{CountTokens})\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // "If this spell was kicked, you gain life equal to the life lost this way."
    // CR 702.33d–e: gated on the kicker cost having been paid.
    if (KickedLifeLostPattern.IsMatch(text))
    {
      effect = new GainLifeEffect
      {
        Amount = new DerivedQuantity { DerivedFrom = DerivedKind.LifeLost },
        Player = ObjectReference.You(),
        Condition = new KeywordCostPaidCondition { Keyword = KeywordAbility.Kicker },
      };
      return true;
    }

    // "You gain life equal to the life lost this way." — DerivedQuantity(LifeLost)
    if (LifeLostPattern.IsMatch(text))
    {
      effect = new GainLifeEffect
      {
        Amount = new DerivedQuantity { DerivedFrom = DerivedKind.LifeLost },
        Player = ObjectReference.You(),
      };
      return true;
    }

    // "Target player gains N life." — CR 119.3
    var tm = TargetPlayerPattern.Match(text);
    if (tm.Success)
    {
      effect = new GainLifeEffect
      {
        Amount = ParseAmount(tm.Groups["amount"].Value),
        Player = ObjectReference.Target(ObjectFilter.Player()),
      };
      return true;
    }

    var m = LiteralPattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    effect = new GainLifeEffect
    {
      Amount = ParseAmount(m.Groups["amount"].Value),
      Player = ObjectReference.You(),
    };
    return true;
  }

  private static Quantity ParseAmount(string raw)
  {
    var lower = raw.ToLowerInvariant();
    if (lower is "x" or "y" or "z")
    {
      return new VariableQuantity { Name = lower.ToUpperInvariant() };
    }
    return LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(raw));
  }
}
