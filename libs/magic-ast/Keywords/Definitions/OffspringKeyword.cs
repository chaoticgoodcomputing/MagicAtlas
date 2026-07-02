namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Offspring {cost}: You may pay an additional {cost} as you cast this spell.
/// If you do, when this creature enters, create a 1/1 token that's a copy of it.
/// Rule 702.175. MAST records the keyword and the additional cost; the token-copy
/// creation on entry is inferred from the rules. Combinator-only: no matching
/// <c>KeywordDefinitions</c> entry exists in the legacy registry.
/// </summary>
[Keyword]
public sealed class OffspringKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Offspring")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Offspring,
      Effects = [new OffspringEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
