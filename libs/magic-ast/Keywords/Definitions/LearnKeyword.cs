namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Learn: You may reveal a Lesson card you own from outside the game and put it into
/// your hand, or discard a card to draw a card.
/// Rule 702.148. A keyword action: parameterless keyword marker. MAST records keyword
/// presence; the choice semantics are engine territory.
/// Note: oracle text uses "Learn." (with a trailing period absorbed by the sentence
/// tokenizer) followed by the reminder in parentheses.
/// </summary>
[Keyword]
public sealed class LearnKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Learn",
      RuleReference = "702.148",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Learn",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Learn }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Learn")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Learn",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Learn }],
      Reminder = reminder,
    }
  );
}
