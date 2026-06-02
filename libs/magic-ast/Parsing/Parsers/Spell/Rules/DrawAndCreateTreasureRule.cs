namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Draw [N] cards and create [N] Treasure tokens." — Unexpected Windfall / Pirate's Pillage pattern.
/// Expands to a flat [<see cref="DrawCardsEffect"/>, <see cref="CreateTokenEffect"/>] pair
/// so both effects sit as siblings on <see cref="MagicAST.AST.Abilities.SpellAbility.Effects"/>.
///
/// <para>
/// Reminder text on the Treasure token ("They're artifacts with...") is stripped by
/// <see cref="SpellAbilityParser"/> before dispatch, so this rule matches the bare
/// "Draw two cards and create two Treasure tokens" form.
/// </para>
///
/// <para>
/// Rule 120.1 (draw), Rule 111.9 (Treasure token).
/// </para>
/// </summary>
[SpellRule]
public sealed class DrawAndCreateTreasureRule : ISpellRule, IMultiSpellRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+|X|Y|Z";

  private static readonly Regex Pattern = new(
    $@"^Draw\s+(?<dn>{CountTokens})\s+cards?\s+and\s+create\s+(?<tn>{CountTokens})\s+Treasure\s+tokens?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>
  /// Returns <c>false</c> unconditionally — the compound draw+create shape always
  /// produces two sibling effects, so callers must use <see cref="TryMatchMulti"/> instead.
  /// </remarks>
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  /// <inheritdoc/>
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var drawCount = ParseCount(m.Groups["dn"].Value);
    var tokenCount = ParseCount(m.Groups["tn"].Value);

    effects = new List<Effect>
    {
      new DrawCardsEffect
      {
        Count = drawCount,
        Player = ObjectReference.You(),
      },
      new CreateTokenEffect
      {
        Player = ObjectReference.You(),
        Count = tokenCount,
        Token = TokenDefinition.Treasure(),
      },
    };
    return true;
  }

  private static Quantity ParseCount(string raw)
  {
    var lower = raw.ToLowerInvariant();
    if (lower is "x" or "y" or "z")
    {
      return new VariableQuantity { Name = lower.ToUpperInvariant() };
    }
    return LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(raw));
  }
}
