namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 997)]
public sealed class AllMustBlockRule : IStaticRule
{
  // "All creatures able to block <subject> do so."
  // Subject is "this creature" (Self) or "enchanted creature" (EnchantedOrEquipped).
  private static readonly Regex _allMustBlockPattern = new(
    @"^\s*All\s+creatures\s+able\s+to\s+block\s+(?<subject>this\s+creature|enchanted\s+creature)\s+do\s+so\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _allMustBlockPattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    var subject = m.Groups["subject"].Value.Trim().ToLowerInvariant();
    var blockTarget = subject switch
    {
      "enchanted creature" => new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
      _ => ObjectReference.Self(), // "this creature" or any self-referential subject
    };

    return
    [
      new StaticAbility
      {
        Effects = [new AllMustBlockEffect { BlockTarget = blockTarget }],
      },
    ];
  }
}
