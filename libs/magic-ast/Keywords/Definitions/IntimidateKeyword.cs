namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Intimidate: This creature can't be blocked except by artifact creatures and/or
/// creatures that share a color with it.
/// Rule 702.13. EvasionEffect with CanBeBlockedBy covering the artifact-type and
/// shares-a-color predicates; mirrors Fear (702.36) but substitutes the color-share
/// predicate for the fixed black-color predicate.
/// </summary>
[Keyword]
public sealed class IntimidateKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Intimidate",
      RuleReference = "702.13",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Intimidate",
        Effects = [new EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["artifact", "shares a color"],
          },
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Intimidate")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Intimidate",
      Effects = [new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["artifact", "shares a color"],
        },
      }],
      Reminder = reminder,
    }
  );
}
