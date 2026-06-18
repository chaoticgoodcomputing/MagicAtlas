namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the two-sentence conditional-token-creation shape:
/// "create a P1/T1 color1 Subtype1 creature token. If [condition], create a P2/T2 color2 Subtype2
/// creature token instead."
///
/// <para>
/// Rule 111 (tokens), CR 603 (triggered abilities). The word "instead" signals that the second
/// creation REPLACES the first (not in addition to it). Modelled as a <see cref="ConditionalEffect"/>:
/// <list type="bullet">
///   <item><see cref="ConditionalEffect.Condition"/> — the mid-resolution board-state predicate;</item>
///   <item><see cref="ConditionalEffect.Then"/> — the token created when the condition is true (the
///   "instead" token from the second sentence);</item>
///   <item><see cref="ConditionalEffect.Else"/> — the token created when the condition is false (the
///   default token from the first sentence).</item>
/// </list>
/// </para>
///
/// <para>
/// The Necrobloom (BIG) is the canonical example:
/// "create a 0/1 green Plant creature token. If you control seven or more lands with different names,
/// create a 2/2 black Zombie creature token instead."
/// CR 207.2c: "Landfall" is a CR ability word with no special rules meaning. CR 603: triggered
/// abilities fire "Whenever a land you control enters". The condition "you control seven or more lands
/// with different names" is an <see cref="OtherCondition"/> residual (ADR 0001) because
/// <see cref="ObjectFilter"/> does not yet have a "different names" axis.
/// </para>
///
/// <para>
/// Called as a composite path from
/// <see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/> BEFORE the sentence bundle
/// splitter, so the two-sentence shape is not incorrectly split into two independent token
/// creation effects.
/// </para>
/// </summary>
public static class CreateTokenOrInsteadIfConditionRule
{
  // Matches the full two-sentence pattern (without trailing period stripped by caller):
  //   "create a P/T color Subtype creature token. If [condition], create a P/T color Subtype
  //    creature token instead"
  // Group "cond" captures the condition phrase between "If " and ",".
  // Groups "p1"/"t1"/"col1"/"sub1" capture the default token (first sentence).
  // Groups "p2"/"t2"/"col2"/"sub2" capture the replacement token (second sentence, "instead").
  private static readonly Regex _pattern = new(
    @"^create\s+a\s+(?<p1>\d+)/(?<t1>\d+)\s+(?<col1>white|blue|black|red|green)\s+(?<sub1>[A-Z][a-z]+)"
      + @"\s+creature\s+token\.\s+"
      + @"If\s+(?<cond>[^,]+),\s+create\s+a\s+(?<p2>\d+)/(?<t2>\d+)\s+(?<col2>white|blue|black|red|green)"
      + @"\s+(?<sub2>[A-Z][a-z]+)\s+creature\s+token\s+instead\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorMap =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W", ["blue"] = "U", ["black"] = "B", ["red"] = "R", ["green"] = "G",
    };

  /// <summary>
  /// Attempts to match <paramref name="text"/> as the two-sentence "create X. If condition, create Y
  /// instead" pattern. Returns the <see cref="ConditionalEffect"/> on success; null on no-match.
  /// </summary>
  public static Effect? TryMatch(string text)
  {
    var match = _pattern.Match(text);
    if (!match.Success)
    {
      return null;
    }

    if (!_colorMap.TryGetValue(match.Groups["col1"].Value, out var colorCode1) ||
        !_colorMap.TryGetValue(match.Groups["col2"].Value, out var colorCode2))
    {
      return null;
    }

    var sub1 = NormalizeSubtype(match.Groups["sub1"].Value);
    var sub2 = NormalizeSubtype(match.Groups["sub2"].Value);
    var condText = match.Groups["cond"].Value.Trim();

    // Default token (created when condition is FALSE — the "Else" branch).
    var defaultToken = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(1),
      Token = new TokenDefinition
      {
        Power = match.Groups["p1"].Value,
        Toughness = match.Groups["t1"].Value,
        Colors = [colorCode1],
        Types = ["creature"],
        Subtypes = [sub1],
      },
    };

    // Replacement token (created when condition is TRUE — the "Then" branch).
    var replacementToken = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(1),
      Token = new TokenDefinition
      {
        Power = match.Groups["p2"].Value,
        Toughness = match.Groups["t2"].Value,
        Colors = [colorCode2],
        Types = ["creature"],
        Subtypes = [sub2],
      },
    };

    return new ConditionalEffect
    {
      Condition = new OtherCondition { Text = condText },
      Then = replacementToken,
      Else = defaultToken,
    };
  }

  private static string NormalizeSubtype(string raw)
  {
    if (raw.Length == 0) return raw;
    return char.ToUpperInvariant(raw[0]) + raw[1..].ToLowerInvariant();
  }
}
