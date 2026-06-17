namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You gain N life and draw M cards." — Candy Trail's
/// "{2}, {T}, Sacrifice this artifact: You gain 3 life and draw a card." A single
/// "... and ..."-joined sentence that is two sibling effects: a
/// <see cref="GainLifeEffect"/> (you gain N) followed by a <see cref="DrawCardsEffect"/>
/// (you draw M).
///
/// <para>
/// CR 119.3 (gain life), CR 121.1 (draw). The two clauses are independent actions
/// resolved in order; representing them as a flat sibling pair (not one residual)
/// preserves both — the naive re-point otherwise drops the gain-life conjunct.
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects sit as a
/// flat sibling pair on <c>Effects</c> (the DrawThenSelfToTopOfLibrary / SurveilThenDraw
/// convention), not nested under a CompositeEffect. <see cref="TryMatch"/> always
/// returns null so the single-effect path never claims the sentence.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 951)]
public sealed class GainLifeAndDrawCardsEffectRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^You\s+gain\s+(?<life>X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\s+and\s+draw\s+(?<cards>a|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>
  /// Always returns null — this shape always produces two sibling effects, so it is
  /// served exclusively via <see cref="TryMatchMulti"/>.
  /// </remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var match = Pattern.Match(effectText.Trim().TrimEnd('.'));
    if (!match.Success)
    {
      return false;
    }

    var lifeText = match.Groups["life"].Value;
    Quantity lifeAmount;
    if (lifeText.Equals("X", StringComparison.OrdinalIgnoreCase))
    {
      lifeAmount = VariableQuantity.X;
    }
    else if (lifeText.Equals("Y", StringComparison.OrdinalIgnoreCase))
    {
      lifeAmount = VariableQuantity.Y;
    }
    else if (lifeText.Equals("Z", StringComparison.OrdinalIgnoreCase))
    {
      lifeAmount = VariableQuantity.Z;
    }
    else
    {
      var count = ActivatedRuleHelpers.ParseNumberWord(lifeText) ?? 1;
      lifeAmount = LiteralQuantity.Of(count);
    }

    var cardsCount = ActivatedRuleHelpers.ParseNumberWord(match.Groups["cards"].Value) ?? 1;

    effects = new List<Effect>
    {
      new GainLifeEffect { Amount = lifeAmount, Player = ObjectReference.You() },
      new DrawCardsEffect { Count = LiteralQuantity.Of(cardsCount), Player = ObjectReference.You() },
    };
    return true;
  }
}
