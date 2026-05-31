namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Storm: When you cast this spell, copy it for each other spell that was
/// cast before it this turn. You may choose new targets for the copies.
/// Rule 702.40.
///
/// <para>
/// Storm is rules-defined as a triggered ability. By the codebase convention
/// of attaching keyword expansions to <see cref="StaticAbility"/> with
/// KeywordSource set, the triggered semantics live in the rules engine, not
/// the AST.
/// </para>
/// </summary>
[Keyword]
public sealed class StormKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Storm",
      RuleReference = "702.40",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Storm",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Storm }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Storm")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Storm",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Storm }],
      Reminder = reminder,
    }
  );
}
