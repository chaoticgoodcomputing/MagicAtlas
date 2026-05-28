namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Myriad: Triggered keyword ability. Whenever this creature attacks, for each opponent other
/// than defending player, you may create a token copy tapped and attacking that player or a
/// planeswalker they control; exile the tokens at end of combat.
/// Rule 702.116. MAST records keyword presence; the per-opponent copy-creation,
/// tapped-and-attacking, and delayed-exile semantics are engine territory.
/// </summary>
[Keyword]
public sealed class MyriadKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Myriad",
      RuleReference = "702.116",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Myriad",
        Effects = [new MyriadEffect()],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Myriad")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Myriad",
      Effects = [new MyriadEffect()],
      Reminder = reminder,
    }
  );
}
