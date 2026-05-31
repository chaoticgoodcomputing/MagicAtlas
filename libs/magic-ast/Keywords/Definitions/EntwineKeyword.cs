namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Entwine [cost]: You may choose all modes of this spell instead of just the number
/// specified. If you do, you pay an additional [the entwine cost].
///
/// <para>
/// CR 702.42: "Entwine is a static ability of modal spells (see rule 700.2) that
/// functions while the spell with entwine is on the stack. 'Entwine [cost]' means 'You
/// may choose all modes of this spell instead of just the number specified. If you do,
/// you pay an additional [the entwine cost].'"
/// </para>
///
/// <para>
/// Entwine is optional (IsOptional: true) and paid at most once (Repeatable: false) —
/// you either pay it to unlock all modes or you don't. The mode-selection override is
/// engine territory; MAST models only the additional cost.
/// </para>
/// </summary>
[Keyword]
public sealed class EntwineKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Entwine")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Entwine",
      Effects = [new AdditionalCastCostEffect
      {
        Cost = cost,
        IsOptional = true,
        Repeatable = false,
      }],
      Reminder = reminder,
    }
  );
}
