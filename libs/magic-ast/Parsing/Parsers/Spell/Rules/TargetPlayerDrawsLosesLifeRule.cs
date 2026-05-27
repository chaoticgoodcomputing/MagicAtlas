namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target player draws N cards and loses N life." / "Target opponent draws N cards and loses N life."
///
/// This is the targeted draw-and-drain pattern. The draw count and life-loss amount use the
/// same quantity token (they may differ in wording but represent the same value in all current
/// oracle text instances). Both effects share the same target reference.
///
/// Recognised shapes:
/// <list type="bullet">
///   <item>"Target player draws X cards and loses X life." — Damnable Pact</item>
///   <item>"Target player draws two cards and loses 2 life." — hypothetical literal variant</item>
///   <item>"Target opponent draws a card and loses 1 life." — opponent-targeted variant</item>
/// </list>
///
/// Emits a flat <c>[DrawCardsEffect, LoseLifeEffect]</c> list via
/// <see cref="IMultiSpellRule.TryMatchMulti"/>. The single-effect
/// <see cref="ISpellRule.TryMatch"/> always returns false.
/// </summary>
[SpellRule]
public sealed class TargetPlayerDrawsLosesLifeRule : ISpellRule, IMultiSpellRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+|X|Y|Z";

  private static readonly Regex _pattern = new(
    $@"^Target\s+(?<target>player|opponent)\s+draws?\s+(?<draw>{CountTokens})\s+cards?\s+and\s+loses?\s+(?<lose>{CountTokens})\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // -------------------------------------------------------------------------
  // ISpellRule — single-effect path intentionally disabled.
  // -------------------------------------------------------------------------
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  // -------------------------------------------------------------------------
  // IMultiSpellRule — flat [DrawCardsEffect, LoseLifeEffect] list.
  // -------------------------------------------------------------------------
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var isOpponent = m.Groups["target"].Value.Equals(
      "opponent", StringComparison.OrdinalIgnoreCase
    );
    var player = isOpponent
      ? new ObjectReference { Kind = ObjectReferenceKind.Opponent }
      : ObjectReference.Target(ObjectFilter.Player());

    var drawQuantity = ParseQuantity(m.Groups["draw"].Value);
    var loseQuantity = ParseQuantity(m.Groups["lose"].Value);

    effects = new List<Effect>
    {
      new DrawCardsEffect
      {
        Count = drawQuantity,
        Player = player,
      },
      new LoseLifeEffect
      {
        Amount = loseQuantity,
        Player = player,
      },
    };
    return true;
  }

  private static Quantity ParseQuantity(string raw)
  {
    var lower = raw.ToLowerInvariant();
    if (lower is "x" or "y" or "z")
    {
      return new VariableQuantity { Name = lower.ToUpperInvariant() };
    }
    return LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(raw));
  }
}
