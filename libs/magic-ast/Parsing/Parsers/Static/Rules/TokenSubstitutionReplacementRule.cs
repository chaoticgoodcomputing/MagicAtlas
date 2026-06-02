namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.References;

[StaticRule(Priority = 979)]
public sealed class TokenSubstitutionReplacementRule : IStaticRule
{
  // Pattern: "If you would create a [Subtype1], [Subtype2], or [Subtype3] token,
  // instead create one of each."
  // The subtypes group captures the comma-and-"or"-delimited list between "a " and
  // " token". The list must contain at least two elements (single-element would be
  // a different pattern — "If you would create a Treasure token, instead ...").
  private static readonly Regex _tokenSubstitutionPattern = new(
    @"^\s*If\s+you\s+would\s+create\s+a\s+(?<subtypes>[A-Z][a-z]+(?:,\s+[A-Z][a-z]+)*,?\s+or\s+[A-Z][a-z]+)\s+token,\s+instead\s+create\s+one\s+of\s+each\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _tokenSubstitutionPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    // Parse the comma-separated list of token subtypes from the "a X, Y, or Z token" clause.
    var subtypeList = match.Groups["subtypes"].Value;
    var subtypes = ParseDisjunctionList(subtypeList);
    if (subtypes.Count == 0)
    {
      return null;
    }

    // Build one CreateTokenEffect per subtype using predefined token factories.
    var createEffects = new List<Effect>();
    foreach (var subtype in subtypes)
    {
      var token = subtype switch
      {
        "Clue" => MagicAST.AST.Effects.TokenDefinition.Clue(),
        "Food" => MagicAST.AST.Effects.TokenDefinition.Food(),
        "Treasure" => MagicAST.AST.Effects.TokenDefinition.Treasure(),
        "Blood" => MagicAST.AST.Effects.TokenDefinition.Blood(),
        _ => null,
      };
      if (token is null)
      {
        return null; // Unknown predefined token type — bail.
      }
      createEffects.Add(new MagicAST.AST.Effects.TokenCopy.CreateTokenEffect
      {
        Player = MagicAST.AST.References.ObjectReference.You(),
        Count = MagicAST.AST.Quantities.LiteralQuantity.Of(1),
        Token = token,
      });
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.TokenCreationEvent
          {
            TokenFilter = new ObjectFilter
            {
              Subtypes = subtypes,
            },
          },
          OriginalEventOccurs = false,
          Replacement = new MagicAST.AST.Effects.Core.CompositeEffect
          {
            Effects = createEffects,
          },
        }],
      },
    ];
  }

  /// <summary>
  /// Parses a comma-and-"or"-delimited English list into individual tokens.
  /// Handles "A, B, or C", "A or B", and "A, B, C, or D" shapes.
  /// </summary>
  private static IReadOnlyList<string> ParseDisjunctionList(string text)
  {
    // Split on ", " and " or " to get individual tokens.
    // Handle "A, B, or C" → ["A", "B", "C"]
    var result = new List<string>();
    var parts = text.Split(new[] { ", or ", ",or ", " or " }, StringSplitOptions.RemoveEmptyEntries);
    foreach (var part in parts)
    {
      var trimmed = part.Trim();
      // The first part may contain further comma-separated items: "A, B" from "A, B, or C"
      var subParts = trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries);
      foreach (var sub in subParts)
      {
        var s = sub.Trim();
        if (s.Length > 0)
        {
          result.Add(s);
        }
      }
    }
    return result;
  }
}
