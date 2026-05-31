namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Ascend: If you control ten or more permanents, you get the city's blessing
/// for the rest of the game.
/// Rule 702.131. Applies to both permanents (Rule 702.131b) and spells (Rule
/// 702.131a). MAST records the keyword's presence; the city's-blessing
/// designation and downstream effects are engine territory.
///
/// <para>
/// The <see cref="Definition"/> is the verbatim former <c>KeywordDefinitions.Ascend</c>;
/// the <see cref="Combinator"/> is the verbatim former <c>OracleParsers.Ascend</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class AscendKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition { get; } =
    new()
    {
      Name = "Ascend",
      RuleReference = "702.131",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Ascend",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Ascend }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Ascend")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Ascend",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Ascend }],
      Reminder = reminder,
    }
  );
}
