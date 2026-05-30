namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Aftermath: Cast this spell only from your graveyard. Then exile it.
/// Rule 702.128. Found on the bottom half of split cards. MAST records
/// the keyword's presence; the graveyard-only cast restriction and
/// exile-on-resolution mechanics are engine territory.
///
/// <para>
/// The <see cref="Definition"/> is the verbatim former <c>KeywordDefinitions.Aftermath</c>;
/// the <see cref="Combinator"/> is the verbatim former <c>OracleParsers.Aftermath</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class AftermathKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition { get; } =
    new()
    {
      Name = "Aftermath",
      RuleReference = "702.128",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Aftermath",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Aftermath }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Aftermath")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Aftermath",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Aftermath }],
      Reminder = reminder,
    }
  );
}
