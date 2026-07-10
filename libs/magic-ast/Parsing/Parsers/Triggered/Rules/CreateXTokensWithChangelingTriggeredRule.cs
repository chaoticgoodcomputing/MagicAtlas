namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "create X [P]/[T] colorless [Subtype] creature tokens with changeling." — a
/// variable-count (X) token-creation effect where the created tokens bear the
/// Changeling keyword (CR 702.73a: "'Changeling' means 'This object is every
/// creature type.'"). The Springleaf Parade shape: "create X 1/1 colorless
/// Shapeshifter creature tokens with changeling."
///
/// <para>
/// The count is the value chosen for the spell's own {X} in its mana cost (CR
/// 107.3 / CR 601.2b) — recorded as a <see cref="VariableQuantity"/> named "X"
/// rather than resolving the numeric value (ADR 0004 reference-not-resolution),
/// mirroring the variable-count convention used elsewhere (e.g. Mind Shatter's
/// "discards X cards").
/// </para>
///
/// <para>
/// Distinct from the generic <see cref="CreateTokenRule"/>: that rule's helpers
/// (<c>TriggeredRuleHelpers.ParseArticle</c>, <c>ParseColors</c>,
/// <c>ParseTokenAbilities</c>) don't recognise an "X" count, the "colorless" color
/// word, or a "with changeling" keyword suffix, so it would either silently
/// miscount (falling through to the default count of 1) or drop the Changeling
/// ability outright. This rule owns the full "create X … colorless … creature
/// tokens with changeling" shape end-to-end, mirroring the activated-ability
/// sibling <see cref="MagicAST.Parsing.Parsers.Activated.Rules.CreateTokenWithChangelingEffectRule"/>.
/// </para>
///
/// <para>
/// Priority 65 — above the generic <see cref="CreateTokenRule"/> (default 50) so
/// this more-specific shape is tried first, mirroring the priority band used by
/// other specific create-token variants (<see cref="CreateTokenWithDiesGainLifeAbilityRule"/>
/// at 60, <see cref="CreateTokensEqualToDieResultTriggeredRule"/> at 72). Anchored
/// (^…$) pattern prevents substring matches against sibling clauses.
/// </para>
/// </summary>
[TriggeredRule(Priority = 65)]
public sealed class CreateXTokensWithChangelingTriggeredRule : ITriggeredRule
{
  // "create X|Y|Z|<n>|a|an|<word> [P]/[T] colorless|<color> [Subtype] creature
  // token(s) with changeling."
  private static readonly Regex _pattern = new(
    @"^create\s+(?<count>X|Y|Z|a|an|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+" +
      @"(?<power>\d+)/(?<toughness>\d+)\s+" +
      @"(?<color>colorless|white|blue|black|red|green)\s+" +
      @"(?<subtype>[A-Za-z]+)\s+creature\s+tokens?\s+with\s+changeling\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> _colorMap =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
      ["colorless"] = "C",
    };

  private static readonly Dictionary<string, int> _wordCounts =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["a"] = 1,
      ["an"] = 1,
      ["one"] = 1,
      ["two"] = 2,
      ["three"] = 3,
      ["four"] = 4,
      ["five"] = 5,
      ["six"] = 6,
      ["seven"] = 7,
      ["eight"] = 8,
      ["nine"] = 9,
      ["ten"] = 10,
    };

  /// <summary>
  /// The Changeling keyword ability modelled as a static ability on the token,
  /// mirroring <see cref="MagicAST.Parsing.Parsers.Activated.Rules.CreateTokenWithChangelingEffectRule"/>'s
  /// ChangelingKeyword combinator shape (CR 702.73a).
  /// </summary>
  private static readonly StaticAbility _changelingTokenAbility = new()
  {
    KeywordSource = KeywordAbility.Changeling,
    Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Changeling }],
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var rawCount = m.Groups["count"].Value;
    Quantity count = rawCount.ToUpperInvariant() switch
    {
      "X" => VariableQuantity.X,
      "Y" => VariableQuantity.Y,
      "Z" => VariableQuantity.Z,
      _ => LiteralQuantity.Of(
        _wordCounts.TryGetValue(rawCount, out var n)
          ? n
          : int.TryParse(rawCount, out var digits) ? digits : 1
      ),
    };

    if (!_colorMap.TryGetValue(m.Groups["color"].Value, out var colorCode))
    {
      return false;
    }

    var subtype = m.Groups["subtype"].Value;
    subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..].ToLowerInvariant();

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = count,
      Token = new TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = [subtype],
        Abilities = [_changelingTokenAbility],
        IsCopy = false,
      },
    };
    return true;
  }
}
