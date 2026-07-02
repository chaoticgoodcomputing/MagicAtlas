namespace MagicAST.Parsing.Parsers.Static;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;
using MagicAST.Parsing;

/// <summary>
/// Parses "You may play [an | N] additional land[s] on each of your turns."
/// CR 305.2: "A player can normally play one land during their turn; however,
/// continuous effects may increase this number."
/// </summary>
[StaticRule(Priority = 944)]
public sealed class PlayAdditionalLandRule : IStaticRule
{
  // Matches both the indefinite-article form ("an additional land") and
  // the explicit-numeral-word form ("two additional lands").
  // Named capture group "count" holds either "an" or the numeral word.
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+play\s+(?<count>an|one|two|three|four|five|six|seven|eight|nine|ten)\s+additional\s+lands?\s+on\s+each\s+of\s+your\s+turns\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Numeral words → integer values. "an" and "one" both map to implicit-1;
  // "an" produces a null Count (omitted in JSON) to match the legacy
  // Exploration fixture, while "one" produces LiteralQuantity(1).
  private static readonly Dictionary<string, int?> _wordToCount =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["an"]    = null,  // "an additional land" — implicit 1, Count omitted
      ["one"]   = 1,
      ["two"]   = 2,
      ["three"] = 3,
      ["four"]  = 4,
      ["five"]  = 5,
      ["six"]   = 6,
      ["seven"] = 7,
      ["eight"] = 8,
      ["nine"]  = 9,
      ["ten"]   = 10,
    };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var countWord = match.Groups["count"].Value;
    if (!_wordToCount.TryGetValue(countWord, out var countValue))
    {
      return null;
    }

    Quantity? count = countValue.HasValue ? LiteralQuantity.Of(countValue.Value) : null;

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new OptionalEffect
          {
            Inner = new PlayAdditionalLandEffect { Count = count },
          },
        ],
      },
    ];
  }
}
