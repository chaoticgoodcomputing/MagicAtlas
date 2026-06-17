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
/// Rule 702.13. EvasionEffect with CanBeBlockedBy structuring the artifact type
/// (CardTypes) and the shares-a-color predicate (SharesColorWith = the source object,
/// Self); mirrors Fear (702.36) but substitutes the relational color-share axis for the
/// fixed black Colors entry.
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
        KeywordSource = KeywordAbility.Intimidate,
        Effects = [new EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature", "artifact"],
            SharesColorWith = ObjectReference.Self(),
          },
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Intimidate")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Intimidate,
      Effects = [new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter
        {
          CardTypes = ["creature", "artifact"],
          SharesColorWith = ObjectReference.Self(),
        },
      }],
      Reminder = reminder,
    }
  );
}
