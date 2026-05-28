namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

[StaticRule(Priority = 977)]
public sealed class TokenDoublingReplacementRule : IStaticRule
{
  // Pattern: "If an effect would create one or more tokens under your control,
  // it creates twice that many of those tokens instead." (Doubling Season)
  private static readonly Regex _tokenDoublingPattern = new(
    @"^\s*If\s+an\s+effect\s+would\s+create\s+one\s+or\s+more\s+tokens\s+under\s+your\s+control,\s+it\s+creates\s+twice\s+that\s+many\s+of\s+those\s+tokens\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Pattern: "If one or more tokens would be created under your control, twice
  // that many of those tokens are created instead." (Parallel Lives / Anointed Procession)
  private static readonly Regex _tokenDoublingPassivePattern = new(
    @"^\s*If\s+one\s+or\s+more\s+tokens\s+would\s+be\s+created\s+under\s+your\s+control,\s+twice\s+that\s+many\s+of\s+those\s+tokens\s+are\s+created\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_tokenDoublingPattern.IsMatch(clause.RawText)
        && !_tokenDoublingPassivePattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.TokenCreationEvent
          {
            MinimumQuantity = 1,
            Controller = ObjectReference.You(),
          },
          OriginalEventOccurs = false,
          Modifier = new MagicAST.AST.Effects.Replacement.ReplacementModifier
          {
            Type = "double",
          },
        }],
      },
    ];
  }
}
