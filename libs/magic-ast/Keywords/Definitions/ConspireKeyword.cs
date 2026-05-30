namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Conspire: As you cast this spell, you may tap two untapped creatures you control
/// that share a color with it. When you do, copy it.
/// Rule 702.78. MAST records the keyword's presence; the tap-two-creatures additional
/// cost and spell-copy triggered ability are engine territory.
/// </summary>
[Keyword]
public sealed class ConspireKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Conspire",
      RuleReference = "702.78",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Conspire",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Conspire }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Conspire")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Conspire",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Conspire }],
      Reminder = reminder,
    }
  );
}
