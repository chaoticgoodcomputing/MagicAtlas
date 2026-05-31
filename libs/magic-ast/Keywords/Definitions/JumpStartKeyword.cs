namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Jump-start: You may cast this card from your graveyard by discarding a card in
/// addition to paying its other costs. If you do, this card is exiled as it resolves.
/// Rule 702.133. MAST records the keyword's presence; the graveyard-cast, discard
/// additional cost, and exile-on-resolution machinery are engine territory.
/// Parameterless keyword marker — mirrors Retrace, Rebound.
/// Note: "Jump-start" is a single Word token.
/// </summary>
[Keyword]
public sealed class JumpStartKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Jump-start",
      RuleReference = "702.133",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Jump-start",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.JumpStart }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Jump-start")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Jump-start",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.JumpStart }],
      Reminder = reminder,
    }
  );
}
