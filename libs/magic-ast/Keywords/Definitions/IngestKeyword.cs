namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Ingest: Whenever this creature deals combat damage to a player, that player
/// exiles the top card of their library.
/// Rule 702.115. Parameterless keyword marker — MAST records keyword presence;
/// the combat-damage trigger and exile-top-of-library action are engine territory.
/// </summary>
[Keyword]
public sealed class IngestKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Ingest",
      RuleReference = "702.115",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Ingest",
        Effects = [new IngestEffect()],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Ingest")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Ingest",
      Effects = [new IngestEffect()],
      Reminder = reminder,
    }
  );
}
