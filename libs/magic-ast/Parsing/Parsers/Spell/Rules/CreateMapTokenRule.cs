namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Creating a predefined Map token (CR 111.10 — predefined tokens) from a spell.
///
/// <para>
/// A Map token is a colorless artifact token with "{1}, {T}, Sacrifice this token: Target
/// creature you control explores. Activate only as a sorcery." Its ability body is carried by
/// the parenthetical reminder text (CR 207.2 — reminder text has no rules meaning), which
/// <see cref="SpellAbilityParser"/> strips before dispatch, so this rule matches the bare
/// "create a Map token" form. The token's structural identity (artifact + "Map" subtype) is
/// modelled by <see cref="TokenDefinition.Map"/> without re-asserting the reminder body as free
/// text — the same clean predefined-token treatment used for Powerstone in
/// <c>CreateTappedPredefinedTokenRule</c>.
/// </para>
///
/// <para>
/// Two surfaces are handled:
/// <list type="bullet">
///   <item>Standalone "create [count] Map token(s)" via <see cref="TryMatch"/> — a single
///   <see cref="CreateTokenEffect"/>.</item>
///   <item>The "Draw N cards and create a Map token" compound (Fanatical Offering — LCI) via
///   <see cref="TryMatchMulti"/>, expanded to a flat [<see cref="DrawCardsEffect"/>,
///   <see cref="CreateTokenEffect"/>] sibling pair — the same shape
///   <see cref="DrawAndCreateTreasureRule"/> produces for the Treasure variant.</item>
/// </list>
/// Both patterns are anchored (<c>^…$</c>) so neither hijacks a more-specific sibling clause.
/// </para>
///
/// <para>
/// CR 111.1 (a token is a marker representing a permanent that isn't a card), CR 111.10
/// (predefined tokens), CR 121.1 (a player draws a card by putting the top card of their
/// library into their hand).
/// </para>
/// </summary>
[SpellRule]
public sealed class CreateMapTokenRule : ISpellRule, IMultiSpellRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+|X|Y|Z";

  // Standalone "create [count] Map token(s)".
  private static readonly Regex StandalonePattern = new(
    $@"^create\s+(?<n>{CountTokens})\s+Map\s+tokens?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "Draw N cards and create [count] Map token(s)".
  private static readonly Regex DrawAndCreatePattern = new(
    $@"^Draw\s+(?<dn>{CountTokens})\s+cards?\s+and\s+create\s+(?<tn>{CountTokens})\s+Map\s+tokens?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = StandalonePattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = ParseCount(m.Groups["n"].Value),
      Token = TokenDefinition.Map(),
    };
    return true;
  }

  /// <inheritdoc/>
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = DrawAndCreatePattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effects = new List<Effect>
    {
      new DrawCardsEffect
      {
        Count = ParseCount(m.Groups["dn"].Value),
        Player = ObjectReference.You(),
      },
      new CreateTokenEffect
      {
        Player = ObjectReference.You(),
        Count = ParseCount(m.Groups["tn"].Value),
        Token = TokenDefinition.Map(),
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
