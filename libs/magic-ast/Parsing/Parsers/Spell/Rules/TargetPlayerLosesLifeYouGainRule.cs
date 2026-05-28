namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target player loses N life and you gain N life." — the targeted drain pattern
/// where the opponent-chosen player loses a fixed amount and the controller gains the
/// same amount (CR 119.3: "If an effect causes a player to gain life or lose life,
/// that player's life total is adjusted accordingly.").
///
/// <para>
/// Recognised shapes:
/// <list type="bullet">
///   <item>"Target player loses 4 life and you gain 4 life." — Absorb Vis</item>
/// </list>
/// </para>
///
/// <para>
/// Emits a flat [<see cref="LoseLifeEffect"/>, <see cref="GainLifeEffect"/>] pair via
/// <see cref="IMultiSpellRule.TryMatchMulti"/>. The two amounts are allowed to differ
/// in oracle text; each is parsed independently. The drain amounts need not be equal
/// for the rule to fire — the conjunction "and you gain N life" is what identifies
/// the shape.
/// </para>
///
/// <para>
/// The single-effect <see cref="ISpellRule.TryMatch"/> always returns false — this
/// shape never reduces to a single Effect.
/// </para>
/// </summary>
[SpellRule]
public sealed class TargetPlayerLosesLifeYouGainRule : ISpellRule, IMultiSpellRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+|X|Y|Z";

  private static readonly Regex _pattern = new(
    $@"^Target\s+player\s+loses?\s+(?<lose>{CountTokens})\s+life\s+and\s+you\s+gain\s+(?<gain>{CountTokens})\s+life$",
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
  // IMultiSpellRule — flat [LoseLifeEffect, GainLifeEffect] pair.
  // -------------------------------------------------------------------------
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var loseQuantity = ParseQuantity(m.Groups["lose"].Value);
    var gainQuantity = ParseQuantity(m.Groups["gain"].Value);

    effects = new List<Effect>
    {
      new LoseLifeEffect
      {
        Amount = loseQuantity,
        Player = ObjectReference.Target(ObjectFilter.Player()),
      },
      new GainLifeEffect
      {
        Amount = gainQuantity,
        Player = ObjectReference.You(),
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
