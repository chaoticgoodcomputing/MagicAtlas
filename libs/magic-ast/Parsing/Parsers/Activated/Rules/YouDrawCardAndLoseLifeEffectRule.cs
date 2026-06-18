namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You draw a card and lose N life." — the Vraska planeswalker 0-ability shape.
///
/// <para>
/// A single "… and …"-joined sentence that expands to two sibling effects:
/// <see cref="DrawCardsEffect"/> (you draw 1) and <see cref="LoseLifeEffect"/>
/// (you lose N). Differs from the similar
/// <c>TryParseYouDrawAndYouLoseLife</c> helper in TriggeredAbilityParser (which
/// requires the second "you" — "you draw … and <b>you</b> lose …") and from
/// <see cref="GainLifeAndDrawCardsEffectRule"/> (which goes gain-then-draw in
/// the opposite order).
/// </para>
///
/// <para>
/// CR 121.1 (draw), CR 119.3 (lose life). Both clauses reference the controller
/// ("you"), so both effects carry <see cref="ObjectReferenceKind.You"/>.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): "draw a card and lose" appears as a SUBSTRING of
/// no sibling rule, but the anchor prevents a future broader pattern from
/// consuming this sentence and silently dropping the lose-life conjunct.
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects sit
/// as a flat sibling pair on <c>Effects</c> — not wrapped in a
/// <c>CompositeEffect</c>.  <see cref="TryMatch"/> always returns null so the
/// single-effect path never claims the sentence.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 952)]
public sealed class YouDrawCardAndLoseLifeEffectRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  // Anchored: "You draw a card and lose N life" or "You draw N cards and lose N life"
  private static readonly Regex Pattern = new(
    @"^You\s+draw\s+(?<draw>a|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?\s+and\s+lose\s+(?<life>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>Always returns null — this shape always produces two sibling effects.</remarks>
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

    var drawRaw = match.Groups["draw"].Value;
    var lifeRaw = match.Groups["life"].Value;

    // "a" is a special case that ParseNumberWord won't catch without surrounding spaces.
    var drawCount = drawRaw.Equals("a", StringComparison.OrdinalIgnoreCase)
      ? 1
      : (ActivatedRuleHelpers.ParseNumberWord(drawRaw) ?? 1);
    var lifeCount = ActivatedRuleHelpers.ParseNumberWord(lifeRaw) ?? 1;

    var you = ObjectReference.You();
    effects = new List<Effect>
    {
      new DrawCardsEffect { Count = LiteralQuantity.Of(drawCount), Player = you },
      new LoseLifeEffect { Amount = LiteralQuantity.Of(lifeCount), Player = you },
    };
    return true;
  }
}
