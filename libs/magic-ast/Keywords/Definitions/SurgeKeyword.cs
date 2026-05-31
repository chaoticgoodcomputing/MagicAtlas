namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Surge {cost}: You may cast this spell for its surge cost if you or a teammate
/// has cast another spell this turn.
/// Rule 702.114. MAST records the keyword and its associated surge cost; the
/// precondition (you or a teammate has cast another spell this turn) is
/// conventionally inferred from the rules and captured in reminder text.
/// </summary>
[Keyword]
public sealed class SurgeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Surge")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Surge,
      Effects = [new SurgeEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
