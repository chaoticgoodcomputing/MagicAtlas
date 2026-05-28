namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Squad {cost}: As an additional cost to cast this spell, you may pay {cost}
/// any number of times. When this creature enters, create that many token copies
/// of it. Rule 702.150. MAST records the keyword and the squad cost; the
/// token-copy creation on entry is inferred from the rules. Combinator-only:
/// no matching <c>KeywordDefinitions</c> entry exists in the legacy registry.
/// </summary>
[Keyword]
public sealed class SquadKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Squad")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Squad",
      Effects = [new SquadEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
