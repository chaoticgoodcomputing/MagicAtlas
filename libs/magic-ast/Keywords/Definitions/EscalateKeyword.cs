namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Escalate [cost]: pay this cost for each mode chosen beyond the first.
/// CR 702.120: "Escalate is a static ability of modal spells (see rule 700.2) that
/// functions while the spell with escalate is on the stack. 'Escalate [cost]' means
/// 'For each mode you choose beyond the first as you cast this spell, you pay an
/// additional [cost].'"
///
/// IsOptional:true — the controller may choose only one mode (paying zero escalate costs).
/// Repeatable:true — the cost is paid once per extra mode chosen (zero, one, or more times).
/// Combinator-only: no KeywordDefinition entry in the legacy registry.
/// </summary>
[Keyword]
public sealed class EscalateKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Escalate")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Escalate",
      Effects = [new AdditionalCastCostEffect
      {
        Cost = cost,
        IsOptional = true,
        Repeatable = true,
      }],
      Reminder = reminder,
    }
  );
}
