namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Start your engines! (Aetherdrift): If you have no speed, it starts at 1.
/// It increases once on each of your turns when an opponent loses life. Max
/// speed is 4. Rule 702.178. MAST records the keyword's presence; the speed
/// initialization and increment semantics are engine territory.
///
/// <para>
/// The '!' is silently dropped by the tokenizer, so the combinator matches
/// "Start your engines". Multi-word simple keyword.
/// </para>
/// </summary>
[Keyword]
public sealed class StartYourEnginesKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Start your engines",
      RuleReference = "702.178",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = KeywordAbility.StartYourEngines,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.StartYourEngines }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from start in Keyword("Start")
    from your in Token.EqualTo(OracleToken.Your)
    from engines in Keyword("engines")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.StartYourEngines,
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.StartYourEngines }],
      Reminder = reminder,
    }
  );
}
