namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Phasing: This permanent phases in or out before you untap during each of
/// your untap steps. Rule 702.26. MAST records the keyword's presence;
/// the phase-bookkeeping is engine territory.
/// </summary>
[Keyword]
public sealed class PhasingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Phasing",
      RuleReference = "702.26",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Phasing",
        Effects = [new PhasingEffect { IsOptional = false }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Phasing")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Phasing",
      Effects = [new PhasingEffect { IsOptional = false }],
      Reminder = reminder,
    }
  );
}
